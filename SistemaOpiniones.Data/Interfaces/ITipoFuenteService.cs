using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface ITipoFuenteService : IBaseService<FuenteDatosDto, TipoFuente>
{
    Task<OperationResult> LoadTipoFuente();
    Task<OperationResult> SaveTipoFuente();
}
