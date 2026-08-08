using Microsoft.Extensions.Options;
using Serilog;
using SistemaOpiniones.Etl;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Extractors;
using SistemaOpiniones.Etl.Services;
using SistemaOpiniones.Etl.Staging;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Services.AddSerilog(Log.Logger);

builder.Services
    .AddOptions<EtlOptions>()
    .Bind(builder.Configuration.GetSection(EtlOptions.SectionName))
    .ValidateOnStart();

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

        if (!string.IsNullOrWhiteSpace(api.ApiKey))
            client.DefaultRequestHeaders.Add(api.ApiKeyHeaderName, api.ApiKey);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// El destino de staging (archivos o tabla SQL) se elige por configuración.
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
