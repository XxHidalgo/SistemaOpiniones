using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Staging;

/// <summary>
/// Aterriza lo extraído en archivos NDJSON (un objeto JSON por línea), uno por fuente y
/// por lote: <c>output/staging/{batchId}/{fuente}.ndjson</c>.
///
/// El formato por líneas permite escribir en streaming y volver a leer el archivo sin
/// cargarlo entero, a diferencia de un único arreglo JSON.
///
/// Es el destino por defecto porque no depende de que haya un servidor de base de datos
/// disponible: el proceso de extracción se puede ejecutar y evidenciar por sí solo.
/// </summary>
public sealed class FileStagingWriter : IStagingWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly StagingOptions _options;
    private readonly ILogger<FileStagingWriter> _logger;

    public FileStagingWriter(IOptions<EtlOptions> options, ILogger<FileStagingWriter> logger)
    {
        _options = options.Value.Staging;
        _logger = logger;
    }

    public string Describe(string sourceName, ExtractionContext context) => ResolvePath(sourceName, context);

    public async Task<int> WriteAsync(
        string sourceName,
        ExtractionContext context,
        IAsyncEnumerable<RawOpinion> records,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(sourceName, context);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.Read,
            bufferSize: 64 * 1024, useAsync: true);

        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var written = 0;

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(record, SerializerOptions));
            written++;

            // Se vacía el buffer por lotes en lugar de en cada línea: menos llamadas al
            // sistema de archivos sin dejar de acotar la memoria.
            if (written % _options.BatchSize == 0)
            {
                await writer.FlushAsync();
                _logger.LogDebug("{Source}: {Written} registros escritos en staging", sourceName, written);
            }
        }

        await writer.FlushAsync();

        return written;
    }

    private string ResolvePath(string sourceName, ExtractionContext context) =>
        Path.GetFullPath(Path.Combine(_options.Directory, context.BatchId, $"{sourceName}.ndjson"));
}
