using CsvHelper.Configuration.Attributes;

namespace SistemaOpiniones.Data.Models.DTO;

// products.csv -> IdProducto,Nombre,Categoría
public class ProductDto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;

    [Name("Categoría")]
    public string Categoria { get; set; } = null!;
}
