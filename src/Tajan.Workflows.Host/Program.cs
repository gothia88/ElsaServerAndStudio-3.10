using System.Text.Encodings.Web;
using Elsa.Extensions;
using Elsa.Identity.Multitenancy;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Tenants;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Tajan.Workflows.Extensions.Data.ArchiveEntities;
using Tajan.Workflows.Extensions.Data.MainEntities;
using Tajan.Workflows.Extensions.Options;
using Tajan.Workflows.Extensions.Services;
using Tajan.Workflows.Host.Authentication;
using Tajan.Workflows.Host.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
builder.Host.UseSerilog();

var connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

services.AddElsa(elsa =>
{
    elsa
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>()
        .UseIdentity(identity =>
        {
            identity.TokenOptions += options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflows()
        .UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef => ef.UseSqlServer(connectionString));
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlServer(connectionString));
        })
        .UseWorkflowsApi()
        .UseScheduling()
        .UseCSharp()
        .UseJavaScript(options =>
        {
            // 3.10: CLR access inside JavaScript is opt-in.
            options.AllowClrAccess = configuration.GetValue("JavaScript:AllowClrAccess", false);
        })
        .UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default)
        .UseHttp(http =>
        {
            http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);
        });

    elsa.UseTenants(tenants =>
    {
        tenants.UseConfigurationBasedTenantsProvider(options => configuration.GetSection("Multitenancy").Bind(options));
        tenants.ConfigureMultitenancy(options =>
            options.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder()
                .Append<ClaimsTenantResolver>());
    });
});

// Custom hooks (ported from the 3.5 project).
services.AddNotificationHandlersFrom<Program>();

// Archive / history extension services.
services.Configure<ArchiveOptions>(configuration.GetSection("Archive"));
services.Configure<HooksOptions>(configuration.GetSection("Hooks"));
services.AddScoped<IArchiveService, ArchiveService>();
services.AddScoped<IWorkflowInstanceService, WorkflowInstanceService>();
services.AddHostedService<ArchiveSchemaInitializer>();
services.AddHostedService<ArchiveRetentionService>();
services.AddDbContext<ElsaMainDataDbContext>(options => options.UseSqlServer(connectionString));
services.AddDbContext<ElsaArchiveDataDbContext>(options => options.UseSqlServer(connectionString));

// Custom client-credentials authentication against an external auth server.
services.Configure<AuthServerSettings>(configuration.GetSection("AuthServer"));
services.AddHttpClient();
services.AddTransient<GlobalExceptionHandlerMiddleware>();
services.AddAuthentication()
    .AddScheme<ClientCredentialsAuthenticationOptions, ClientCredentialsAuthenticationHandler>(
        ClientCredentialsAuthenticationOptions.DefaultScheme, _ => { });

services.AddAuthorization(options =>
{
    options.AddPolicy("test", policy =>
    {
        policy.AddAuthenticationSchemes(ClientCredentialsAuthenticationOptions.DefaultScheme);
        policy.RequireClaim("Scope").RequireAssertion(context =>
        {
            var scope = context.User.FindFirst("Scope")?.Value;
            return scope?.Contains("testWorkFlow", StringComparison.OrdinalIgnoreCase) == true;
        });
    });
});

services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors();

var apiRoutePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.MapWorkflowsApi(apiRoutePrefix);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Re-authenticate Elsa HTTP workflow endpoints with the client-credentials scheme,
// mirroring the behavior of the original server.
app.Use(async (context, next) =>
{
    var workflowBasePath = configuration.GetSection("Http").GetValue<string>("BasePath") ?? "/workflows";
    if (context.Request.Path.StartsWithSegments(workflowBasePath, StringComparison.OrdinalIgnoreCase))
    {
        var authResult = await context.AuthenticateAsync(ClientCredentialsAuthenticationOptions.DefaultScheme);
        if (authResult.Succeeded && authResult.Principal is not null)
            context.User = authResult.Principal;
    }

    await next();
});

app.UseTenants();
app.UseWorkflows();

await app.RunAsync();

public partial class Program;
