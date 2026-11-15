using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Notifications;
using Microsoft.Extensions.Options;
using Tajan.Workflows.Extensions.Options;

namespace Tajan.Workflows.Host.NotificationHandlers;

/// <summary>
/// Persists workflow state after each executed activity (legacy behavior inherited from the
/// 3.5 project). This is write-heavy; disable with Hooks:PersistOnActivityExecuted=false.
/// </summary>
public class ActivityExecutedHandler : INotificationHandler<ActivityExecuted>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActivityExecutedHandler> _logger;
    private readonly HooksOptions _options;

    public ActivityExecutedHandler(
        IServiceProvider serviceProvider,
        ILogger<ActivityExecutedHandler> logger,
        IOptions<HooksOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task HandleAsync(ActivityExecuted notification, CancellationToken cancellationToken)
    {
        if (!_options.PersistOnActivityExecuted)
            return;

        try
        {
            var workflowInstanceManager = _serviceProvider.GetRequiredService<IWorkflowInstanceManager>();
            await workflowInstanceManager.SaveAsync(notification.ActivityExecutionContext.WorkflowExecutionContext, cancellationToken);

            var activity = notification.ActivityExecutionContext.Activity;
            if (activity is SetVariable || activity.GetOutputs().Any())
            {
                var variableService = _serviceProvider.GetRequiredService<IVariablePersistenceManager>();
                await variableService.SaveVariablesAsync(notification.ActivityExecutionContext.WorkflowExecutionContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ActivityExecuted state persistence failed.");
        }
    }
}
