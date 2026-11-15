using System.Text.Json.Serialization;

namespace Tajan.Workflows.Host.Authentication.Models;

public class GetTokenInfoResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("exp")]
    public long ExpireIn { get; set; }
}
