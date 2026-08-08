using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Abstractions;

/// <summary>
/// Contrato único de toda fuente de datos del proceso de extracción.
///
/// Escalabilidad: agregar un canal nuevo se reduce a implementar esta interfaz y
/// registrarla en el contenedor; ni el orquestador ni el Worker cambian.
///
/// Rendimiento: devuelve <see cref="IAsyncEnumerable{T}"/> en vez de una lista, de modo
/// que los registros fluyen hacia staging a medida que se leen y el consumo de memoria
/// no depende del tamaño de la fuente.
/// </summary>
public interface IExtractor
{
    /// <summary>Nombre lógico de la fuente; aparece en los logs y en la columna SourceName de staging.</summary>
    string SourceName { get; }

    /// <summary>Tipo de origen: CSV, Database o ApiRest.</summary>
    string SourceType { get; }

    /// <summary>Permite apagar la fuente desde configuración, sin recompilar.</summary>
    bool Enabled { get; }

    IAsyncEnumerable<RawOpinion> ExtractAsync(ExtractionContext context, CancellationToken cancellationToken);
}
