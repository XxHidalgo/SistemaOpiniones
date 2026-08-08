namespace SistemaOpiniones.Etl.Abstractions;

// Estado de una corrida: identifica el lote y cuenta los descartes de cada fuente.
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

    public int Rejected => Volatile.Read(ref _rejected);

    public void RegisterRejected() => Interlocked.Increment(ref _rejected);
}
