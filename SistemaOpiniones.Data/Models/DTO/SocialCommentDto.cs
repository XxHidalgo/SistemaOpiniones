namespace SistemaOpiniones.Data.Models.DTO;

// social_comments.csv -> IdComment,IdCliente,IdProducto,Fuente,Fecha,Comentario
public class SocialCommentDto
{
    public string IdComment { get; set; } = null!;
    public string? IdCliente { get; set; }   // formato "C019", puede venir vacío
    public string? IdProducto { get; set; }  // formato "P003"
    public string Fuente { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public string? Comentario { get; set; }
}
