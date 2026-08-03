using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface ISurveyService : IBaseService<SurveyDto, Opinion>
{
    Task<OperationResult> LoadSurvey();
    Task<OperationResult> SaveSurvey();
}
