using Microsoft.AspNetCore.Authentication;

namespace Tajan.Workflows.Host.Authentication;

public class ClientCredentialsAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ClientCredentialsAuthenticationScheme";
    public string TokenHeaderName { get; set; } = "Authorization";
    public string TokenType { get; set; } = "Bearer";
}
