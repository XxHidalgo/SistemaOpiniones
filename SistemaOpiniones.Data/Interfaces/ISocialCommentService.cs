using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Interfaces;

public interface ISocialCommentService : IBaseService<SocialCommentDto, Opinion>
{
    Task<OperationResult> LoadSocialComment();
    Task<OperationResult> SaveSocialComment();
}
