using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Abstractions;

// Destino de staging: archivos o tabla SQL según configuración.
public interface IStagingWriter
{
    string Describe(string sourceName, ExtractionContext context);

    Task<int> WriteAsync(
        string sourceName,
        ExtractionContext context,
        IAsyncEnumerable<RawOpinion> records,
        CancellationToken cancellationToken);
}
