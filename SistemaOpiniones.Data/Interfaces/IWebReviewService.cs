using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface IWebReviewService : IBaseService<WebReviewDto, Opinion>
{
    Task<OperationResult> LoadWebReview();
    Task<OperationResult> SaveWebReview();
}
