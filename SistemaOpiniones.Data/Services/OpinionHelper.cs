using Microsoft.EntityFrameworkCore;
using SistemaOpiniones.Data.Context;

namespace SistemaOpiniones.Data.Services;

internal static class OpinionHelper
{
    /// <summary>
    /// Extrae los dígitos de un texto ("C019" -> 19) y devuelve el id solo si
    /// existe en el conjunto de válidos; si no, devuelve null.
    /// </summary>
    public static int? ResolverId(string? texto, HashSet<int> validos)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var digitos = new string(texto.Where(char.IsDigit).ToArray());

        if (int.TryParse(digitos, out var id) && validos.Contains(id))
            return id;

        return null;
    }

    /// <summary>
    /// Los CSV de opiniones no traen el IdFuente; solo se sabe por qué canal
    /// llegaron. Se toma la fuente registrada de ese tipo con el menor IdFuente
    /// para que la carga sea siempre la misma. Devuelve null si el tipo no existe.
    /// </summary>
    public static async Task<string?> ResolverIdFuenteAsync(SistemaOpinionesContext context, string tipoFuente)
        => await context.FuenteDatos
            .Where(f => f.IdTipoFuenteNavigation.Nombre == tipoFuente)
            .OrderBy(f => f.IdFuente)
            .Select(f => f.IdFuente)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Traduce un puntaje de 1 a 5 a la clasificación de sentimiento, usando el
    /// mismo criterio con el que vienen clasificadas las encuestas
    /// (1-2 Negativa, 3 Neutra, 4-5 Positiva).
    /// </summary>
    public static string? ClasificacionPorPuntaje(byte? puntaje) => puntaje switch
    {
        1 or 2 => "Negativa",
        3 => "Neutra",
        4 or 5 => "Positiva",
        _ => null
    };
}
