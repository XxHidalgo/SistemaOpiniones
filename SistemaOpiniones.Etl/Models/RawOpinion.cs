namespace SistemaOpiniones.Etl.Models;

/// <summary>
/// Registro crudo aterrizado en staging.
///
/// La extracción deliberadamente NO interpreta ni convierte: conserva cada campo como
/// texto tal y como llegó de la fuente. Así la fase de transformación (Actividad 2)
/// decide tipos, reglas de negocio y rechazos teniendo a mano la evidencia original,
/// y un error de parseo nunca hace perder el dato extraído.
/// </summary>
public sealed record RawOpinion
{
    /// <summary>Identificador del lote de ejecución; agrupa todo lo extraído en una corrida.</summary>
    public required string BatchId { get; init; }

    /// <summary>Nombre lógico de la fuente (ej. "EncuestasInternas").</summary>
    public required string SourceName { get; init; }

    /// <summary>Tipo de origen: CSV, Database o ApiRest.</summary>
    public required string SourceType { get; init; }

    /// <summary>Identificador del registro en el sistema origen (IdOpinion, IdReview, IdComment...).</summary>
    public string? ExternalId { get; init; }

    /// <summary>Referencia al cliente tal como viene: "19" en encuestas, "C019" en los otros canales.</summary>
    public string? ClienteRef { get; init; }

    /// <summary>Referencia al producto tal como viene: "3" o "P003".</summary>
    public string? ProductoRef { get; init; }

    /// <summary>Canal o fuente declarada por el propio registro (ej. "Twitter", "Encuesta").</summary>
    public string? FuenteRef { get; init; }

    public string? Fecha { get; init; }

    public string? Comentario { get; init; }

    /// <summary>Puntaje/rating sin convertir; cada canal usa su propia escala.</summary>
    public string? Puntaje { get; init; }

    /// <summary>Etiqueta de sentimiento si la fuente la trae (solo las encuestas la incluyen).</summary>
    public string? Clasificacion { get; init; }

    public required DateTime ExtractedAtUtc { get; init; }

    /// <summary>Payload original (JSON) cuando la fuente lo provee; sirve de traza y auditoría.</summary>
    public string? RawPayload { get; init; }
}
