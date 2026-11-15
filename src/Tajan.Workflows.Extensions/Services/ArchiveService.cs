using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tajan.Workflows.Extensions.Data.ArchiveEntities;
using Tajan.Workflows.Extensions.Data.MainEntities;
using Tajan.Workflows.Extensions.Options;
using ArchiveActivityExecutionRecord = Tajan.Workflows.Extensions.Data.ArchiveEntities.ActivityExecutionRecord;
using ArchiveBookmark = Tajan.Workflows.Extensions.Data.ArchiveEntities.Bookmark;
using ArchiveBookmarkQueueItem = Tajan.Workflows.Extensions.Data.ArchiveEntities.BookmarkQueueItem;
using ArchiveWorkflowExecutionLogRecord = Tajan.Workflows.Extensions.Data.ArchiveEntities.WorkflowExecutionLogRecord;
using ArchiveWorkflowInboxMessage = Tajan.Workflows.Extensions.Data.ArchiveEntities.WorkflowInboxMessage;
using ArchiveWorkflowInstance = Tajan.Workflows.Extensions.Data.ArchiveEntities.WorkflowInstance;
using MainWorkflowInstance = Tajan.Workflows.Extensions.Data.MainEntities.WorkflowInstance;

namespace Tajan.Workflows.Extensions.Services;

/// <summary>
/// Moves finished workflow instances from Elsa's hot tables (schema "Elsa") into the
/// archive tables (schema "dbo") in the same database.
///
/// Compared to the original 3.5 implementation this version:
///  - is idempotent (skips instances already archived),
///  - runs inside a single ambient transaction (copy + optional purge),
///  - optionally deletes the source rows, and
///  - supports retention cleanup.
/// </summary>
public class ArchiveService : IArchiveService
{
    private const int RetentionBatchSize = 5000;

    private readonly ElsaMainDataDbContext _main;
    private readonly ElsaArchiveDataDbContext _archive;
    private readonly ILogger<ArchiveService> _logger;
    private readonly ArchiveOptions _options;

    public ArchiveService(
        ElsaMainDataDbContext main,
        ElsaArchiveDataDbContext archive,
        ILogger<ArchiveService> logger,
        IOptions<ArchiveOptions> options)
    {
        _main = main;
        _archive = archive;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> ArchiveWorkflowInstanceAndDependencies(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        if (await _archive.WorkflowInstances.AnyAsync(w => w.Id == workflowInstanceId, cancellationToken))
        {
            _logger.LogInformation("Workflow instance {WorkflowInstanceId} is already archived; skipping.", workflowInstanceId);
            return false;
        }

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        var workflowInstance = await _main.WorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == workflowInstanceId, cancellationToken);

        if (workflowInstance is null)
        {
            _logger.LogWarning("Workflow instance {WorkflowInstanceId} was not found in the main tables; nothing to archive.", workflowInstanceId);
            return false;
        }

        await _archive.WorkflowInstances.AddAsync(Map(workflowInstance), cancellationToken);

        var activityExecutionRecords = await _main.ActivityExecutionRecords
            .AsNoTracking()
            .Where(a => a.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync(cancellationToken);
        await _archive.ActivityExecutionRecords.AddRangeAsync(activityExecutionRecords.Select(Map), cancellationToken);

        var bookmarks = await _main.Bookmarks
            .AsNoTracking()
            .Where(b => b.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync(cancellationToken);
        await _archive.Bookmarks.AddRangeAsync(bookmarks.Select(Map), cancellationToken);

        var bookmarkQueueItems = await _main.BookmarkQueueItems
            .AsNoTracking()
            .Where(b => b.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync(cancellationToken);
        await _archive.BookmarkQueueItems.AddRangeAsync(bookmarkQueueItems.Select(Map), cancellationToken);

        var workflowExecutionLogRecords = await _main.WorkflowExecutionLogRecords
            .AsNoTracking()
            .Where(l => l.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync(cancellationToken);
        await _archive.WorkflowExecutionLogRecords.AddRangeAsync(workflowExecutionLogRecords.Select(Map), cancellationToken);

        var workflowInboxMessages = await _main.WorkflowInboxMessages
            .AsNoTracking()
            .Where(m => m.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync(cancellationToken);
        await _archive.WorkflowInboxMessages.AddRangeAsync(workflowInboxMessages.Select(Map), cancellationToken);

        await _archive.SaveChangesAsync(cancellationToken);

        if (_options.PurgeMainAfterArchive)
        {
            // Delete children first, the instance last. ExecuteDelete runs immediately and
            // enlists in the ambient TransactionScope.
            await _main.ActivityExecutionRecords.Where(x => x.WorkflowInstanceId == workflowInstanceId).ExecuteDeleteAsync(cancellationToken);
            await _main.Bookmarks.Where(x => x.WorkflowInstanceId == workflowInstanceId).ExecuteDeleteAsync(cancellationToken);
            await _main.BookmarkQueueItems.Where(x => x.WorkflowInstanceId == workflowInstanceId).ExecuteDeleteAsync(cancellationToken);
            await _main.WorkflowExecutionLogRecords.Where(x => x.WorkflowInstanceId == workflowInstanceId).ExecuteDeleteAsync(cancellationToken);
            await _main.WorkflowInboxMessages.Where(x => x.WorkflowInstanceId == workflowInstanceId).ExecuteDeleteAsync(cancellationToken);
            await _main.WorkflowInstances.Where(x => x.Id == workflowInstanceId).ExecuteDeleteAsync(cancellationToken);
        }

        scope.Complete();

        _logger.LogInformation(
            "Archived workflow instance {WorkflowInstanceId} (purgedMain={PurgedMain}).",
            workflowInstanceId,
            _options.PurgeMainAfterArchive);

        return true;
    }

    public async Task<int> PurgeArchivedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        var ids = await _archive.WorkflowInstances
            .Where(w => w.FinishedAt != null && w.FinishedAt < cutoff)
            .OrderBy(w => w.FinishedAt)
            .Select(w => w.Id)
            .Take(RetentionBatchSize)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
            return 0;

        await _archive.ActivityExecutionRecords.Where(x => x.WorkflowInstanceId != null && ids.Contains(x.WorkflowInstanceId)).ExecuteDeleteAsync(cancellationToken);
        await _archive.Bookmarks.Where(x => x.WorkflowInstanceId != null && ids.Contains(x.WorkflowInstanceId)).ExecuteDeleteAsync(cancellationToken);
        await _archive.BookmarkQueueItems.Where(x => x.WorkflowInstanceId != null && ids.Contains(x.WorkflowInstanceId)).ExecuteDeleteAsync(cancellationToken);
        await _archive.WorkflowExecutionLogRecords.Where(x => x.WorkflowInstanceId != null && ids.Contains(x.WorkflowInstanceId)).ExecuteDeleteAsync(cancellationToken);
        await _archive.WorkflowInboxMessages.Where(x => x.WorkflowInstanceId != null && ids.Contains(x.WorkflowInstanceId)).ExecuteDeleteAsync(cancellationToken);
        await _archive.WorkflowInstances.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        return ids.Count;
    }

    private static ArchiveWorkflowInstance Map(MainWorkflowInstance w) => new()
    {
        Id = w.Id,
        CorrelationId = w.CorrelationId,
        CreatedAt = w.CreatedAt,
        Data = w.Data,
        DataCompressionAlgorithm = w.DataCompressionAlgorithm,
        DefinitionId = w.DefinitionId,
        DefinitionVersionId = w.DefinitionVersionId,
        FinishedAt = w.FinishedAt,
        IncidentCount = w.IncidentCount,
        IsExecuting = w.IsExecuting,
        IsSystem = w.IsSystem,
        Name = w.Name,
        ParentWorkflowInstanceId = w.ParentWorkflowInstanceId,
        Status = w.Status,
        SubStatus = w.SubStatus,
        TenantId = w.TenantId,
        UpdatedAt = w.UpdatedAt,
        Version = w.Version,
    };

    private static ArchiveActivityExecutionRecord Map(Data.MainEntities.ActivityExecutionRecord a) => new()
    {
        Id = a.Id,
        ActivityId = a.ActivityId,
        ActivityName = a.ActivityName,
        ActivityNodeId = a.ActivityNodeId,
        ActivityType = a.ActivityType,
        ActivityTypeVersion = a.ActivityTypeVersion,
        AggregateFaultCount = a.AggregateFaultCount,
        CompletedAt = a.CompletedAt,
        HasBookmarks = a.HasBookmarks,
        SerializedActivityState = a.SerializedActivityState,
        SerializedActivityStateCompressionAlgorithm = a.SerializedActivityStateCompressionAlgorithm,
        SerializedException = a.SerializedException,
        SerializedMetadata = a.SerializedMetadata,
        SerializedOutputs = a.SerializedOutputs,
        SerializedPayload = a.SerializedPayload,
        SerializedProperties = a.SerializedProperties,
        StartedAt = a.StartedAt,
        Status = a.Status,
        TenantId = a.TenantId,
        WorkflowInstanceId = a.WorkflowInstanceId,
    };

    private static ArchiveBookmark Map(Data.MainEntities.Bookmark b) => new()
    {
        Id = b.Id,
        ActivityInstanceId = b.ActivityInstanceId,
        ActivityTypeName = b.ActivityTypeName,
        CorrelationId = b.CorrelationId,
        CreatedAt = b.CreatedAt,
        Hash = b.Hash,
        Name = b.Name,
        SerializedMetadata = b.SerializedMetadata,
        SerializedPayload = b.SerializedPayload,
        TenantId = b.TenantId,
        WorkflowInstanceId = b.WorkflowInstanceId,
    };

    private static ArchiveBookmarkQueueItem Map(Data.MainEntities.BookmarkQueueItem b) => new()
    {
        Id = b.Id,
        ActivityInstanceId = b.ActivityInstanceId,
        ActivityTypeName = b.ActivityTypeName,
        BookmarkId = b.BookmarkId,
        CorrelationId = b.CorrelationId,
        CreatedAt = b.CreatedAt,
        SerializedOptions = b.SerializedOptions,
        StimulusHash = b.StimulusHash,
        TenantId = b.TenantId,
        WorkflowInstanceId = b.WorkflowInstanceId,
    };

    private static ArchiveWorkflowExecutionLogRecord Map(Data.MainEntities.WorkflowExecutionLogRecord l) => new()
    {
        Id = l.Id,
        ActivityId = l.ActivityId,
        ActivityInstanceId = l.ActivityInstanceId,
        ActivityName = l.ActivityName,
        ActivityNodeId = l.ActivityNodeId,
        ActivityType = l.ActivityType,
        ActivityTypeVersion = l.ActivityTypeVersion,
        EventName = l.EventName,
        Message = l.Message,
        ParentActivityInstanceId = l.ParentActivityInstanceId,
        Sequence = l.Sequence,
        SerializedActivityState = l.SerializedActivityState,
        SerializedPayload = l.SerializedPayload,
        Source = l.Source,
        TenantId = l.TenantId,
        Timestamp = l.Timestamp,
        WorkflowDefinitionId = l.WorkflowDefinitionId,
        WorkflowDefinitionVersionId = l.WorkflowDefinitionVersionId,
        WorkflowInstanceId = l.WorkflowInstanceId,
        WorkflowVersion = l.WorkflowVersion,
    };

    private static ArchiveWorkflowInboxMessage Map(Data.MainEntities.WorkflowInboxMessage m) => new()
    {
        Id = m.Id,
        ActivityInstanceId = m.ActivityInstanceId,
        ActivityTypeName = m.ActivityTypeName,
        CorrelationId = m.CorrelationId,
        CreatedAt = m.CreatedAt,
        ExpiresAt = m.ExpiresAt,
        Hash = m.Hash,
        SerializedBookmarkPayload = m.SerializedBookmarkPayload,
        SerializedInput = m.SerializedInput,
        TenantId = m.TenantId,
        WorkflowInstanceId = m.WorkflowInstanceId,
    };
}
