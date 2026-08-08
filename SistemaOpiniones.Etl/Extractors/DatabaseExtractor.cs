using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Extractors;

/// <summary>
/// Extrae las reseñas del sitio web desde la base de datos relacional de origen.
///
/// La consulta vive en appsettings.json y debe exponer sus columnas con los alias que
/// esperan los campos de <see cref="RawOpinion"/>. El lector resuelve los ordinales una
/// sola vez y tolera columnas ausentes, de modo que un cambio menor en el origen no
/// tumba la extracción.
///
/// Rendimiento: se usa un DataReader en modo streaming; las filas se ceden una a una y
/// nunca se materializa el resultado completo en memoria.
/// </summary>
public sealed class DatabaseExtractor : IExtractor
{
    private static readonly string[] Columns =
    [
        "ExternalId", "ClienteRef", "ProductoRef", "FuenteRef",
        "Fecha", "Comentario", "Puntaje", "Clasificacion"
    ];

    private readonly DatabaseSourceOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(
        IOptions<EtlOptions> options,
        IConfiguration configuration,
        ILogger<DatabaseExtractor> logger)
    {
        _options = options.Value.Sources.Database;
        _configuration = configuration;
        _logger = logger;
    }

    public string SourceName => _options.Name;

    public string SourceType => "Database";

    public bool Enabled => _options.Enabled;

    public async IAsyncEnumerable<RawOpinion> ExtractAsync(
        ExtractionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString(_options.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No hay cadena de conexión '{_options.ConnectionStringName}'. " +
                "Defínela en User Secrets o en la variable de entorno " +
                $"ConnectionStrings__{_options.ConnectionStringName}.");
        }

        if (string.IsNullOrWhiteSpace(_options.Query))
        {
            throw new InvalidOperationException(
                "Etl:Sources:Database:Query está vacía; no hay nada que extraer.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        _logger.LogInformation(
            "Conectado a {Database} en {Server} para extraer reseñas web",
            connection.Database, connection.DataSource);

        await using var command = new SqlCommand(_options.Query, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = _options.CommandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var ordinals = MapOrdinals(reader);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new RawOpinion
            {
                BatchId = context.BatchId,
                SourceName = SourceName,
                SourceType = SourceType,
                ExternalId = Read(reader, ordinals, "ExternalId"),
                ClienteRef = Read(reader, ordinals, "ClienteRef"),
                ProductoRef = Read(reader, ordinals, "ProductoRef"),
                FuenteRef = Read(reader, ordinals, "FuenteRef"),
                Fecha = Read(reader, ordinals, "Fecha"),
                Comentario = Read(reader, ordinals, "Comentario"),
                Puntaje = Read(reader, ordinals, "Puntaje"),
                Clasificacion = Read(reader, ordinals, "Clasificacion"),
                ExtractedAtUtc = context.StartedAtUtc
            };
        }
    }

    /// <summary>Resuelve una sola vez la posición de cada columna esperada; -1 si no viene.</summary>
    private Dictionary<string, int> MapOrdinals(SqlDataReader reader)
    {
        var present = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
            present[reader.GetName(i)] = i;

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in Columns)
        {
            map[column] = present.TryGetValue(column, out var ordinal) ? ordinal : -1;

            if (map[column] < 0)
                _logger.LogDebug("La consulta de {Source} no devuelve la columna {Column}", SourceName, column);
        }

        return map;
    }

    private static string? Read(SqlDataReader reader, Dictionary<string, int> ordinals, string column)
    {
        var ordinal = ordinals[column];

        if (ordinal < 0 || reader.IsDBNull(ordinal))
            return null;

        // Se convierte a texto sin interpretar: staging guarda el valor crudo.
        return reader.GetValue(ordinal) switch
        {
            DateTime date => date.ToString("yyyy-MM-dd"),
            var value => value.ToString()
        };
    }
}
