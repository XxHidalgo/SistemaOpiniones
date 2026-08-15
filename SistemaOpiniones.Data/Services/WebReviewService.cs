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

public class WebReviewService : BaseService<WebReviewDto, Opinion>, IWebReviewService
{
    public WebReviewService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["WebReviewsCsvPath"]!)
    {
    }

    // IdCliente/IdProducto vienen como texto ("C007", "P016"); se parsean y validan.
    // Estas opiniones llegan del sitio web, así que apuntan a la fuente de tipo "Web".
    // El CSV no trae clasificación, se deriva del Rating con el mismo criterio de las
    // encuestas (1-2 Negativa, 3 Neutra, 4-5 Positiva).
    protected override async Task ResolveForeignKeysAsync(List<Opinion> entities, List<WebReviewDto> dtos)
    {
        var clientes = (await Context.Cliente.Select(c => c.IdCliente).ToListAsync()).ToHashSet();
        var productos = (await Context.Producto.Select(p => p.IdProducto).ToListAsync()).ToHashSet();
        var idFuente = await OpinionHelper.ResolverIdFuenteAsync(Context, "Web");
        var clasificaciones = await Context.Clasificacion
            .ToDictionaryAsync(c => c.Nombre, c => c.IdClasificacion);

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].IdCliente = OpinionHelper.ResolverId(dtos[i].IdCliente, clientes);
            entities[i].IdProducto = OpinionHelper.ResolverId(dtos[i].IdProducto, productos);
            entities[i].IdFuente = idFuente;

            var nombre = OpinionHelper.ClasificacionPorPuntaje(entities[i].PuntajeSatisfaccion);
            entities[i].IdClasificacion =
                nombre is not null && clasificaciones.TryGetValue(nombre, out var idc) ? idc : null;
        }
    }

    public Task<OperationResult> LoadWebReview()
        => LoadAsync();

    public Task<OperationResult> SaveWebReview()
        => SaveAsync(MethodDbEnum.AdoNet);

    protected override (string ProcedureName, List<SqlParameter> Parameters) BuildInsertCommand(Opinion entity)
        => ("dbo.usp_InsertOpinion", new List<SqlParameter>
        {
            new("@IdCliente", (object?)entity.IdCliente ?? DBNull.Value),
            new("@IdProducto", (object?)entity.IdProducto ?? DBNull.Value),
            new("@IdFuente", (object?)entity.IdFuente ?? DBNull.Value),
            new("@IdClasificacion", (object?)entity.IdClasificacion ?? DBNull.Value),
            new("@Fecha", entity.Fecha),
            new("@Comentario", (object?)entity.Comentario ?? DBNull.Value),
            new("@PuntajeSatisfaccion", (object?)entity.PuntajeSatisfaccion ?? DBNull.Value)
        });
}
