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

public class SocialCommentService : BaseService<SocialCommentDto, Opinion>, ISocialCommentService
{
    public SocialCommentService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["SocialCommentsCsvPath"]!)
    {
    }

    // IdCliente/IdProducto vienen como texto ("C019", "P003"). Se parsean y, si el
    // registro existe, se asigna; si no, queda en null (la FK es opcional).
    protected override async Task ResolveForeignKeysAsync(List<Opinion> entities, List<SocialCommentDto> dtos)
    {
        var clientes = (await Context.Cliente.Select(c => c.IdCliente).ToListAsync()).ToHashSet();
        var productos = (await Context.Producto.Select(p => p.IdProducto).ToListAsync()).ToHashSet();

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].IdCliente = OpinionHelper.ResolverId(dtos[i].IdCliente, clientes);
            entities[i].IdProducto = OpinionHelper.ResolverId(dtos[i].IdProducto, productos);
            entities[i].IdFuente = null;
        }
    }

    public Task<OperationResult> LoadSocialComment()
        => LoadAsync();

    public Task<OperationResult> SaveSocialComment()
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
