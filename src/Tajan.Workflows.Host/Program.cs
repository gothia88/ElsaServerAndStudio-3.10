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
using Serilog.Sinks.MSSqlServer;
using Tajan.Workflows.Extensions.Data.ArchiveEntities;
using Tajan.Workflows.Extensions.Data.MainEntities;
using Tajan.Workflows.Extensions.Options;
using Tajan.Workflows.Extensions.Services;
using Tajan.Workflows.Host.Authentication;
using Tajan.Workflows.Host.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Configuration.AddJsonFile("appsettings.Identity.json", optional: true, reloadOnChange: false);

// Local, gitignored overrides (connection string etc.) for `dotnet run` in Development.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
}

var services = builder.Services;
var configuration = builder.Configuration;

// Serilog: configuration-driven, plus the optional SQL Server sink (ElsaServerLogs)
// when ConnectionStrings:Logs is provided. No connection string is committed.
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration);

var logDbConnectionString = configuration.GetConnectionString("Logs");
if (!string.IsNullOrWhiteSpace(logDbConnectionString))
{
    loggerConfiguration.WriteTo.MSSqlServer(
        connectionString: logDbConnectionString,
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "ElsaServerLogs",
            SchemaName = "dbo",
            AutoCreateSqlTable = true
        });
}

Log.Logger = loggerConfiguration.CreateLogger();
builder.Host.UseSerilog();
Log.Information("Content root: {ContentRoot}", builder.Environment.ContentRootPath);

var connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

// Log the target (server/database only — never credentials) for easier troubleshooting.
var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
Log.Information(
    "Database target: {DataSource}/{Database}",
    connectionStringBuilder.DataSource,
    connectionStringBuilder.InitialCatalog);

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
        })
        .UseFileStorage();

    elsa.UseTenants(tenants =>
    {
        tenants.UseConfigurationBasedTenantsProvider(options => configuration.GetSection("Multitenancy").Bind(options));
        tenants.ConfigureMultitenancy(options =>
            options.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder()
                .Append<ClaimsTenantResolver>()
                // Allows unauthenticated HTTP workflow calls to select a tenant via X-Tenant-Id.
                .Append<HeaderTenantResolver>());
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
services.AddHostedService<AuditingProceduresInitializer>();
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
services.AddRazorPages();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors();
app.UseBlazorFrameworkFiles();

var apiRoutePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.MapWorkflowsApi(apiRoutePrefix);

app.UseRouting();
app.UseStaticFiles();
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

// Single-process Studio: the Blazor WASM client is served by this host.
app.MapFallbackToPage("/_Host");

await app.RunAsync();

public partial class Program;
