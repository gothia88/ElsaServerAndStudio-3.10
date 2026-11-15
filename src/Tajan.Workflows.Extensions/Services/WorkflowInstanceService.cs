using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Tajan.Workflows.Extensions.Data.ArchiveEntities;
using Tajan.Workflows.Extensions.Data.MainEntities;
using Tajan.Workflows.Extensions.Dtos;

namespace Tajan.Workflows.Extensions.Services;

public class WorkflowInstanceService : IWorkflowInstanceService
{
    private readonly ElsaMainDataDbContext _main;
    private readonly ElsaArchiveDataDbContext _archive;

    public WorkflowInstanceService(ElsaMainDataDbContext main, ElsaArchiveDataDbContext archive)
    {
        _main = main;
        _archive = archive;
    }

    public async Task<WorkflowInstanceHistory?> GetWorkflowInstanceHistory(string id, CancellationToken cancellationToken = default)
    {
        // Prefer the archive; fall back to the live tables.
        var archivedInstance = await _archive.WorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (archivedInstance is not null)
        {
            var archivedRecords = await _archive.ActivityExecutionRecords
                .AsNoTracking()
                .Join(
                    _archive.WorkflowExecutionLogRecords.AsNoTracking(),
                    aer => aer.Id,
                    welr => welr.ActivityInstanceId,
                    (aer, welr) => new WorkflowInstanceHistoryRecord
                    {
                        WorkflowInstanceId = aer.WorkflowInstanceId,
                        WorkflowDefinitionId = welr.WorkflowDefinitionId,
                        ActivityId = aer.ActivityId,
                        ActivityType = aer.ActivityType,
                        ActivityName = aer.ActivityName,
                        StartedAt = aer.StartedAt,
                        HasBookmarks = aer.HasBookmarks,
                        Status = aer.Status,
                        CompletedAt = aer.CompletedAt,
                        SerializedActivityState = aer.SerializedActivityState,
                        SerializedException = aer.SerializedException,
                        SerializedOutputs = aer.SerializedOutputs,
                        SerializedPayload = aer.SerializedPayload,
                        SerializedProperties = aer.SerializedProperties,
                        WorkflowDefinitionVersionId = welr.WorkflowDefinitionVersionId,
                        Sequence = welr.Sequence,
                        EventName = welr.EventName,
                        TenantId = welr.TenantId,
                    })
                .Where(r => r.WorkflowInstanceId == id)
                .OrderBy(r => r.Sequence)
                .ToListAsync(cancellationToken);

            return new WorkflowInstanceHistory { WorkflowInstanceHistoryRecords = archivedRecords };
        }

        var mainInstance = await _main.WorkflowInstances
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (mainInstance is null)
            return null;

        var mainRecords = await _main.ActivityExecutionRecords
            .AsNoTracking()
            .Join(
                _main.WorkflowExecutionLogRecords.AsNoTracking(),
                aer => aer.Id,
                welr => welr.ActivityInstanceId,
                (aer, welr) => new WorkflowInstanceHistoryRecord
                {
                    WorkflowInstanceId = aer.WorkflowInstanceId,
                    WorkflowDefinitionId = welr.WorkflowDefinitionId,
                    ActivityId = aer.ActivityId,
                    ActivityType = aer.ActivityType,
                    ActivityName = aer.ActivityName,
                    StartedAt = aer.StartedAt,
                    HasBookmarks = aer.HasBookmarks,
                    Status = aer.Status,
                    CompletedAt = aer.CompletedAt,
                    SerializedActivityState = aer.SerializedActivityState,
                    SerializedException = aer.SerializedException,
                    SerializedOutputs = aer.SerializedOutputs,
                    SerializedPayload = aer.SerializedPayload,
                    SerializedProperties = aer.SerializedProperties,
                    WorkflowDefinitionVersionId = welr.WorkflowDefinitionVersionId,
                    Sequence = welr.Sequence,
                    EventName = welr.EventName,
                    TenantId = welr.TenantId,
                })
            .Where(r => r.WorkflowInstanceId == id)
            .OrderBy(r => r.Sequence)
            .ToListAsync(cancellationToken);

        return new WorkflowInstanceHistory { WorkflowInstanceHistoryRecords = mainRecords };
    }

    public async Task<WorkflowInstanceBusinessHistory?> GetWorkflowInstanceBusinessHistory(string id, CancellationToken cancellationToken = default)
    {
        var history = await GetWorkflowInstanceHistory(id, cancellationToken);
        if (history is null || history.Count == 0)
            return null;

        var definitionVersionId = history.WorkflowInstanceHistoryRecords.First().WorkflowDefinitionVersionId;

        var workflowDefinition = await _main.WorkflowDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(wd => wd.Id == definitionVersionId, cancellationToken);

        if (workflowDefinition?.StringData is null)
            return null;

        var workflowDefinitionData = JsonSerializer.Deserialize<StringData>(workflowDefinition.StringData);
        if (workflowDefinitionData?.Activities is null)
            return null;

        var recordsWithActivityInfo = history.WorkflowInstanceHistoryRecords
            .GroupJoin(
                workflowDefinitionData.Activities,
                record => record.ActivityId,
                activity => activity.Id,
                (record, activities) => new { Activities = activities, Record = record })
            .Select(item => new
            {
                ActivityInfo = item.Activities.FirstOrDefault(),
                WorkflowInstanceHistoryRecord = item.Record,
            })
            .ToList();

        return new WorkflowInstanceBusinessHistory
        {
            WorkflowInstanceBusinessHistoryRecords = recordsWithActivityInfo
                .GroupBy(item => item.ActivityInfo?.MetaDataValue?.Description)
                .Select(group => new WorkflowInstanceBusinessHistoryRecord
                {
                    Process = group.Key,
                    WorkflowInstanceHistoryRecords = group.Select(item => item.WorkflowInstanceHistoryRecord).ToList(),
                })
                .ToList(),
        };
    }

    public async Task<List<WorkflowInstanceWithDescription>> GetWorkflowInstancesWithAuditingAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _main.Database
            .SqlQuery<WorkflowInstanceWithDescription>($"""
                EXEC [Elsa].[sp_GetWorkflowInstanceWithAuditing]
                    @Id = {id}
                """)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowInstanceWithDescription>> GetBusinessWorkflowInstancesWithAuditingAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _main.Database
            .SqlQuery<WorkflowInstanceWithDescription>($"""
                EXEC [Elsa].[sp_GetBusinessWorkflowInstanceWithAuditing]
                    @Id = {id}
                """)
            .ToListAsync(cancellationToken);
    }

    public class StringData
    {
        [JsonPropertyName("activities")]
        public List<Activity> Activities { get; set; } = [];

        public class Activity
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("metadata")]
            public MetaData? MetaDataValue { get; set; }

            public class MetaData
            {
                [JsonPropertyName("description")]
                public string? Description { get; set; }
            }
        }
    }
}
