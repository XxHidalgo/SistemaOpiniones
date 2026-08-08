using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Abstractions;

/// <summary>
/// Destino de los datos extraídos. La extracción no escribe en las tablas finales del
/// modelo analítico: aterriza en staging y ahí termina su responsabilidad.
///
/// Tener esto como interfaz permite alternar entre archivos y tablas staging por
/// configuración, y hace que los extractores no sepan nada del destino.
/// </summary>
public interface IStagingWriter
{
    /// <summary>Descripción legible del destino, para logs y para el reporte de la corrida.</summary>
    string Describe(string sourceName, ExtractionContext context);

    /// <summary>Consume el flujo de registros y los persiste. Devuelve cuántos escribió.</summary>
    Task<int> WriteAsync(
        string sourceName,
        ExtractionContext context,
        IAsyncEnumerable<RawOpinion> records,
        CancellationToken cancellationToken);
}
