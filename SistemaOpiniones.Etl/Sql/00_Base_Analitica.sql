CREATE DATABASE SistemaOpiniones_Analitica;
GO

USE SistemaOpiniones_Analitica;
GO

CREATE TABLE dbo.Categoria
(
    IdCategoria INT IDENTITY(1,1) NOT NULL,
    Nombre      NVARCHAR(50)      NOT NULL,

    CONSTRAINT PK_Categoria PRIMARY KEY (IdCategoria),
    CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.TipoFuente
(
    IdTipoFuente INT IDENTITY(1,1) NOT NULL,
    Nombre       NVARCHAR(20)      NOT NULL,

    CONSTRAINT PK_TipoFuente PRIMARY KEY (IdTipoFuente),
    CONSTRAINT UQ_TipoFuente_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Clasificacion
(
    IdClasificacion INT IDENTITY(1,1) NOT NULL,
    Nombre          NVARCHAR(10)      NOT NULL,

    CONSTRAINT PK_Clasificacion PRIMARY KEY (IdClasificacion),
    CONSTRAINT UQ_Clasificacion_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Cliente
(
    IdCliente INT           NOT NULL,
    Nombre    NVARCHAR(100) NOT NULL,
    Email     NVARCHAR(256) NULL,

    CONSTRAINT PK_Cliente PRIMARY KEY (IdCliente)
);
GO

CREATE TABLE dbo.Producto
(
    IdProducto  INT           NOT NULL,
    Nombre      NVARCHAR(100) NOT NULL,
    IdCategoria INT           NOT NULL,

    CONSTRAINT PK_Producto PRIMARY KEY (IdProducto),
    CONSTRAINT FK_Producto_Categoria
        FOREIGN KEY (IdCategoria) REFERENCES dbo.Categoria (IdCategoria)
);
GO

CREATE TABLE dbo.FuenteDatos
(
    IdFuente     NVARCHAR(10) NOT NULL,
    IdTipoFuente INT          NOT NULL,
    FechaCarga   DATE         NOT NULL,

    CONSTRAINT PK_FuenteDatos PRIMARY KEY (IdFuente),
    CONSTRAINT FK_FuenteDatos_TipoFuente
        FOREIGN KEY (IdTipoFuente) REFERENCES dbo.TipoFuente (IdTipoFuente)
);
GO

CREATE TABLE dbo.Opinion
(
    IdOpinion           INT IDENTITY(1,1) NOT NULL,
    IdCliente           INT               NULL,
    IdProducto          INT               NULL,
    IdFuente            NVARCHAR(10)      NULL,
    IdClasificacion     INT               NULL,
    Fecha               DATE              NOT NULL,
    Comentario          NVARCHAR(1000)    NULL,
    PuntajeSatisfaccion TINYINT           NULL,

    CONSTRAINT PK_Opinion PRIMARY KEY (IdOpinion),
    CONSTRAINT FK_Opinion_Cliente
        FOREIGN KEY (IdCliente) REFERENCES dbo.Cliente (IdCliente),
    CONSTRAINT FK_Opinion_Producto
        FOREIGN KEY (IdProducto) REFERENCES dbo.Producto (IdProducto),
    CONSTRAINT FK_Opinion_Fuente
        FOREIGN KEY (IdFuente) REFERENCES dbo.FuenteDatos (IdFuente),
    CONSTRAINT FK_Opinion_Clasificacion
        FOREIGN KEY (IdClasificacion) REFERENCES dbo.Clasificacion (IdClasificacion)
);
GO

CREATE PROCEDURE dbo.usp_InsertOpinion
    @IdCliente           INT            = NULL,
    @IdProducto          INT            = NULL,
    @IdFuente            NVARCHAR(10)   = NULL,
    @IdClasificacion     INT            = NULL,
    @Fecha               DATE,
    @Comentario          NVARCHAR(1000) = NULL,
    @PuntajeSatisfaccion TINYINT        = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Opinion
        (IdCliente, IdProducto, IdFuente, IdClasificacion,
         Fecha, Comentario, PuntajeSatisfaccion)
    VALUES
        (@IdCliente, @IdProducto, @IdFuente, @IdClasificacion,
         @Fecha, @Comentario, @PuntajeSatisfaccion);
END
GO

CREATE PROCEDURE dbo.usp_InsertProducto
    @IdProducto  INT,
    @Nombre      NVARCHAR(100),
    @IdCategoria INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Producto (IdProducto, Nombre, IdCategoria)
    VALUES (@IdProducto, @Nombre, @IdCategoria);
END
GO

CREATE PROCEDURE dbo.usp_InsertFuenteDatos
    @IdFuente     NVARCHAR(10),
    @IdTipoFuente INT,
    @FechaCarga   DATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.FuenteDatos (IdFuente, IdTipoFuente, FechaCarga)
    VALUES (@IdFuente, @IdTipoFuente, @FechaCarga);
END
GO

-- ============================================================
-- Procesos de limpieza previos a la carga
-- ============================================================

-- Vacía la tabla de hechos y reinicia su identity, para que una
-- recarga vuelva a numerar las opiniones desde 1.
CREATE PROCEDURE dbo.usp_LimpiarHechos
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Filas INT = (SELECT COUNT(*) FROM dbo.Opinion);

    DELETE FROM dbo.Opinion;
    DBCC CHECKIDENT ('dbo.Opinion', RESEED, 0) WITH NO_INFOMSGS;

    SELECT 'Opinion' AS Tabla, @Filas AS FilasEliminadas;
END
GO

-- Vacía las dimensiones. Se borran en orden inverso a las FK
-- (primero las que dependen de otras) para no violar restricciones.
-- Requiere que los hechos ya estén limpios.
CREATE PROCEDURE dbo.usp_LimpiarDimensiones
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Opinion)
    BEGIN
        RAISERROR ('No se pueden limpiar las dimensiones: la tabla de hechos Opinion todavía tiene filas. Ejecute dbo.usp_LimpiarHechos primero.', 16, 1);
        RETURN;
    END

    DECLARE @Conteos TABLE (Tabla SYSNAME, FilasEliminadas INT);

    INSERT INTO @Conteos
    SELECT 'Producto',      COUNT(*) FROM dbo.Producto
    UNION ALL SELECT 'FuenteDatos',   COUNT(*) FROM dbo.FuenteDatos
    UNION ALL SELECT 'Cliente',       COUNT(*) FROM dbo.Cliente
    UNION ALL SELECT 'Categoria',     COUNT(*) FROM dbo.Categoria
    UNION ALL SELECT 'TipoFuente',    COUNT(*) FROM dbo.TipoFuente
    UNION ALL SELECT 'Clasificacion', COUNT(*) FROM dbo.Clasificacion;

    DELETE FROM dbo.Producto;
    DELETE FROM dbo.FuenteDatos;
    DELETE FROM dbo.Cliente;
    DELETE FROM dbo.Categoria;
    DELETE FROM dbo.TipoFuente;
    DELETE FROM dbo.Clasificacion;

    DBCC CHECKIDENT ('dbo.Categoria',     RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.TipoFuente',    RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Clasificacion', RESEED, 0) WITH NO_INFOMSGS;

    SELECT Tabla, FilasEliminadas FROM @Conteos;
END
GO

-- Punto de entrada que usa la aplicación de carga.
-- @SoloHechos = 1 limpia únicamente la tabla de hechos y deja las
-- dimensiones intactas (recarga incremental de hechos).
CREATE PROCEDURE dbo.usp_LimpiarDataWarehouse
    @SoloHechos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    EXEC dbo.usp_LimpiarHechos;

    IF @SoloHechos = 0
        EXEC dbo.usp_LimpiarDimensiones;

    COMMIT TRANSACTION;
END
GO
