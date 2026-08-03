using System.Configuration;
using AutoMapper;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Services;

// Las clasificaciones vienen repetidas dentro de surveys_part1.csv -> se toman las únicas.
public class ClasificacionService : BaseService<SurveyDto, Clasificacion>, IClasificacionService
{
    public ClasificacionService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["SurveysCsvPath"]!)
    {
    }

    protected override Func<Clasificacion, object>? DistinctKey => c => c.Nombre;

    public Task<OperationResult> LoadClasificacion()
        => LoadAsync();

    public Task<OperationResult> SaveClasificacion()
        => SaveAsync(MethodDbEnum.EntityFramework);
}
