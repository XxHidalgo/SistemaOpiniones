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

public class FuenteDatosService : BaseService<FuenteDatosDto, FuenteDatos>, IFuenteDatosService
{
    public FuenteDatosService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["FuenteDatosCsvPath"]!)
    {
    }

    public Task<OperationResult> LoadFuenteDatos()
        => LoadAsync();

    protected override Func<FuenteDatos, object>? DistinctKey => f => f.IdFuente;

    // El CSV trae el tipo como texto ("Web"); aquí lo resolvemos a su IdTipoFuente.
    protected override async Task ResolveForeignKeysAsync(List<FuenteDatos> entities, List<FuenteDatosDto> dtos)
    {
        var tipos = await Context.TipoFuente
            .ToDictionaryAsync(t => t.Nombre, t => t.IdTipoFuente);

        for (int i = 0; i < entities.Count; i++)
        {
            if (tipos.TryGetValue(dtos[i].TipoFuente, out var id))
                entities[i].IdTipoFuente = id;
        }
    }

    public Task<OperationResult> SaveFuenteDatos()
        => SaveAsync(MethodDbEnum.AdoNet);

    protected override (string ProcedureName, List<SqlParameter> Parameters) BuildInsertCommand(FuenteDatos entity)
        => ("dbo.usp_InsertFuenteDatos", new List<SqlParameter>
        {
            new("@IdFuente", entity.IdFuente),
            new("@IdTipoFuente", entity.IdTipoFuente),
            new("@FechaCarga", entity.FechaCarga)
        });
}
