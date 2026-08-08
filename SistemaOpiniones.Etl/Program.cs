using Microsoft.Extensions.Options;
using Serilog;
using SistemaOpiniones.Etl;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Extractors;
using SistemaOpiniones.Etl.Services;
using SistemaOpiniones.Etl.Staging;

var builder = Host.CreateApplicationBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────────────
// Serilog se conecta detrás de ILogger: el resto del código sigue dependiendo solo
// de la abstracción. Los sinks (consola y archivo diario) se declaran en appsettings.json.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Services.AddSerilog(Log.Logger);

// ── Configuración ──────────────────────────────────────────────────────────────
// Rutas, consultas y endpoints salen de appsettings.json. Las credenciales llegan por
// User Secrets (desarrollo) o variables de entorno (despliegue), que el host ya incluye
// en la cadena de proveedores y sobrescriben lo que haya en el archivo.
builder.Services
    .AddOptions<EtlOptions>()
    .Bind(builder.Configuration.GetSection(EtlOptions.SectionName))
    .ValidateOnStart();

// ── Fuentes de datos ───────────────────────────────────────────────────────────
// Las tres se registran contra la misma abstracción; el orquestador las recibe como
// colección y no sabe cuáles son. Sumar una cuarta es agregar una línea aquí.
builder.Services.AddTransient<IExtractor, CsvExtractor>();
builder.Services.AddTransient<IExtractor, DatabaseExtractor>();
builder.Services.AddTransient<IExtractor, ApiExtractor>();

builder.Services
    .AddHttpClient(ApiExtractor.HttpClientName, (serviceProvider, client) =>
    {
        var api = serviceProvider.GetRequiredService<IOptions<EtlOptions>>().Value.Sources.Api;

        if (!string.IsNullOrWhiteSpace(api.BaseUrl))
            client.BaseAddress = new Uri(api.BaseUrl, UriKind.Absolute);

        client.Timeout = TimeSpan.FromSeconds(api.TimeoutSeconds);
        client.DefaultRequestHeaders.Accept.Add(new("application/json"));

        // La llave nunca se registra en logs ni se guarda en appsettings.json.
        if (!string.IsNullOrWhiteSpace(api.ApiKey))
            client.DefaultRequestHeaders.Add(api.ApiKeyHeaderName, api.ApiKey);
    })
    // Recicla el handler periódicamente para que los cambios de DNS se tomen en cuenta.
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// ── Destino de staging ─────────────────────────────────────────────────────────
// Archivos o tabla staging según configuración; los extractores no se enteran de cuál.
builder.Services.AddSingleton<IStagingWriter>(serviceProvider =>
{
    var staging = serviceProvider.GetRequiredService<IOptions<EtlOptions>>().Value.Staging;

    return staging.Mode switch
    {
        StagingMode.SqlServer => ActivatorUtilities.CreateInstance<SqlStagingWriter>(serviceProvider),
        _ => ActivatorUtilities.CreateInstance<FileStagingWriter>(serviceProvider)
    };
});

builder.Services.AddScoped<ExtractionOrchestrator>();
builder.Services.AddHostedService<Worker>();

// Permite instalarlo como servicio de Windows sin cambiar el código.
builder.Services.AddWindowsService(options => options.ServiceName = "SistemaOpiniones ETL");

try
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El servicio ETL no pudo iniciar.");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
