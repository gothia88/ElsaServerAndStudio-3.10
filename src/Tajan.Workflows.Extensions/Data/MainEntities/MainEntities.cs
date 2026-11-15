namespace Tajan.Workflows.Extensions.Data.MainEntities;

// Hand-written mirrors of Elsa 3.x EF Core tables in the "Elsa" schema.
// IMPORTANT: re-sync these with Elsa's schema after upgrading Elsa (see README).

public partial class WorkflowInstance
{
    public string Id { get; set; } = null!;
    public string DefinitionId { get; set; } = null!;
    public string DefinitionVersionId { get; set; } = null!;
    public int Version { get; set; }
    public string Status { get; set; } = null!;
    public string SubStatus { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }
    public int IncidentCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Data { get; set; }
    public string? DataCompressionAlgorithm { get; set; }
    public bool IsSystem { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    public string? TenantId { get; set; }
    public bool IsExecuting { get; set; }
}

public partial class WorkflowDefinition
{
    public string Id { get; set; } = null!;
    public string DefinitionId { get; set; } = null!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ToolVersion { get; set; }
    public string? ProviderName { get; set; }
    public string MaterializerName { get; set; } = null!;
    public string? MaterializerContext { get; set; }
    public string? StringData { get; set; }
    public byte[]? BinaryData { get; set; }
    public bool IsReadonly { get; set; }
    public string? Data { get; set; }
    public bool? UsableAsActivity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public bool IsSystem { get; set; }
    public string? TenantId { get; set; }
}

public partial class ActivityExecutionRecord
{
    public string Id { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string ActivityNodeId { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public int ActivityTypeVersion { get; set; }
    public string? ActivityName { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public bool HasBookmarks { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? SerializedActivityState { get; set; }
    public string? SerializedException { get; set; }
    public string? SerializedOutputs { get; set; }
    public string? SerializedPayload { get; set; }
    public string? SerializedActivityStateCompressionAlgorithm { get; set; }
    public string? SerializedProperties { get; set; }
    public string? TenantId { get; set; }
    public int AggregateFaultCount { get; set; }
    public string? SerializedMetadata { get; set; }
}

public partial class WorkflowExecutionLogRecord
{
    public string Id { get; set; } = null!;
    public string WorkflowDefinitionId { get; set; } = null!;
    public string WorkflowDefinitionVersionId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public int WorkflowVersion { get; set; }
    public string ActivityInstanceId { get; set; } = null!;
    public string? ParentActivityInstanceId { get; set; }
    public string ActivityId { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public int ActivityTypeVersion { get; set; }
    public string? ActivityName { get; set; }
    public string ActivityNodeId { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public long Sequence { get; set; }
    public string? EventName { get; set; }
    public string? Message { get; set; }
    public string? Source { get; set; }
    public string? SerializedActivityState { get; set; }
    public string? SerializedPayload { get; set; }
    public string? TenantId { get; set; }
}

public partial class Bookmark
{
    public string Id { get; set; } = null!;
    public string? ActivityInstanceId { get; set; }
    public string? ActivityTypeName { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Hash { get; set; }
    public string? Name { get; set; }
    public string? SerializedMetadata { get; set; }
    public string? SerializedPayload { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowInstanceId { get; set; }
}

public partial class BookmarkQueueItem
{
    public string Id { get; set; } = null!;
    public string? ActivityInstanceId { get; set; }
    public string? ActivityTypeName { get; set; }
    public string? BookmarkId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? SerializedOptions { get; set; }
    public string? StimulusHash { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowInstanceId { get; set; }
}

public partial class WorkflowInboxMessage
{
    public string Id { get; set; } = null!;
    public string? ActivityInstanceId { get; set; }
    public string? ActivityTypeName { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Hash { get; set; }
    public string? SerializedBookmarkPayload { get; set; }
    public string? SerializedInput { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowInstanceId { get; set; }
}

public partial class Trigger
{
    public string Id { get; set; } = null!;
    public string? Hash { get; set; }
    public string? Name { get; set; }
    public string? SerializedPayload { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowDefinitionVersionId { get; set; }
}

public partial class KeyValuePair
{
    public string Id { get; set; } = null!;
    public string? Key { get; set; }
    public string? Value { get; set; }
    public string? TenantId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
