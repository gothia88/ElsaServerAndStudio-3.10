using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tajan.Workflows.Extensions.Data.ArchiveEntities;
using Tajan.Workflows.Extensions.Options;

namespace Tajan.Workflows.Extensions.Services;

/// <summary>
/// Creates the archive tables in the "dbo" schema on startup when they do not exist yet.
/// Only the archive model is created, so Elsa's own tables are left untouched.
/// </summary>
public class ArchiveSchemaInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ArchiveOptions _options;
    private readonly ILogger<ArchiveSchemaInitializer> _logger;

    public ArchiveSchemaInitializer(
        IServiceScopeFactory scopeFactory,
        IOptions<ArchiveOptions> options,
        ILogger<ArchiveSchemaInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoCreateTables)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var archive = scope.ServiceProvider.GetRequiredService<ElsaArchiveDataDbContext>();

            var exists = await archive.Database
                .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.WorkflowInstances', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
                .SingleAsync(cancellationToken);

            if (exists == 1)
                return;

            _logger.LogInformation("Archive tables not found; creating them in the dbo schema...");
            var creator = archive.Database.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync(cancellationToken);
            _logger.LogInformation("Archive tables created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure the archive tables exist. Create them manually if this persists.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
