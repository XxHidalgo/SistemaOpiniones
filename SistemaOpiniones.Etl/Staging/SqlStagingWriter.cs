using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Staging;

// Guarda lo extraído en la tabla staging con SqlBulkCopy por lotes.
public sealed class SqlStagingWriter : IStagingWriter
{
    private readonly StagingOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlStagingWriter> _logger;

    public SqlStagingWriter(
        IOptions<EtlOptions> options,
        IConfiguration configuration,
        ILogger<SqlStagingWriter> logger)
    {
        _options = options.Value.Staging;
        _configuration = configuration;
        _logger = logger;
    }

    public string Describe(string sourceName, ExtractionContext context) => _options.TableName;

    public async Task<int> WriteAsync(
        string sourceName,
        ExtractionContext context,
        IAsyncEnumerable<RawOpinion> records,
        CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString(_options.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No hay cadena de conexión '{_options.ConnectionStringName}' para escribir en staging.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = _options.TableName,
            BatchSize = _options.BatchSize,
            BulkCopyTimeout = 0
        };

        var table = CreateTable();

        foreach (DataColumn column in table.Columns)
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        var written = 0;

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            AddRow(table, record);

            if (table.Rows.Count >= _options.BatchSize)
                written += await FlushAsync(bulkCopy, table, sourceName, cancellationToken);
        }

        if (table.Rows.Count > 0)
            written += await FlushAsync(bulkCopy, table, sourceName, cancellationToken);

        return written;
    }

    private async Task<int> FlushAsync(
        SqlBulkCopy bulkCopy, DataTable table, string sourceName, CancellationToken cancellationToken)
    {
        var count = table.Rows.Count;

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
        table.Clear();

        _logger.LogDebug("{Source}: {Count} registros volcados a {Table}", sourceName, count, _options.TableName);

        return count;
    }

    // Debe coincidir con las columnas de dbo.Stg_Opinion (IdStaging es IDENTITY, no se envía).
    private static DataTable CreateTable()
    {
        var table = new DataTable();

        table.Columns.Add("BatchId", typeof(string));
        table.Columns.Add("SourceName", typeof(string));
        table.Columns.Add("SourceType", typeof(string));
        table.Columns.Add("ExternalId", typeof(string));
        table.Columns.Add("ClienteRef", typeof(string));
        table.Columns.Add("ProductoRef", typeof(string));
        table.Columns.Add("FuenteRef", typeof(string));
        table.Columns.Add("Fecha", typeof(string));
        table.Columns.Add("Comentario", typeof(string));
        table.Columns.Add("Puntaje", typeof(string));
        table.Columns.Add("Clasificacion", typeof(string));
        table.Columns.Add("ExtractedAtUtc", typeof(DateTime));
        table.Columns.Add("RawPayload", typeof(string));

        return table;
    }

    private static void AddRow(DataTable table, RawOpinion record)
    {
        var row = table.NewRow();

        row["BatchId"] = record.BatchId;
        row["SourceName"] = record.SourceName;
        row["SourceType"] = record.SourceType;
        row["ExternalId"] = (object?)record.ExternalId ?? DBNull.Value;
        row["ClienteRef"] = (object?)record.ClienteRef ?? DBNull.Value;
        row["ProductoRef"] = (object?)record.ProductoRef ?? DBNull.Value;
        row["FuenteRef"] = (object?)record.FuenteRef ?? DBNull.Value;
        row["Fecha"] = (object?)record.Fecha ?? DBNull.Value;
        row["Comentario"] = (object?)record.Comentario ?? DBNull.Value;
        row["Puntaje"] = (object?)record.Puntaje ?? DBNull.Value;
        row["Clasificacion"] = (object?)record.Clasificacion ?? DBNull.Value;
        row["ExtractedAtUtc"] = record.ExtractedAtUtc;
        row["RawPayload"] = (object?)record.RawPayload ?? DBNull.Value;

        table.Rows.Add(row);
    }
}
