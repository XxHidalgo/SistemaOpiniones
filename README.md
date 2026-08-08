# Sistema de Análisis de Opiniones de Clientes

Proceso ETL que junta las opiniones de clientes que llegan por tres canales
(encuestas en CSV, reseñas del sitio web en SQL Server y comentarios de redes
sociales por API REST) en una base de datos analítica.

## Proyectos

- `SistemaOpiniones.Etl` — Worker Service (.NET 8) con la fase de extracción.
- `SistemaOpiniones.Data` — modelo, DTOs, contexto EF Core y servicios de datos.
- `SistemaOpiniones.Load` — consola que carga los CSV al modelo analítico.

## Entregables

Están en la carpeta [Entregables](Entregables): script de la base de datos,
documento técnico de la extracción y los diagramas de arquitectura y de flujo.

## Cómo ejecutar

```bash
dotnet build SistemaOpiniones.sln
dotnet run --project SistemaOpiniones.Etl
```

Por defecto corre una sola vez y el staging va a archivos NDJSON en
`SistemaOpiniones.Etl/output/staging/`. Los logs quedan en `SistemaOpiniones.Etl/logs/`.

## Base de datos

Ejecutar en orden en SQL Server:

1. `Entregables/01_Script_SistemaOpiniones_Analitica.sql` — modelo analítico.
2. `SistemaOpiniones.Etl/Sql/01_Staging_Schema.sql` — tablas de staging.
3. `SistemaOpiniones.Etl/Sql/02_Origen_ResenasWeb.sql` — base de origen de reseñas web.

Para que el staging vaya a la base de datos en vez de archivos, cambiar
`Etl:Staging:Mode` a `SqlServer` en `SistemaOpiniones.Etl/appsettings.json`.
