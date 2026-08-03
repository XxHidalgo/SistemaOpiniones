using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface ICategoriaService : IBaseService<ProductDto, Categoria>
{
    Task<OperationResult> LoadCategoria();
    Task<OperationResult> SaveCategoria();
}
