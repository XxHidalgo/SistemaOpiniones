using System.Diagnostics;
using Microsoft.Extensions.Options;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;

namespace SistemaOpiniones.Etl.Services;

// Lanza las fuentes habilitadas en paralelo y reporta el resumen del lote.
public sealed class ExtractionOrchestrator
{
    private readonly IReadOnlyList<IExtractor> _extractors;
    private readonly IStagingWriter _stagingWriter;
    private readonly EtlOptions _options;
    private readonly ILogger<ExtractionOrchestrator> _logger;

    public ExtractionOrchestrator(
        IEnumerable<IExtractor> extractors,
        IStagingWriter stagingWriter,
        IOptions<EtlOptions> options,
        ILogger<ExtractionOrchestrator> logger)
    {
        _extractors = extractors.ToList();
        _stagingWriter = stagingWriter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExtractionResult>> RunAsync(CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTime.UtcNow;
        var batchId = $"{startedAtUtc:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";

        var enabled = _extractors.Where(e => e.Enabled).ToList();
        var disabled = _extractors.Where(e => !e.Enabled).Select(e => e.SourceName).ToList();

        if (disabled.Count > 0)
            _logger.LogInformation("Fuentes deshabilitadas por configuración: {Sources}", string.Join(", ", disabled));

        if (enabled.Count == 0)
        {
            _logger.LogWarning("No hay ninguna fuente habilitada; no se extrajo nada.");
            return [];
        }

        _logger.LogInformation(
            "Lote {BatchId}: iniciando extracción de {Count} fuente(s) en paralelo -> {Sources}",
            batchId, enabled.Count, string.Join(", ", enabled.Select(e => e.SourceName)));

        var stopwatch = Stopwatch.StartNew();

        using var gate = _options.MaxDegreeOfParallelism > 0
            ? new SemaphoreSlim(_options.MaxDegreeOfParallelism)
            : null;

        var results = await Task.WhenAll(
            enabled.Select(extractor => ExtractSourceAsync(extractor, batchId, startedAtUtc, gate, cancellationToken)));

        stopwatch.Stop();

        LogSummary(batchId, results, stopwatch.ElapsedMilliseconds);

        return results;
    }

    private async Task<ExtractionResult> ExtractSourceAsync(
        IExtractor extractor,
        string batchId,
        DateTime startedAtUtc,
        SemaphoreSlim? gate,
        CancellationToken cancellationToken)
    {
        if (gate is not null)
            await gate.WaitAsync(cancellationToken);

        var context = new ExtractionContext(batchId, startedAtUtc);
        var stopwatch = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["BatchId"] = batchId,
            ["Source"] = extractor.SourceName
        });

        try
        {
            _logger.LogInformation("[{Source}] Extrayendo desde {Type}...", extractor.SourceName, extractor.SourceType);

            var target = _stagingWriter.Describe(extractor.SourceName, context);

            var written = await _stagingWriter.WriteAsync(
                extractor.SourceName,
                context,
                extractor.ExtractAsync(context, cancellationToken),
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "[{Source}] OK: {Written} registros en {Elapsed} ms ({Rate:N0} reg/s), {Rejected} descartados -> {Target}",
                extractor.SourceName, written, stopwatch.ElapsedMilliseconds,
                Rate(written, stopwatch.ElapsedMilliseconds), context.Rejected, target);

            return ExtractionResult.Ok(
                extractor.SourceName, extractor.SourceType,
                written, context.Rejected, stopwatch.ElapsedMilliseconds, target);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex, "[{Source}] Falló la extracción tras {Elapsed} ms",
                extractor.SourceName, stopwatch.ElapsedMilliseconds);

            return ExtractionResult.Fail(
                extractor.SourceName, extractor.SourceType, ex.Message, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            gate?.Release();
        }
    }

    private void LogSummary(string batchId, IReadOnlyList<ExtractionResult> results, long totalMs)
    {
        var extracted = results.Sum(r => r.Extracted);
        var rejected = results.Sum(r => r.Rejected);
        var failed = results.Count(r => !r.Success);
        var sequentialMs = results.Sum(r => r.ElapsedMs);

        _logger.LogInformation("──────── Resumen del lote {BatchId} ────────", batchId);

        foreach (var result in results)
        {
            if (result.Success)
            {
                _logger.LogInformation(
                    "  {Source,-28} {Type,-9} OK      {Extracted,7} extraídos  {Rejected,5} descartados  {Elapsed,7} ms",
                    result.SourceName, result.SourceType, result.Extracted, result.Rejected, result.ElapsedMs);
            }
            else
            {
                _logger.LogError(
                    "  {Source,-28} {Type,-9} ERROR   {Error}",
                    result.SourceName, result.SourceType, result.Error);
            }
        }

        _logger.LogInformation(
            "  TOTAL: {Extracted} registros, {Rejected} descartados, {Failed} fuente(s) con error en {Total} ms " +
            "(suma secuencial {Sequential} ms; el paralelismo ahorró {Saved} ms)",
            extracted, rejected, failed, totalMs, sequentialMs, Math.Max(0, sequentialMs - totalMs));
    }

    private static double Rate(int records, long elapsedMs) =>
        elapsedMs <= 0 ? records : records * 1000d / elapsedMs;
}
