# Sistema de Análisis de Opiniones de Clientes

Proceso ETL que consolida las opiniones de clientes de una empresa de comercio
electrónico recogidas por tres canales distintos, en una base de datos analítica con
modelo en estrella.

## Proyectos

| Proyecto | Framework | Descripción |
|---|---|---|
| `SistemaOpiniones.Etl` | .NET 8 (Worker Service) | **Fase de extracción (E).** Lee las tres fuentes y aterriza el dato crudo en staging. |
| `SistemaOpiniones.Data` | .NET 8 (biblioteca) | Modelo de dominio, DTO, contexto EF Core y servicios de la capa de datos. |
| `SistemaOpiniones.Load` | .NET 8 (consola) | Cargador de la actividad anterior: pobla el modelo analítico desde archivos CSV. |

## Entregables

| Actividad | Entregable |
|---|---|
| **1 — Arquitectura y extracción** | [Documento técnico (PDF)](Entregables/03_Documento_Tecnico_Extraccion.pdf) · [versión Markdown](Entregables/03_Documento_Tecnico_Extraccion.md) |
| | [Diagrama de arquitectura](Entregables/04_Diagrama_Arquitectura.svg) (SVG · [PNG](Entregables/04_Diagrama_Arquitectura.png)) |
| | [Diagrama de flujo del ETL](Entregables/05_Diagrama_Flujo_ETL.svg) (SVG · [PNG](Entregables/05_Diagrama_Flujo_ETL.png)) |
| | Código fuente: [`SistemaOpiniones.Etl/`](SistemaOpiniones.Etl) |
| **3.1 — Modelado de la BD** | [Script SQL](Entregables/01_Script_SistemaOpiniones_Analitica.sql) · [Documento de decisiones (PDF)](Entregables/02_Documento_Decisiones_Diseno.pdf) |

Los diagramas están en SVG: se ven directamente en GitHub y se pueden abrir y editar en
[draw.io](https://app.diagrams.net) mediante *Archivo → Importar*.

## Arquitectura de la extracción

```
  Encuestas (CSV) ─┐
  Reseñas (BD SQL) ─┼─► [ Worker Service .NET 8 ] ─► staging ─► (fase T/L) ─► BD analítica ─► Dashboard
  Comentarios (API) ┘        IExtractor ×3              NDJSON o Stg_Opinion
```

Cada fuente implementa `IExtractor` y se extrae en paralelo. El destino de staging
(`IStagingWriter`) se elige por configuración: archivos NDJSON o tabla `dbo.Stg_Opinion`.

El detalle y la justificación están en
[`Entregables/03_Documento_Tecnico_Extraccion.md`](Entregables/03_Documento_Tecnico_Extraccion.md).

## Requisitos

- SDK de .NET 8 o superior (el proyecto tiene `RollForward=LatestMajor`, corre sobre .NET 9/10).
- SQL Server, solo para la fuente de base de datos y para el modo de staging `SqlServer`.

## Ejecutar

```bash
dotnet build SistemaOpiniones.sln
dotnet run --project SistemaOpiniones.Etl
```

Con la configuración por defecto la corrida es única (`RunOnceAndExit: true`), el staging
va a archivos y no hace falta base de datos: las encuestas se leen de la muestra en
`SistemaOpiniones.Etl/Data/` y los comentarios de una API pública de prueba.

Salidas:

- `SistemaOpiniones.Etl/output/staging/{lote}/{fuente}.ndjson` — dato extraído.
- `SistemaOpiniones.Etl/logs/etl-{fecha}.log` — traza completa.

El proceso devuelve código de salida `1` si alguna fuente falló, para que un job
programado lo detecte.

## Configuración

Todo vive en `SistemaOpiniones.Etl/appsettings.json`, bajo la sección `Etl`.

| Clave | Para qué |
|---|---|
| `RunOnceAndExit` | `true` = una corrida y termina; `false` = ciclo cada `IntervalMinutes`. |
| `MaxDegreeOfParallelism` | Fuentes simultáneas. `0` = todas a la vez. |
| `Staging.Mode` | `File` (NDJSON) o `SqlServer` (tabla staging vía `SqlBulkCopy`). |
| `Sources.*.Enabled` | Apaga o enciende un canal sin tocar código. |
| `Sources.Csv.Path` | Ruta del archivo de encuestas. |
| `Sources.Database.Query` | SQL de extracción. Debe usar los alias `ExternalId`, `ClienteRef`, `ProductoRef`, `FuenteRef`, `Fecha`, `Comentario`, `Puntaje`, `Clasificacion`. |
| `Sources.Api.FieldMap` | Correspondencia campo destino → propiedad JSON. Cambiar de API no requiere recompilar. |

### Credenciales

Nunca se escriben en `appsettings.json`. En desarrollo van en User Secrets:

```bash
cd SistemaOpiniones.Etl
dotnet user-secrets set "ConnectionStrings:OrigenResenasWeb" "Server=TU-SERVIDOR;Database=SistemaOpiniones_Origen;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
dotnet user-secrets set "Etl:Sources:Api:ApiKey" "..."
```

En despliegue, por variables de entorno con doble guion bajo:
`ConnectionStrings__OrigenResenasWeb`, `Etl__Sources__Api__ApiKey`.

## Base de datos

Ejecutar en orden desde SQL Server Management Studio:

1. `Entregables/01_Script_SistemaOpiniones_Analitica.sql` — modelo analítico en estrella.
2. `SistemaOpiniones.Etl/Sql/01_Staging_Schema.sql` — tablas `Stg_Opinion` y `Etl_Ejecucion`.
3. `SistemaOpiniones.Etl/Sql/02_Origen_ResenasWeb.sql` — base de origen de las reseñas web.

Luego apuntar las cadenas de conexión al servidor y, si se quiere staging en base de
datos, cambiar `Etl:Staging:Mode` a `SqlServer`.

## Datos de origen

`SistemaOpiniones.Etl/Data/surveys_part1.csv` es una **muestra** para poder ejecutar y
evidenciar el proceso. Los archivos reales no se versionan (ver `.gitignore`): se colocan
localmente y se apunta `Sources.Csv.Path` a ellos.
