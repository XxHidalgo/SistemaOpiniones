-- BD de origen con las reseñas del sitio web (fuente relacional del ETL).
-- Ejecutar antes de habilitar la fuente Etl:Sources:Database.

IF DB_ID('SistemaOpiniones_Origen') IS NULL
    CREATE DATABASE SistemaOpiniones_Origen;
GO

USE SistemaOpiniones_Origen;
GO

IF OBJECT_ID('dbo.WebReview', 'U') IS NOT NULL
    DROP TABLE dbo.WebReview;
GO

-- Mismo esquema que web_reviews.csv
CREATE TABLE dbo.WebReview
(
    IdReview   NVARCHAR(20)   NOT NULL,
    IdCliente  NVARCHAR(20)   NULL,
    IdProducto NVARCHAR(20)   NULL,
    Fecha      DATE           NOT NULL,
    Comentario NVARCHAR(1000) NULL,
    Rating     INT            NULL,

    CONSTRAINT PK_WebReview PRIMARY KEY (IdReview)
);
GO

CREATE INDEX IX_WebReview_Fecha ON dbo.WebReview (Fecha);
GO

-- Datos de muestra para probar la extracción.
INSERT INTO dbo.WebReview (IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating)
VALUES
    (N'R001', N'C007', N'P016', '2025-01-14', N'El producto llegó antes de lo esperado y en buen estado.', 5),
    (N'R002', N'C012', N'P003', '2025-01-16', N'Cumple lo que promete, aunque el empaque venía maltratado.', 4),
    (N'R003', N'C019', N'P016', '2025-01-21', N'No es lo que esperaba, la descripción es confusa.', 2),
    (N'R004', N'C004', N'P008', '2025-02-02', N'Excelente relación precio-calidad.', 5),
    (N'R005', N'C031', N'P022', '2025-02-05', N'Dejó de funcionar a la semana. Muy decepcionado.', 1),
    (N'R006', N'C007', N'P008', '2025-02-11', N'Funciona bien, nada extraordinario.', 3),
    (N'R007', N'C045', N'P003', '2025-02-18', N'Buen producto pero el envío tardó tres semanas.', 3),
    (N'R008', N'C012', N'P030', '2025-03-01', N'Superó mis expectativas, lo volvería a comprar.', 5),
    (N'R009', NULL,    N'P022', '2025-03-07', N'Calidad aceptable para el precio.', 4),
    (N'R010', N'C028', N'P016', '2025-03-15', N'Llegó incompleto, faltaban piezas.', 1),
    (N'R011', N'C033', N'P008', '2025-03-19', N'Muy cómodo y fácil de usar.', 5),
    (N'R012', N'C019', N'P030', '2025-03-24', N'El color no coincide con el de las fotos.', 2),
    (N'R013', N'C050', N'P003', '2025-04-02', N'Recomendado, buen soporte del vendedor.', 4),
    (N'R014', N'C004', N'P022', '2025-04-09', N'Regular. Esperaba más por lo que costó.', 3),
    (N'R015', N'C045', N'P016', '2025-04-17', N'Perfecto, sin quejas.', 5);
GO

SELECT COUNT(*) AS ResenasCargadas FROM dbo.WebReview;
GO
