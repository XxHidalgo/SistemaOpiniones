using System.Configuration;
using AutoMapper;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Enums;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Models.Domain;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Data.Result;

namespace SistemaOpiniones.Data.Services;

public class ClienteService : BaseService<ClientDto, Cliente>, IClienteService
{
    public ClienteService(SistemaOpinionesContext context, IMapper mapper)
        : base(context, mapper, ConfigurationManager.AppSettings["ClientsCsvPath"]!)
    {
    }

    public Task<OperationResult> LoadCliente()
        => LoadAsync();

    protected override Func<Cliente, object>? DistinctKey => c => c.IdCliente;

    public Task<OperationResult> SaveCliente()
        => SaveAsync(MethodDbEnum.EntityFramework);
}
