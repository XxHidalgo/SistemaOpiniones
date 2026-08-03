using System.Configuration;
using AutoMapper;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Services;

// Las categorías vienen repetidas dentro de products.csv -> se toman las únicas.
public class CategoriaService : BaseService<ProductDto, Categoria>, ICategoriaService
{
    public CategoriaService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["ProductsCsvPath"]!)
    {
    }

    protected override Func<Categoria, object>? DistinctKey => c => c.Nombre;

    public Task<OperationResult> LoadCategoria()
        => LoadAsync();

    public Task<OperationResult> SaveCategoria()
        => SaveAsync(MethodDbEnum.EntityFramework);
}
