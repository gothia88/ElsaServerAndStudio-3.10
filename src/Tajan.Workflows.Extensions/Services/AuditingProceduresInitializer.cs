using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tajan.Workflows.Extensions.Data.MainEntities;
using Tajan.Workflows.Extensions.Options;

namespace Tajan.Workflows.Extensions.Services;

/// <summary>
/// Creates (or updates) the auditing stored procedures from the embedded sql/*.sql scripts.
/// Runs idempotently via CREATE OR ALTER, with retries to tolerate startup ordering.
/// </summary>
public class AuditingProceduresInitializer : IHostedService
{
    private const int MaxAttempts = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ArchiveOptions _options;
    private readonly ILogger<AuditingProceduresInitializer> _logger;

    public AuditingProceduresInitializer(
        IServiceScopeFactory scopeFactory,
        IOptions<ArchiveOptions> options,
        ILogger<AuditingProceduresInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoCreateAuditingProcedures)
            return;

        var assembly = typeof(AuditingProceduresInitializer).Assembly;
        var resources = assembly
            .GetManifestResourceNames()
            .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n)
            .ToArray();

        if (resources.Length == 0)
        {
            _logger.LogWarning("No auditing SQL scripts were embedded; skipping stored procedure creation.");
            return;
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ElsaMainDataDbContext>();

                foreach (var resource in resources)
                {
                    var sql = await ReadResourceAsync(assembly, resource, cancellationToken);
                    if (string.IsNullOrWhiteSpace(sql))
                        continue;

                    await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                    _logger.LogInformation("Ensured auditing stored procedure from {Resource}.", resource);
                }

                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "Auditing stored procedure creation failed (attempt {Attempt}/{MaxAttempts}); retrying in 3 seconds.",
                    attempt,
                    MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create the auditing stored procedures. Run sql/*.sql manually if needed.");
                return;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName, CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return string.Empty;

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
