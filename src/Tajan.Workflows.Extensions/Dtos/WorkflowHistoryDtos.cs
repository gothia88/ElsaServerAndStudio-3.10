namespace Tajan.Workflows.Extensions.Dtos;

public class WorkflowInstanceHistory
{
    public int Count => WorkflowInstanceHistoryRecords?.Count ?? 0;
    public List<WorkflowInstanceHistoryRecord> WorkflowInstanceHistoryRecords { get; set; } = [];
}

public class WorkflowInstanceHistoryRecord
{
    public string WorkflowInstanceId { get; set; } = null!;
    public string WorkflowDefinitionId { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public string? ActivityName { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public bool HasBookmarks { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? SerializedActivityState { get; set; }
    public string? SerializedException { get; set; }
    public string? SerializedOutputs { get; set; }
    public string? SerializedPayload { get; set; }
    public string? SerializedProperties { get; set; }
    public string WorkflowDefinitionVersionId { get; set; } = null!;
    public long Sequence { get; set; }
    public string? EventName { get; set; }
    public string? TenantId { get; set; }
}

public class WorkflowInstanceBusinessHistory
{
    public int Count => WorkflowInstanceBusinessHistoryRecords?.Count ?? 0;
    public List<WorkflowInstanceBusinessHistoryRecord> WorkflowInstanceBusinessHistoryRecords { get; set; } = [];
}

public class WorkflowInstanceBusinessHistoryRecord
{
    public string? Process { get; set; }
    public int Count => WorkflowInstanceHistoryRecords?.Count ?? 0;
    public List<WorkflowInstanceHistoryRecord> WorkflowInstanceHistoryRecords { get; set; } = [];
}

public sealed class WorkflowInstanceWithDescription
{
    public string Id { get; init; } = default!;
    public string WorkflowDefinitionId { get; init; } = default!;
    public string WorkflowDefinitionVersionId { get; init; } = default!;
    public string WorkflowInstanceId { get; init; } = default!;
    public int WorkflowVersion { get; init; }
    public string ActivityInstanceId { get; init; } = default!;
    public string? ParentActivityInstanceId { get; init; }
    public string ActivityId { get; init; } = default!;
    public string ActivityType { get; init; } = default!;
    public int ActivityTypeVersion { get; init; }
    public string? ActivityName { get; init; }
    public string ActivityNodeId { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; }
    public long Sequence { get; init; }
    public string EventName { get; init; } = default!;
    public string? Message { get; init; }
    public string? Source { get; init; }
    public string? SerializedActivityState { get; init; }
    public string? SerializedPayload { get; init; }
    public string? TenantId { get; init; }
    public string? DisplayText { get; init; }
    public string? Auditing { get; init; }
}
