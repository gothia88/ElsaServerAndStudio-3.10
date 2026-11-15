using Elsa.Common.Multitenancy;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

namespace Tajan.Workflows.Host.TenantResolvers;

/// <summary>
/// Sample resolver kept from the original project. Not registered by default; the host uses
/// <c>ClaimsTenantResolver</c>. Register it in Program.cs if you need custom claim mapping.
/// </summary>
public class CustomClaimsTenantResolver(IHttpContextAccessor httpContextAccessor, IOptions<IdentityTokenOptions> options)
    : TenantResolverBase
{
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
            return Unresolved();

        var tenantId = httpContext.User.FindFirst(options.Value.TenantIdClaimsType)?.Value;
        return AutoResolve(tenantId);
    }
}
