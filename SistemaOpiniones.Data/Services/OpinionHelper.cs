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
}
