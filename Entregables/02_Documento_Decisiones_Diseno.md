# Actividad 3.1 – Modelado de la Base de Datos

## Sistema de Análisis de Opiniones de Clientes

**Base de datos:** `SistemaOpiniones_Analitica` (Microsoft SQL Server)

---

## 1. Objetivo

Diseñar la base de datos analítica destino del proceso ETL que consolida las opiniones de clientes de una empresa de comercio electrónico, recopiladas desde tres canales distintos:

| Canal | Origen técnico | Contenido |
|---|---|---|
| Encuestas internas | Archivos CSV | Resultados de encuestas de satisfacción |
| Reseñas web | Base de datos relacional | Reseñas publicadas en el sitio web |
| Redes sociales | API REST | Comentarios de usuarios en redes sociales |

El modelo debe permitir responder indicadores clave: total de comentarios por periodo o producto, clasificación de opiniones (positiva / negativa / neutra), porcentaje de satisfacción por producto y tendencias de opinión en el tiempo.

---

## 2. Estructura elegida: modelo tipo estrella

Se optó por un **esquema en estrella**, con una tabla de hechos central y dimensiones a su alrededor:

```
                Cliente          Producto ── Categoria
                    \               /
                     \             /
                      ▶  OPINION  ◀        (tabla de hechos)
                     /             \
                    /               \
      Clasificacion                  FuenteDatos ── TipoFuente
```

- **Tabla de hechos: `Opinion`.** Cada fila es una opinión individual procesada por el ETL, sin importar el canal de origen. Contiene las medidas del análisis (`PuntajeSatisfaccion`, conteo de filas) y las claves foráneas hacia las dimensiones.
- **Dimensiones: `Cliente`, `Producto`, `FuenteDatos`, `Clasificacion`.** Describen el "quién, qué, de dónde y cómo se clasificó" cada opinión.
- **Sub-dimensiones: `Categoria` y `TipoFuente`.** Normalizan atributos que se repiten en `Producto` y `FuenteDatos` respectivamente (esto introduce un ligero "copo de nieve", justificado más abajo).

### ¿Por qué una sola tabla de hechos para los tres canales?

Las tres fuentes (encuestas, reseñas web, comentarios sociales) describen el **mismo hecho de negocio**: *un cliente opinó sobre un producto en una fecha, con cierto tono y cierto puntaje*. Unificarlas en `Opinion` permite:

1. Comparar canales con una sola consulta (`GROUP BY` sobre la fuente), en lugar de unir tres tablas heterogéneas con `UNION`.
2. Calcular indicadores globales (promedio de satisfacción general, % de positivas) sin duplicar lógica.
3. Que el ETL trate cada canal como un flujo más que desemboca en el mismo destino.

La dimensión `FuenteDatos` → `TipoFuente` conserva la trazabilidad de qué canal y qué lote de carga produjo cada fila.

### Dimensión tiempo

La fecha se almacena directamente como columna `Fecha` (tipo `DATE`) en la tabla de hechos, en lugar de una dimensión calendario separada. Para el alcance de esta práctica, las funciones de fecha de SQL Server (`YEAR`, `MONTH`, `DATEPART(QUARTER, ...)`) cubren todos los análisis por periodo requeridos sin el costo de poblar y mantener una tabla `DimFecha`. Si el volumen o la complejidad del análisis creciera, agregar una dimensión calendario sería una extensión natural del modelo.

---

## 3. Entidades y claves

| Tabla | Clave primaria | Tipo de PK | Claves foráneas |
|---|---|---|---|
| `Categoria` | `IdCategoria` | `INT IDENTITY` (surrogate) | — |
| `TipoFuente` | `IdTipoFuente` | `INT IDENTITY` (surrogate) | — |
| `Clasificacion` | `IdClasificacion` | `INT IDENTITY` (surrogate) | — |
| `Cliente` | `IdCliente` | `INT` (natural, del origen) | — |
| `Producto` | `IdProducto` | `INT` (natural, del origen) | `IdCategoria → Categoria` |
| `FuenteDatos` | `IdFuente` | `NVARCHAR(10)` (natural, ej. "F001") | `IdTipoFuente → TipoFuente` |
| `Opinion` | `IdOpinion` | `INT IDENTITY` (surrogate) | `IdCliente → Cliente`, `IdProducto → Producto`, `IdFuente → FuenteDatos`, `IdClasificacion → Clasificacion` |

### Decisiones sobre las claves

- **Claves naturales en `Cliente` y `Producto`.** Los sistemas origen ya identifican a clientes y productos con códigos ("C019", "P003"). El ETL extrae la parte numérica y la usa como PK, lo que permite resolver las referencias de las opiniones de cualquier canal sin tablas de mapeo intermedias.
- **Claves surrogate (`IDENTITY`) en los catálogos y en `Opinion`.** Los catálogos se derivan de valores de texto encontrados en los archivos (nombres de categoría, tipos de fuente, etiquetas de sentimiento); la identidad la genera la base. En `Opinion`, ninguna combinación de columnas es única por naturaleza (un mismo cliente puede opinar dos veces del mismo producto el mismo día), así que se usa un autonumérico.
- **FKs opcionales (NULL) en la tabla de hechos.** No todos los canales aportan todos los datos: un comentario de red social puede ser de un usuario que no es cliente registrado, o referir a un producto no catalogado. Hacer las FKs opcionales permite conservar la opinión (cuenta para los totales y el análisis de sentimiento) sin violar la integridad referencial. El ETL valida cada referencia contra la dimensión y la deja en `NULL` cuando no existe.

---

## 4. Normalización

- Las dimensiones están en **3FN**: cada atributo depende solo de su clave (`Nombre` y `Email` de `Cliente`; `Nombre` de `Producto`, etc.).
- Los valores repetitivos de texto se extrajeron a catálogos con **restricción de unicidad** (`UNIQUE` sobre `Nombre` en `Categoria`, `TipoFuente` y `Clasificacion`), evitando redundancia y errores de tipeo: la etiqueta "Positiva" existe una sola vez y las opiniones la referencian por Id.
- Se aceptó un **ligero copo de nieve** (`Producto → Categoria` y `FuenteDatos → TipoFuente`) en lugar de desnormalizar la categoría y el tipo dentro de las dimensiones. Con catálogos tan pequeños, el costo del JOIN adicional es despreciable y se gana consistencia.
- La tabla de hechos, en cambio, sigue el criterio dimensional clásico: solo claves foráneas y medidas, sin atributos descriptivos duplicados.

---

## 5. Integridad referencial

Todas las relaciones están materializadas como restricciones `FOREIGN KEY`:

- `Producto.IdCategoria → Categoria` (obligatoria: todo producto tiene categoría).
- `FuenteDatos.IdTipoFuente → TipoFuente` (obligatoria: toda fuente tiene un tipo).
- `Opinion.{IdCliente, IdProducto, IdFuente, IdClasificacion}` → sus dimensiones (opcionales, por lo explicado arriba).

---

## 6. Criterios de análisis: cómo responde el modelo a las preguntas

| Pregunta de análisis | Cómo la resuelve el modelo |
|---|---|
| Total de comentarios procesados | `COUNT(*)` sobre `Opinion` |
| Promedio de satisfacción general | `AVG(PuntajeSatisfaccion)` sobre `Opinion` |
| Comentarios por fuente / canal | JOIN `Opinion → FuenteDatos → TipoFuente`, `GROUP BY TipoFuente.Nombre` |
| % positivas / negativas / neutras | JOIN `Opinion → Clasificacion`, conteo por clasificación sobre el total |
| Producto con más comentarios / mejor calificado / más negativas | JOIN `Opinion → Producto`, `GROUP BY` con `COUNT` / `AVG` / filtro por clasificación |
| Satisfacción por producto en el tiempo | `GROUP BY Producto, YEAR(Fecha), MONTH(Fecha)` |
| Clientes que más opinan / patrones por segmento | JOIN `Opinion → Cliente`, `GROUP BY` cliente (extensible con atributos de segmento) |
| Tendencia mensual / trimestral | `GROUP BY YEAR(Fecha), MONTH(Fecha)` o `DATEPART(QUARTER, Fecha)` |
| Diferencia de tono entre canales | `GROUP BY TipoFuente.Nombre, Clasificacion.Nombre` |

### Consultas de ejemplo

```sql
-- Porcentaje de opiniones por clasificación
SELECT c.Nombre,
       COUNT(*)                                            AS Total,
       CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER ()
            AS DECIMAL(5,2))                               AS Porcentaje
FROM dbo.Opinion o
JOIN dbo.Clasificacion c ON c.IdClasificacion = o.IdClasificacion
GROUP BY c.Nombre;

-- Satisfacción promedio por producto y mes (tendencia)
SELECT p.Nombre,
       YEAR(o.Fecha)  AS Anio,
       MONTH(o.Fecha) AS Mes,
       AVG(CAST(o.PuntajeSatisfaccion AS DECIMAL(4,2))) AS PromedioSatisfaccion,
       COUNT(*)       AS TotalOpiniones
FROM dbo.Opinion o
JOIN dbo.Producto p ON p.IdProducto = o.IdProducto
GROUP BY p.Nombre, YEAR(o.Fecha), MONTH(o.Fecha)
ORDER BY p.Nombre, Anio, Mes;

-- Comentarios y proporción de negativas por canal
SELECT tf.Nombre AS Canal,
       COUNT(*)  AS TotalComentarios,
       CAST(100.0 * SUM(CASE WHEN cl.Nombre = 'Negativa' THEN 1 ELSE 0 END)
            / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeNegativas
FROM dbo.Opinion o
JOIN dbo.FuenteDatos f   ON f.IdFuente = o.IdFuente
JOIN dbo.TipoFuente tf   ON tf.IdTipoFuente = f.IdTipoFuente
LEFT JOIN dbo.Clasificacion cl ON cl.IdClasificacion = o.IdClasificacion
GROUP BY tf.Nombre
ORDER BY TotalComentarios DESC;
```

---

## 7. Carga de datos (contexto del ETL)

El script incluye también el procedimiento almacenado `dbo.usp_InsertOpinion`, que el proceso ETL (aplicación de consola en .NET) utiliza para insertar las opiniones vía ADO.NET. El flujo de carga respeta el orden de dependencias del modelo:

1. **Catálogos:** `Categoria`, `TipoFuente`, `Clasificacion` (valores únicos derivados de los archivos).
2. **Dimensiones:** `Cliente`, `Producto`, `FuenteDatos`.
3. **Hechos:** `Opinion`, unificando los tres canales; las filas cuya referencia no se puede resolver conservan la FK en `NULL` y las que violan restricciones se contabilizan como rechazadas.

---

## 8. Entregables

1. **Diagrama Entidad–Relación (DER)** — se anexa por separado.
2. **Script SQL** — `01_Script_SistemaOpiniones_Analitica.sql` (creación de la base, tablas, claves, relaciones y procedimiento de inserción).
3. **Este documento** — decisiones de diseño, normalización, relaciones y criterios de análisis.
