using AutoMapper;
using SistemaOpiniones.Data.Context;
using SistemaOpiniones.Data.Interfaces;
using SistemaOpiniones.Data.Mappings;
using SistemaOpiniones.Data.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace SistemaOpiniones.Load;

public static class AppHost
{
    public static IHost CreateHost(string[] args)
    {
        var connectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["SistemaOpinionesDb"].ConnectionString;

        return Host.CreateDefaultBuilder(args)
            // EF Core registra cada comando SQL en consola y tapa el resumen de la
            // carga; se deja solo a partir de advertencias.
            .ConfigureLogging(logging =>
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning))
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<SistemaOpinionesContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });

                services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfiles>());

                // Catálogos (valores únicos derivados de otros CSV)
                services.AddTransient<ICategoriaService, CategoriaService>();
                services.AddTransient<ITipoFuenteService, TipoFuenteService>();
                services.AddTransient<IClasificacionService, ClasificacionService>();

                // Entidades principales
                services.AddTransient<IClienteService, ClienteService>();
                services.AddTransient<IProductoService, ProductoService>();
                services.AddTransient<IFuenteDatosService, FuenteDatosService>();
                services.AddTransient<ISocialCommentService, SocialCommentService>();
                services.AddTransient<IWebReviewService, WebReviewService>();
                services.AddTransient<ISurveyService, SurveyService>();
            })
            .Build();
    }
}
