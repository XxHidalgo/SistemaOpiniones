namespace SistemaOpiniones.Etl.Configuration;

/// <summary>
/// Configuración completa del proceso de extracción, enlazada desde la sección "Etl"
/// de appsettings.json. Rutas, consultas, endpoints y banderas viven aquí; las
/// credenciales NO (ver <see cref="ApiSourceOptions.ApiKey"/> y las cadenas de conexión).
/// </summary>
public sealed class EtlOptions
{
    public const string SectionName = "Etl";

    /// <summary>Si es true el Worker corre una vez y termina; si es false queda en ciclo.</summary>
    public bool RunOnceAndExit { get; set; } = true;

    /// <summary>Minutos entre corridas cuando <see cref="RunOnceAndExit"/> es false.</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Límite de fuentes extrayéndose a la vez. 0 = sin límite (todas en paralelo).
    /// Existe para no saturar la red o el servidor origen si el día de mañana hay
    /// muchas más fuentes registradas.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; }

    public StagingOptions Staging { get; set; } = new();

    public SourcesOptions Sources { get; set; } = new();
}

public enum StagingMode
{
    /// <summary>Aterriza en archivos NDJSON. No requiere base de datos.</summary>
    File,

    /// <summary>Aterriza en la tabla staging vía SqlBulkCopy.</summary>
    SqlServer
}

public sealed class StagingOptions
{
    public StagingMode Mode { get; set; } = StagingMode.File;

    /// <summary>Carpeta destino cuando el modo es File. Relativa al directorio de ejecución.</summary>
    public string Directory { get; set; } = "staging";

    /// <summary>Nombre (no valor) de la cadena de conexión a usar cuando el modo es SqlServer.</summary>
    public string ConnectionStringName { get; set; } = "Analitica";

    public string TableName { get; set; } = "dbo.Stg_Opinion";

    /// <summary>Tamaño del lote de escritura. Acota la memoria y aprovecha SqlBulkCopy.</summary>
    public int BatchSize { get; set; } = 1000;
}

public sealed class SourcesOptions
{
    public CsvSourceOptions Csv { get; set; } = new();

    public DatabaseSourceOptions Database { get; set; } = new();

    public ApiSourceOptions Api { get; set; } = new();
}

/// <summary>Encuestas internas de satisfacción, entregadas como archivo CSV.</summary>
public sealed class CsvSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "EncuestasInternas";

    public string Path { get; set; } = "Data/surveys_part1.csv";

    public string Delimiter { get; set; } = ",";

    /// <summary>Cultura para interpretar fechas y números del archivo.</summary>
    public string Culture { get; set; } = "es-DO";
}

/// <summary>Reseñas publicadas en el sitio web, almacenadas en una BD relacional.</summary>
public sealed class DatabaseSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "ResenasWeb";

    /// <summary>Nombre de la cadena de conexión en ConnectionStrings; nunca el valor.</summary>
    public string ConnectionStringName { get; set; } = "OrigenResenasWeb";

    /// <summary>
    /// Consulta de extracción. Debe devolver las columnas con los alias que espera
    /// el extractor: ExternalId, ClienteRef, ProductoRef, FuenteRef, Fecha, Comentario,
    /// Puntaje, Clasificacion. Tener el SQL en configuración permite ajustar el filtro
    /// incremental sin recompilar.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 60;
}

/// <summary>Comentarios de redes sociales, expuestos por una API REST.</summary>
public sealed class ApiSourceOptions
{
    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "ComentariosRedesSociales";

    public string BaseUrl { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string PageParamName { get; set; } = "_page";

    public string PageSizeParamName { get; set; } = "_limit";

    public int PageSize { get; set; } = 100;

    /// <summary>Tope de páginas por corrida; evita un bucle infinito si la API no pagina bien.</summary>
    public int MaxPages { get; set; } = 10;

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Reintentos ante fallos transitorios (5xx, 429, timeouts), con backoff exponencial.</summary>
    public int RetryCount { get; set; } = 3;

    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// Credencial de la API. NO se escribe en appsettings.json: se inyecta por User Secrets
    /// en desarrollo (dotnet user-secrets set "Etl:Sources:Api:ApiKey" "...") o por variable
    /// de entorno en despliegue (Etl__Sources__Api__ApiKey).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Propiedad del JSON que contiene el arreglo de resultados cuando la respuesta viene
    /// envuelta (ej. "data", "items"). Si está vacía se asume que la raíz ya es un arreglo.
    /// </summary>
    public string ResultsProperty { get; set; } = string.Empty;

    /// <summary>
    /// Correspondencia campo de <c>RawOpinion</c> → propiedad del JSON. Cambiar de proveedor
    /// de API, o adaptarse a un cambio de contrato, no requiere tocar código: basta editar
    /// este diccionario en appsettings.json.
    /// </summary>
    public Dictionary<string, string> FieldMap { get; set; } = new();
}
