using CsvHelper.Configuration.Attributes;

namespace SistemaOpiniones.Data.Models.DTO;

// surveys_part1.csv -> IdOpinion,IdCliente,IdProducto,Fecha,Comentario,Clasificación,PuntajeSatisfacción,Fuente
public class SurveyDto
{
    public int IdOpinion { get; set; }
    public int IdCliente { get; set; }   // numérico en este archivo
    public int IdProducto { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Comentario { get; set; }

    [Name("Clasificación")]
    public string? Clasificacion { get; set; }

    [Name("PuntajeSatisfacción")]
    public byte? PuntajeSatisfaccion { get; set; }

    public string Fuente { get; set; } = null!;
}
