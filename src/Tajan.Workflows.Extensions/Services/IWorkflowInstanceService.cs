using Tajan.Workflows.Extensions.Dtos;

namespace Tajan.Workflows.Extensions.Services;

public interface IWorkflowInstanceService
{
    Task<WorkflowInstanceHistory?> GetWorkflowInstanceHistory(string id, CancellationToken cancellationToken = default);
    Task<WorkflowInstanceBusinessHistory?> GetWorkflowInstanceBusinessHistory(string id, CancellationToken cancellationToken = default);
    Task<List<WorkflowInstanceWithDescription>> GetWorkflowInstancesWithAuditingAsync(string id, CancellationToken cancellationToken = default);
    Task<List<WorkflowInstanceWithDescription>> GetBusinessWorkflowInstancesWithAuditingAsync(string id, CancellationToken cancellationToken = default);
}
