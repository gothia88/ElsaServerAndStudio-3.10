using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tajan.Workflows.Extensions.Options;

namespace Tajan.Workflows.Extensions.Services;

/// <summary>
/// Background cleanup of archived workflow instances older than the configured retention window.
/// Disabled when <see cref="ArchiveOptions.RetentionDays"/> is 0.
/// </summary>
public class ArchiveRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ArchiveOptions _options;
    private readonly ILogger<ArchiveRetentionService> _logger;

    public ArchiveRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<ArchiveOptions> options,
        ILogger<ArchiveRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.RetentionDays <= 0)
        {
            _logger.LogInformation("Archive retention cleanup is disabled (Archive:RetentionDays = 0).");
            return;
        }

        var period = TimeSpan.FromHours(Math.Max(1, _options.RetentionScanHours));
        using var timer = new PeriodicTimer(period);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var archiveService = scope.ServiceProvider.GetRequiredService<IArchiveService>();
                var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays);
                var deleted = await archiveService.PurgeArchivedBeforeAsync(cutoff, stoppingToken);

                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "Archive retention removed {Count} workflow instances finished before {Cutoff}.",
                        deleted,
                        cutoff);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Archive retention scan failed.");
            }
        }
    }
}
