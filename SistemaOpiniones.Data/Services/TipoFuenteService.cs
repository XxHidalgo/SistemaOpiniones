using System.Configuration;
using AutoMapper;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Services;

// Los tipos de fuente vienen repetidos dentro de fuente_datos.csv -> se toman los únicos.
public class TipoFuenteService : BaseService<FuenteDatosDto, TipoFuente>, ITipoFuenteService
{
    public TipoFuenteService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["FuenteDatosCsvPath"]!)
    {
    }

    protected override Func<TipoFuente, object>? DistinctKey => t => t.Nombre;

    public Task<OperationResult> LoadTipoFuente()
        => LoadAsync();

    public Task<OperationResult> SaveTipoFuente()
        => SaveAsync(MethodDbEnum.EntityFramework);
}
