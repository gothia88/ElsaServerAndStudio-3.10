using System.Net.Http.Json;
using System.Text.Json;

namespace Elsa.Activities.Samples.Ca;

/// <summary>
/// Small HTTP helper shared by the converted TJ.CA activities.
/// </summary>
internal static class CaHttp
{
    public static async Task<JsonElement> PostJsonAsync(
        IHttpClientFactory httpClientFactory,
        string url,
        object payload,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(CaHttp));
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    public static async Task PostAsync(
        IHttpClientFactory httpClientFactory,
        string url,
        object payload,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(CaHttp));
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public static bool IsValid(JsonElement response)
    {
        if (!response.TryGetProperty("data", out var data))
            return false;

        if (!data.TryGetProperty("isValid", out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    public static string GetErrorMessage(JsonElement response)
    {
        if (!response.TryGetProperty("data", out var data))
            return string.Empty;

        if (!data.TryGetProperty("errorMessage", out var errors))
            return string.Empty;

        return errors.ValueKind switch
        {
            JsonValueKind.Array => string.Join("\n", errors.EnumerateArray().Select(e => e.GetString())),
            JsonValueKind.String => errors.GetString() ?? string.Empty,
            _ => errors.ToString()
        };
    }
}
