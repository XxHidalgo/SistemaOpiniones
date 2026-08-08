namespace SistemaOpiniones.Etl.Abstractions;

// Resultado de extraer una fuente; el orquestador lo usa para el resumen final.
public sealed record ExtractionResult
{
    public required string SourceName { get; init; }

    public required string SourceType { get; init; }

    public required bool Success { get; init; }

    public int Extracted { get; init; }

    public int Rejected { get; init; }

    public long ElapsedMs { get; init; }

    public string? StagingTarget { get; init; }

    public string? Error { get; init; }

    public static ExtractionResult Ok(
        string sourceName, string sourceType, int extracted, int rejected, long elapsedMs, string? target) =>
        new()
        {
            SourceName = sourceName,
            SourceType = sourceType,
            Success = true,
            Extracted = extracted,
            Rejected = rejected,
            ElapsedMs = elapsedMs,
            StagingTarget = target
        };

    public static ExtractionResult Fail(
        string sourceName, string sourceType, string error, long elapsedMs) =>
        new()
        {
            SourceName = sourceName,
            SourceType = sourceType,
            Success = false,
            ElapsedMs = elapsedMs,
            Error = error
        };
}
