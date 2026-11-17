using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Activities.Samples.Ca;

/// <summary>
/// Converted from the "TJ.CA Business Rule Check With Change State without EndPoint" workflow.
/// Calls the business rule, changes state to the true/false result and completes with the outcome.
/// </summary>
[Activity("Tajan.CA", "Tajan.CA", "TJ.CA Business Rule Check With Change State without EndPoint",
    Description = "Runs the business-rule check and changes state without waiting for an HTTP endpoint.")]
public class CaBusinessRuleCheckWithChangeStateActivity(IHttpClientFactory httpClientFactory) : CodeActivity
{
    [Input] public Input<string> ChangeStateUrl { get; set; } = default!;
    [Input] public Input<string> BusinessRuleCnfUrl { get; set; } = default!;
    [Input] public Input<int> BusinessRuleStepDefinitionId { get; set; } = default!;
    [Input] public Input<int> FalseResultForBusinessRuleFlowStateId { get; set; } = default!;
    [Input] public Input<int> FalseResultBusinessRuleStepDefinitionActionId { get; set; } = default!;
    [Input] public Input<int> TrueResultForBusinessRuleFlowStateId { get; set; } = default!;
    [Input] public Input<int> TrueResultBusinessRuleStepDefinitionActionId { get; set; } = default!;
    [Input] public Input<long> FlowStateChangedBy { get; set; } = default!;
    [Input] public Input<long> AssignedTo { get; set; } = default!;
    [Input] public Input<int> UnitId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var instanceId = context.WorkflowExecutionContext.Id;
        var changeStateUrl = $"{ChangeStateUrl.Get(context).TrimEnd('/')}/Api/Request/ChangeFlowState";
        var cnfUrl = BusinessRuleCnfUrl.Get(context);

        var response = await CaHttp.PostJsonAsync(
            httpClientFactory,
            $"{cnfUrl.TrimEnd('/')}/Api/BusinessRule",
            new
            {
                stepDefinitionId = BusinessRuleStepDefinitionId.Get(context),
                workFLowInstanceId = instanceId,
                version = 1
            },
            context.CancellationToken);

        if (CaHttp.IsValid(response))
        {
            await PostChangeStateAsync(
                context,
                changeStateUrl,
                instanceId,
                TrueResultForBusinessRuleFlowStateId.Get(context),
                TrueResultBusinessRuleStepDefinitionActionId.Get(context),
                string.Empty);

            await context.CompleteActivityWithOutcomesAsync("BusinessRuleTrue");
        }
        else
        {
            await PostChangeStateAsync(
                context,
                changeStateUrl,
                instanceId,
                FalseResultForBusinessRuleFlowStateId.Get(context),
                FalseResultBusinessRuleStepDefinitionActionId.Get(context),
                CaHttp.GetErrorMessage(response));

            await context.CompleteActivityWithOutcomesAsync("BusinessRuleFalse");
        }
    }

    private Task PostChangeStateAsync(
        ActivityExecutionContext context,
        string url,
        string instanceId,
        int flowStateId,
        int stepDefinitionActionId,
        string reason) =>
        CaHttp.PostAsync(
            httpClientFactory,
            url,
            new
            {
                workflowInstanceId = instanceId,
                flowStateId,
                flowStateChangedBy = FlowStateChangedBy.Get(context),
                assignedTo = AssignedTo.Get(context),
                unitId = UnitId.Get(context),
                stepDefinitionActionId,
                reason,
                updatedBy = ""
            },
            context.CancellationToken);
}
