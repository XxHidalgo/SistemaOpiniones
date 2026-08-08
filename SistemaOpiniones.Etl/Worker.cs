using Microsoft.Extensions.Options;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Services;

namespace SistemaOpiniones.Etl;

/// <summary>
/// Servicio en segundo plano que hospeda el proceso de extracción.
///
/// Según configuración corre una sola vez y termina (útil para ejecutarlo desde el
/// Programador de tareas o un job de CI) o queda en ciclo con un intervalo fijo, que es
/// el modo de servicio permanente.
///
/// Cada corrida se resuelve en su propio ámbito de inyección de dependencias, para que
/// las conexiones y los extractores no sobrevivan entre ejecuciones.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly EtlOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        IOptions<EtlOptions> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Servicio ETL iniciado. Modo: {Mode}. Destino de staging: {Staging}",
            _options.RunOnceAndExit ? "corrida única" : $"cíclico cada {_options.IntervalMinutes} min",
            _options.Staging.Mode);

        try
        {
            if (_options.RunOnceAndExit)
            {
                await RunOnceAsync(stoppingToken);
                return;
            }

            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
            using var timer = new PeriodicTimer(interval);

            do
            {
                await RunOnceAsync(stoppingToken);
                _logger.LogInformation("Próxima corrida en {Interval}.", interval);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Extracción detenida por solicitud de apagado.");
        }
        catch (Exception ex)
        {
            // Falla que ningún extractor pudo absorber (configuración inválida, destino de
            // staging inaccesible). Se marca el proceso como fallido para que quien lo
            // programe se entere.
            _logger.LogCritical(ex, "El servicio ETL terminó de forma inesperada.");
            Environment.ExitCode = 1;
        }
        finally
        {
            if (_options.RunOnceAndExit)
                _lifetime.StopApplication();
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var orchestrator = scope.ServiceProvider.GetRequiredService<ExtractionOrchestrator>();
        var results = await orchestrator.RunAsync(cancellationToken);

        if (results.Any(r => !r.Success))
            Environment.ExitCode = 1;
    }
}
