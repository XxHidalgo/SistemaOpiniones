using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface IFuenteDatosService : IBaseService<FuenteDatosDto, FuenteDatos>
{
    Task<OperationResult> LoadFuenteDatos();
    Task<OperationResult> SaveFuenteDatos();
}
