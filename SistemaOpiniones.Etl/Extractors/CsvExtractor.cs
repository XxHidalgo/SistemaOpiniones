using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Options;
using SistemaOpiniones.Data.Models.DTO;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Extractors;

// Extrae las encuestas internas desde el archivo CSV con CsvHelper.
public sealed class CsvExtractor : IExtractor
{
    private readonly CsvSourceOptions _options;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IOptions<EtlOptions> options, ILogger<CsvExtractor> logger)
    {
        _options = options.Value.Sources.Csv;
        _logger = logger;
    }

    public string SourceName => _options.Name;

    public string SourceType => "CSV";

    public bool Enabled => _options.Enabled;

    public async IAsyncEnumerable<RawOpinion> ExtractAsync(
        ExtractionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(_options.Path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No se encontró el archivo de encuestas en '{path}'. " +
                "Revisa Etl:Sources:Csv:Path en appsettings.json.", path);
        }

        _logger.LogInformation("Leyendo encuestas desde {Path}", path);

        var configuration = new CsvConfiguration(CultureInfo.GetCultureInfo(_options.Culture))
        {
            Delimiter = _options.Delimiter,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = true,
            MissingFieldFound = null,

            BadDataFound = args =>
                _logger.LogWarning(
                    "Dato mal formado en la fila {Row} de {Source}: {Field}",
                    args.Context.Parser?.Row, SourceName, args.Field),

            // Una fila mala se registra y se salta, no aborta la lectura.
            ReadingExceptionOccurred = args =>
            {
                var row = args.Exception.Context?.Parser?.Row;

                if (args.Exception is TypeConverterException)
                {
                    context.RegisterRejected();
                    _logger.LogWarning(
                        "Fila {Row} descartada en {Source}: {Message}",
                        row, SourceName, args.Exception.Message);
                }
                else
                {
                    _logger.LogWarning(
                        "Fila {Row} con formato irregular en {Source}: {Message}",
                        row, SourceName, args.Exception.Message);
                }

                return false;
            }
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, configuration);

        await foreach (var record in csv.GetRecordsAsync<SurveyDto>(cancellationToken))
        {
            yield return new RawOpinion
            {
                BatchId = context.BatchId,
                SourceName = SourceName,
                SourceType = SourceType,
                ExternalId = record.IdOpinion.ToString(CultureInfo.InvariantCulture),
                ClienteRef = record.IdCliente.ToString(CultureInfo.InvariantCulture),
                ProductoRef = record.IdProducto.ToString(CultureInfo.InvariantCulture),
                FuenteRef = record.Fuente,
                Fecha = record.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Comentario = record.Comentario,
                Puntaje = record.PuntajeSatisfaccion?.ToString(CultureInfo.InvariantCulture),
                Clasificacion = record.Clasificacion,
                ExtractedAtUtc = context.StartedAtUtc
            };
        }
    }
}
