-- Tabla staging del proceso de extracción (BD analítica).
-- Todo llega como texto y sin validar; la fase de transformación decide después.

USE SistemaOpiniones_Analitica;
GO

IF OBJECT_ID('dbo.Stg_Opinion', 'U') IS NOT NULL
    DROP TABLE dbo.Stg_Opinion;
GO

CREATE TABLE dbo.Stg_Opinion
(
    IdStaging      BIGINT IDENTITY(1,1) NOT NULL,

    BatchId        NVARCHAR(40)   NOT NULL,
    SourceName     NVARCHAR(60)   NOT NULL,
    SourceType     NVARCHAR(20)   NOT NULL,

    ExternalId     NVARCHAR(60)   NULL,
    ClienteRef     NVARCHAR(60)   NULL,
    ProductoRef    NVARCHAR(60)   NULL,
    FuenteRef      NVARCHAR(60)   NULL,
    Fecha          NVARCHAR(40)   NULL,
    Comentario     NVARCHAR(MAX)  NULL,
    Puntaje        NVARCHAR(20)   NULL,
    Clasificacion  NVARCHAR(40)   NULL,

    ExtractedAtUtc DATETIME2(3)   NOT NULL,

    -- JSON original de la API, por si hace falta un campo no mapeado.
    RawPayload     NVARCHAR(MAX)  NULL,

    CONSTRAINT PK_Stg_Opinion PRIMARY KEY (IdStaging)
);
GO

CREATE INDEX IX_Stg_Opinion_Batch  ON dbo.Stg_Opinion (BatchId, SourceName);
CREATE INDEX IX_Stg_Opinion_Source ON dbo.Stg_Opinion (SourceType);
GO

-- Control de corridas: una fila por fuente y por lote.
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
