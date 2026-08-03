namespace SistemaOpiniones.Data.Result;
public class OperationResult
{
    public bool Success { get; set; } = true;

    public string? Message { get; set; }

    public object? Data { get; set; }

    // Conteos del proceso de carga
    public int Processed { get; set; }   // leídos/mapeados
    public int Inserted { get; set; }    // guardados en la BD
    public int Rejected { get; set; }    // duplicados + errores
}
