using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Activities.Samples.Ca;

/// <summary>
/// Converted from the "TJ.CA Just Business Rule Check" workflow.
/// Calls the CNF business-rule endpoint and branches on <c>data.isValid</c>.
/// </summary>
[Activity("Tajan.CA", "Tajan.CA", "TJ.CA Just Business Rule Check",
    Description = "Calls the CNF business-rule endpoint and completes with True/False.")]
public class CaJustBusinessRuleCheckActivity(IHttpClientFactory httpClientFactory) : CodeActivity
{
    [Input(Description = "The CNF base URL.")]
    public Input<string> BusinessRuleCnfUrl { get; set; } = default!;

    [Input(Description = "The business-rule step definition ID.")]
    public Input<int> BusinessRuleStepDefinitionId { get; set; } = default!;

    [Output(Description = "The parsed response from the business-rule endpoint.")]
    public Output<object> GetBusinessRuleResponseOutput { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cnfUrl = BusinessRuleCnfUrl.Get(context);
        var payload = new
        {
            stepDefinitionId = BusinessRuleStepDefinitionId.Get(context),
            workFLowInstanceId = context.WorkflowExecutionContext.Id,
            version = 1
        };

        var response = await CaHttp.PostJsonAsync(
            httpClientFactory,
            $"{cnfUrl.TrimEnd('/')}/Api/BusinessRule",
            payload,
            context.CancellationToken);

        GetBusinessRuleResponseOutput.Set(context, response);

        await context.CompleteActivityWithOutcomesAsync(CaHttp.IsValid(response) ? "True" : "False");
    }
}
