/* ============================================================================
   Tabla staging del proceso de extracción
   Base de datos: SistemaOpiniones_Analitica

   Zona de aterrizaje de la fase E. Recibe los tres canales sin transformar y
   sin restricciones de integridad: todo llega como texto, las claves foráneas
   no se resuelven aquí y nada se rechaza por formato.

   La razón es que extracción y transformación son fases separadas. Si staging
   validara, un cambio de formato en el origen haría perder el dato en vez de
   registrarlo; así el dato siempre queda persistido y la fase T decide.
   ============================================================================ */

USE SistemaOpiniones_Analitica;
GO

IF OBJECT_ID('dbo.Stg_Opinion', 'U') IS NOT NULL
    DROP TABLE dbo.Stg_Opinion;
GO

CREATE TABLE dbo.Stg_Opinion
(
    IdStaging      BIGINT IDENTITY(1,1) NOT NULL,

    -- Trazabilidad: qué corrida y qué canal produjeron esta fila.
    BatchId        NVARCHAR(40)   NOT NULL,
    SourceName     NVARCHAR(60)   NOT NULL,
    SourceType     NVARCHAR(20)   NOT NULL,

    -- Campos crudos. Todos NVARCHAR a propósito: staging no interpreta tipos.
    ExternalId     NVARCHAR(60)   NULL,
    ClienteRef     NVARCHAR(60)   NULL,
    ProductoRef    NVARCHAR(60)   NULL,
    FuenteRef      NVARCHAR(60)   NULL,
    Fecha          NVARCHAR(40)   NULL,
    Comentario     NVARCHAR(MAX)  NULL,
    Puntaje        NVARCHAR(20)   NULL,
    Clasificacion  NVARCHAR(40)   NULL,

    ExtractedAtUtc DATETIME2(3)   NOT NULL,

    -- Payload original de la API; permite recuperar campos no mapeados sin volver a llamarla.
    RawPayload     NVARCHAR(MAX)  NULL,

    CONSTRAINT PK_Stg_Opinion PRIMARY KEY (IdStaging)
);
GO

-- La fase de transformación procesa un lote a la vez y suele filtrar por canal.
CREATE INDEX IX_Stg_Opinion_Batch  ON dbo.Stg_Opinion (BatchId, SourceName);
CREATE INDEX IX_Stg_Opinion_Source ON dbo.Stg_Opinion (SourceType);
GO

/* Control de corridas: una fila por fuente y por lote. Es la evidencia de qué se
   extrajo, cuánto tardó y qué falló, sin depender de leer los archivos de log. */
IF OBJECT_ID('dbo.Etl_Ejecucion', 'U') IS NOT NULL
    DROP TABLE dbo.Etl_Ejecucion;
GO

CREATE TABLE dbo.Etl_Ejecucion
(
    IdEjecucion  INT IDENTITY(1,1) NOT NULL,
    BatchId      NVARCHAR(40)  NOT NULL,
    SourceName   NVARCHAR(60)  NOT NULL,
    SourceType   NVARCHAR(20)  NOT NULL,
    Exitoso      BIT           NOT NULL,
    Extraidos    INT           NOT NULL DEFAULT 0,
    Descartados  INT           NOT NULL DEFAULT 0,
    DuracionMs   BIGINT        NOT NULL DEFAULT 0,
    Mensaje      NVARCHAR(MAX) NULL,
    FechaUtc     DATETIME2(3)  NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Etl_Ejecucion PRIMARY KEY (IdEjecucion)
);
GO

/* Consultas de verificación tras una corrida:

   SELECT SourceName, SourceType, COUNT(*) AS Registros
   FROM dbo.Stg_Opinion
   WHERE BatchId = '<batch>'
   GROUP BY SourceName, SourceType;

   SELECT TOP 20 * FROM dbo.Stg_Opinion WHERE BatchId = '<batch>' ORDER BY IdStaging;
*/
