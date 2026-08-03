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

public class SurveyService : BaseService<SurveyDto, Opinion>, ISurveyService
{
    public SurveyService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["SurveysCsvPath"]!)
    {
    }

    // IdCliente/IdProducto son numéricos pero pueden estar fuera de rango -> null si no existen.
    // Clasificacion viene como texto ("Neutra") -> se resuelve a su IdClasificacion.
    protected override async Task ResolveForeignKeysAsync(List<Opinion> entities, List<SurveyDto> dtos)
    {
        var clientes = (await Context.Cliente.Select(c => c.IdCliente).ToListAsync()).ToHashSet();
        var productos = (await Context.Producto.Select(p => p.IdProducto).ToListAsync()).ToHashSet();
        var clasificaciones = await Context.Clasificacion
            .ToDictionaryAsync(c => c.Nombre, c => c.IdClasificacion);

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].IdCliente = clientes.Contains(dtos[i].IdCliente) ? dtos[i].IdCliente : null;
            entities[i].IdProducto = productos.Contains(dtos[i].IdProducto) ? dtos[i].IdProducto : null;
            entities[i].IdFuente = null;

            entities[i].IdClasificacion =
                dtos[i].Clasificacion is not null && clasificaciones.TryGetValue(dtos[i].Clasificacion!, out var idc)
                    ? idc
                    : null;
        }
    }

    public Task<OperationResult> LoadSurvey()
        => LoadAsync();

    public Task<OperationResult> SaveSurvey()
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
