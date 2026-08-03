using System.Configuration;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Services;

public class ProductoService : BaseService<ProductDto, Producto>, IProductoService
{
    public ProductoService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["ProductsCsvPath"]!)
    {
    }

    public Task<OperationResult> LoadProducto()
        => LoadAsync();

    protected override Func<Producto, object>? DistinctKey => p => p.IdProducto;

    // El CSV trae la categoría como texto ("Juguetes"); aquí la resolvemos a su IdCategoria.
    protected override async Task ResolveForeignKeysAsync(List<Producto> entities, List<ProductDto> dtos)
    {
        var categorias = await Context.Categoria
            .ToDictionaryAsync(c => c.Nombre, c => c.IdCategoria);

        for (int i = 0; i < entities.Count; i++)
        {
            if (categorias.TryGetValue(dtos[i].Categoria, out var id))
                entities[i].IdCategoria = id;
        }
    }

    public Task<OperationResult> SaveProducto()
        => SaveAsync(MethodDbEnum.AdoNet);

    protected override (string ProcedureName, List<SqlParameter> Parameters) BuildInsertCommand(Producto entity)
        => ("dbo.usp_InsertProducto", new List<SqlParameter>
        {
            new("@IdProducto", entity.IdProducto),
            new("@Nombre", entity.Nombre),
            new("@IdCategoria", entity.IdCategoria)
        });
}
