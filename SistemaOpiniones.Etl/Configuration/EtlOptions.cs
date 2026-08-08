namespace SistemaOpiniones.Etl.Configuration;

// Configuración del proceso de extracción (sección "Etl" de appsettings.json).
public sealed class EtlOptions
{
    public const string SectionName = "Etl";

    // true = corre una vez y termina; false = queda en ciclo cada IntervalMinutes.
    public bool RunOnceAndExit { get; set; } = true;

    public int IntervalMinutes { get; set; } = 60;

    // 0 = todas las fuentes en paralelo.
    public int MaxDegreeOfParallelism { get; set; }

    public StagingOptions Staging { get; set; } = new();

    public SourcesOptions Sources { get; set; } = new();
}

public enum StagingMode
{
    File,
    SqlServer
}

public sealed class StagingOptions
{
    public StagingMode Mode { get; set; } = StagingMode.File;

    public string Directory { get; set; } = "staging";

    public string ConnectionStringName { get; set; } = "Analitica";

    public string TableName { get; set; } = "dbo.Stg_Opinion";

    public int BatchSize { get; set; } = 1000;
}

public sealed class SourcesOptions
{
    public CsvSourceOptions Csv { get; set; } = new();

    public DatabaseSourceOptions Database { get; set; } = new();

    public ApiSourceOptions Api { get; set; } = new();
}

public sealed class CsvSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "EncuestasInternas";

    public string Path { get; set; } = "Data/surveys_part1.csv";

    public string Delimiter { get; set; } = ",";

    public string Culture { get; set; } = "es-DO";
}

public sealed class DatabaseSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "ResenasWeb";

    public string ConnectionStringName { get; set; } = "OrigenResenasWeb";

    // Debe devolver las columnas con los alias que espera el extractor.
    public string Query { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 60;
}

public sealed class ApiSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "ComentariosRedesSociales";

    public string BaseUrl { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string PageParamName { get; set; } = "_page";

    public string PageSizeParamName { get; set; } = "_limit";

    public int PageSize { get; set; } = 100;

    public int MaxPages { get; set; } = 10;

    public int TimeoutSeconds { get; set; } = 30;

    public int RetryCount { get; set; } = 3;

    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

    // Se define por User Secrets o variable de entorno, no en appsettings.json.
    public string? ApiKey { get; set; }

    // Propiedad que envuelve el arreglo de resultados; vacía si la raíz ya es un arreglo.
    public string ResultsProperty { get; set; } = string.Empty;

    // Campo de RawOpinion -> propiedad del JSON de la API.
    public Dictionary<string, string> FieldMap { get; set; } = new();
}
