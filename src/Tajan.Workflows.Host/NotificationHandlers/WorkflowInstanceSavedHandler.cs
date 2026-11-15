using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Management.Notifications;
using Tajan.Workflows.Extensions.Services;

namespace Tajan.Workflows.Host.NotificationHandlers;

/// <summary>
/// Archives a workflow instance once it reaches the Finished state.
/// </summary>
public class WorkflowInstanceSavedHandler : INotificationHandler<WorkflowInstanceSaved>
{
    private readonly IArchiveService _archiveService;
    private readonly ILogger<WorkflowInstanceSavedHandler> _logger;

    public WorkflowInstanceSavedHandler(IArchiveService archiveService, ILogger<WorkflowInstanceSavedHandler> logger)
    {
        _archiveService = archiveService;
        _logger = logger;
    }

    public async Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        if (notification.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;

        try
        {
            await _archiveService.ArchiveWorkflowInstanceAndDependencies(notification.WorkflowInstance.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive workflow instance {WorkflowInstanceId}.", notification.WorkflowInstance.Id);
        }
    }
}
