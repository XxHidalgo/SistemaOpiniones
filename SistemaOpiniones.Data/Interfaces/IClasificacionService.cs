using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface IClasificacionService : IBaseService<SurveyDto, Clasificacion>
{
    Task<OperationResult> LoadClasificacion();
    Task<OperationResult> SaveClasificacion();
}
