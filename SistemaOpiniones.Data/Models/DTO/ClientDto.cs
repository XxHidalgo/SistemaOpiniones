namespace SistemaOpiniones.Data.Models.DTO;

// clients.csv -> IdCliente,Nombre,Email
public class ClientDto
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Email { get; set; }
}
