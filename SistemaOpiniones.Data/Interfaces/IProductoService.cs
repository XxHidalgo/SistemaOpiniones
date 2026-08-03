using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface IProductoService : IBaseService<ProductDto, Producto>
{
    Task<OperationResult> LoadProducto();
    Task<OperationResult> SaveProducto();
}
