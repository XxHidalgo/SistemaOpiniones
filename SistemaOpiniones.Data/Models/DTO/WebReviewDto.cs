namespace SistemaOpiniones.Data.Models.DTO;

// web_reviews.csv -> IdReview,IdCliente,IdProducto,Fecha,Comentario,Rating
public class WebReviewDto
{
    public string IdReview { get; set; } = null!;
    public string? IdCliente { get; set; }   // formato "C007"
    public string? IdProducto { get; set; }  // formato "P016"
    public DateOnly Fecha { get; set; }
    public string? Comentario { get; set; }
    public int? Rating { get; set; }
}
