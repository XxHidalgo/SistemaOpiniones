namespace SistemaOpiniones.Etl.Models;

// Registro crudo que aterriza en staging; todos los campos quedan como texto.
public sealed record RawOpinion
{
    public required string BatchId { get; init; }

    public required string SourceName { get; init; }

    public required string SourceType { get; init; }

    public string? ExternalId { get; init; }

    public string? ClienteRef { get; init; }

    public string? ProductoRef { get; init; }

    public string? FuenteRef { get; init; }

    public string? Fecha { get; init; }

    public string? Comentario { get; init; }

    public string? Puntaje { get; init; }

    public string? Clasificacion { get; init; }

    public required DateTime ExtractedAtUtc { get; init; }

    public string? RawPayload { get; init; }
}
