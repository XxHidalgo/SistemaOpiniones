namespace SistemaOpiniones.Data.Models.DTO;

// fuente_datos.csv -> IdFuente,TipoFuente,FechaCarga
public class FuenteDatosDto
{
    public string IdFuente { get; set; } = null!;
    public string TipoFuente { get; set; } = null!;
    public DateOnly FechaCarga { get; set; }
}
