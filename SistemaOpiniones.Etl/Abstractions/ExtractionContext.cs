namespace SistemaOpiniones.Etl.Abstractions;

/// <summary>
/// Estado compartido de una corrida de extracción. Se crea uno por lote y se pasa a cada
/// extractor, que lo usa para sellar los registros con el mismo <see cref="BatchId"/> y
/// para contabilizar los descartes.
///
/// Los contadores usan operaciones atómicas porque las fuentes se extraen en paralelo.
/// </summary>
public sealed class ExtractionContext
{
    private int _rejected;

    public ExtractionContext(string batchId, DateTime startedAtUtc)
    {
        BatchId = batchId;
        StartedAtUtc = startedAtUtc;
    }

    public string BatchId { get; }

    public DateTime StartedAtUtc { get; }

    /// <summary>Registros que la fuente entregó pero que no se pudieron leer (fila corrupta, tipo inválido).</summary>
    public int Rejected => Volatile.Read(ref _rejected);

    public void RegisterRejected() => Interlocked.Increment(ref _rejected);
}
