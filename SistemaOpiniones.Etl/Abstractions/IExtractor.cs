using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Abstractions;

// Contrato común de las fuentes de datos del proceso de extracción.
public interface IExtractor
{
    string SourceName { get; }

    // CSV, Database o ApiRest
    string SourceType { get; }

    bool Enabled { get; }

    IAsyncEnumerable<RawOpinion> ExtractAsync(ExtractionContext context, CancellationToken cancellationToken);
}
