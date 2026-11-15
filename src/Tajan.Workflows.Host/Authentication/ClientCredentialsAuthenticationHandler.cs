using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tajan.Workflows.Host.Authentication.Models;

namespace Tajan.Workflows.Host.Authentication;

/// <summary>
/// Validates a Bearer token by calling an external auth server's token-info endpoint.
/// Ported 1:1 from the original 3.5 project. Consider caching or local JWT validation
/// before using this on a hot path.
/// </summary>
public class ClientCredentialsAuthenticationHandler : AuthenticationHandler<ClientCredentialsAuthenticationOptions>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AuthServerSettings> _settings;
    private readonly ILogger<ClientCredentialsAuthenticationHandler> _logger;

    public ClientCredentialsAuthenticationHandler(
        IOptionsMonitor<ClientCredentialsAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory,
        IOptions<AuthServerSettings> settings)
        : base(options, logger, encoder)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger.CreateLogger<ClientCredentialsAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Options.TokenHeaderName))
            return AuthenticateResult.Fail($"Missing header: {Options.TokenHeaderName}");

        var header = Request.Headers[Options.TokenHeaderName].ToString();
        if (!header.StartsWith(Options.TokenType, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Invalid token.");

        var token = header[Options.TokenType.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.Fail("Invalid token.");

        var httpClient = _httpClientFactory.CreateClient(nameof(ClientCredentialsAuthenticationHandler));
        httpClient.BaseAddress = new Uri(_settings.Value.BaseUrl);
        httpClient.DefaultRequestHeaders.ConnectionClose = true;

        var credential = $"{_settings.Value.ApplicationId}:{_settings.Value.ApplicationSecret}";
        var parameters = Convert.ToBase64String(Encoding.UTF8.GetBytes(credential));
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", parameters);

        using var jsonContent = new StringContent(
            JsonSerializer.Serialize(new { token }),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync("/api/ClientCredentialsFlow/token_info", jsonContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication request to the auth server failed.");
            return AuthenticateResult.Fail("Authentication is failed.");
        }

        if (!response.IsSuccessStatusCode)
            return AuthenticateResult.Fail("Authentication is failed.");

        var tokenInfo = await response.Content.ReadFromJsonAsync<GetTokenInfoResponse>();
        if (tokenInfo is null || !tokenInfo.Active)
            return AuthenticateResult.Fail("Invalid token.");

        var claims = new List<Claim> { new("Scope", tokenInfo.Scope ?? string.Empty) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
