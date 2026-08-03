using SistemaOpiniones.Data.Result;
using SistemaOpiniones.Data.Enums;

namespace SistemaOpiniones.Data.Interfaces;

public interface IBaseService<TDto, TEntity>
    where TDto : class
    where TEntity : class
{
    Task<OperationResult> LoadAsync();
    Task<OperationResult> SaveAsync(MethodDbEnum methodDbEnum);
}
