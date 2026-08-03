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
