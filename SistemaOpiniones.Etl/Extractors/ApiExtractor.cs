using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SistemaOpiniones.Etl.Abstractions;
using SistemaOpiniones.Etl.Configuration;
using SistemaOpiniones.Etl.Models;

namespace SistemaOpiniones.Etl.Extractors;

/// <summary>
/// Extrae los comentarios de redes sociales consumiendo una API REST.
///
/// El HttpClient se obtiene de <see cref="IHttpClientFactory"/>, que reutiliza y recicla
/// los handlers: evita el agotamiento de sockets y el problema del DNS obsoleto que tiene
/// instanciar HttpClient a mano.
///
/// El mapeo JSON → <see cref="RawOpinion"/> es dirigido por configuración (FieldMap), así
/// que cambiar de proveedor o absorber un cambio de contrato no requiere recompilar.
/// </summary>
public sealed class ApiExtractor : IExtractor
{
    public const string HttpClientName = "OpinionesApi";

    private readonly ApiSourceOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(
        IOptions<EtlOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<ApiExtractor> logger)
    {
        _options = options.Value.Sources.Api;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string SourceName => _options.Name;

    public string SourceType => "ApiRest";

    public bool Enabled => _options.Enabled;

    public async IAsyncEnumerable<RawOpinion> ExtractAsync(
        ExtractionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("Etl:Sources:Api:BaseUrl está vacía.");

        var client = _httpClientFactory.CreateClient(HttpClientName);

        for (var page = 1; page <= _options.MaxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var document = await FetchPageAsync(client, page, cancellationToken);

            var results = ResolveResultsArray(document.RootElement);
            var countInPage = 0;

            foreach (var element in results.EnumerateArray())
            {
                countInPage++;

                var record = MapElement(element, context);

                if (record is null)
                {
                    context.RegisterRejected();
                    continue;
                }

                yield return record;
            }

            _logger.LogDebug("Página {Page} de {Source}: {Count} elementos", page, SourceName, countInPage);

            // Página incompleta = última página. Evita una llamada extra innecesaria.
            if (countInPage < _options.PageSize)
                yield break;
        }

        _logger.LogWarning(
            "Se alcanzó el tope de {MaxPages} páginas en {Source}; podrían quedar datos sin extraer.",
            _options.MaxPages, SourceName);
    }

    /// <summary>
    /// Pide una página con reintentos y backoff exponencial ante fallos transitorios.
    /// Un 404 o un 401 no se reintentan: no se arreglan esperando.
    /// </summary>
    private async Task<JsonDocument> FetchPageAsync(HttpClient client, int page, CancellationToken cancellationToken)
    {
        var url = BuildUrl(page);
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                using var response = await client.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if (!IsTransient(response.StatusCode) || attempt > _options.RetryCount)
                    {
                        throw new HttpRequestException(
                            $"La API respondió {(int)response.StatusCode} {response.ReasonPhrase} en '{url}'.");
                    }

                    await DelayBeforeRetryAsync(attempt, $"HTTP {(int)response.StatusCode}", cancellationToken);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (
                (ex is HttpRequestException or TaskCanceledException)
                && attempt <= _options.RetryCount
                && !cancellationToken.IsCancellationRequested)
            {
                await DelayBeforeRetryAsync(attempt, ex.Message, cancellationToken);
            }
        }
    }

    private async Task DelayBeforeRetryAsync(int attempt, string reason, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

        _logger.LogWarning(
            "Intento {Attempt}/{Total} falló en {Source} ({Reason}); reintentando en {Delay}s",
            attempt, _options.RetryCount + 1, SourceName, reason, delay.TotalSeconds);

        await Task.Delay(delay, cancellationToken);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private string BuildUrl(int page)
    {
        var endpoint = _options.Endpoint.TrimStart('/');
        var separator = endpoint.Contains('?') ? '&' : '?';

        return $"{endpoint}{separator}" +
               $"{Uri.EscapeDataString(_options.PageParamName)}={page}&" +
               $"{Uri.EscapeDataString(_options.PageSizeParamName)}={_options.PageSize}";
    }

    /// <summary>Ubica el arreglo de resultados: la raíz misma, o la propiedad envolvente configurada.</summary>
    private JsonElement ResolveResultsArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        if (!string.IsNullOrWhiteSpace(_options.ResultsProperty)
            && TryGetProperty(root, _options.ResultsProperty, out var wrapped)
            && wrapped.ValueKind == JsonValueKind.Array)
        {
            return wrapped;
        }

        throw new InvalidOperationException(
            $"La respuesta de {SourceName} no es un arreglo ni contiene la propiedad " +
            $"'{_options.ResultsProperty}'. Revisa Etl:Sources:Api:ResultsProperty.");
    }

    private RawOpinion? MapElement(JsonElement element, ExtractionContext context)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        return new RawOpinion
        {
            BatchId = context.BatchId,
            SourceName = SourceName,
            SourceType = SourceType,
            ExternalId = ReadMapped(element, nameof(RawOpinion.ExternalId)),
            ClienteRef = ReadMapped(element, nameof(RawOpinion.ClienteRef)),
            ProductoRef = ReadMapped(element, nameof(RawOpinion.ProductoRef)),
            FuenteRef = ReadMapped(element, nameof(RawOpinion.FuenteRef)),
            Fecha = ReadMapped(element, nameof(RawOpinion.Fecha)),
            Comentario = ReadMapped(element, nameof(RawOpinion.Comentario)),
            Puntaje = ReadMapped(element, nameof(RawOpinion.Puntaje)),
            Clasificacion = ReadMapped(element, nameof(RawOpinion.Clasificacion)),
            ExtractedAtUtc = context.StartedAtUtc,

            // Se conserva el JSON original: si mañana el mapeo resulta incompleto,
            // el dato sigue estando en staging y no hay que volver a llamar la API.
            RawPayload = element.GetRawText()
        };
    }

    private string? ReadMapped(JsonElement element, string field)
    {
        if (!_options.FieldMap.TryGetValue(field, out var property) || string.IsNullOrWhiteSpace(property))
            return null;

        if (!TryGetProperty(element, property, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => value.GetString(),
            _ => value.GetRawText()
        };
    }

    /// <summary>Busca la propiedad respetando mayúsculas primero y sin distinguirlas después.</summary>
    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
