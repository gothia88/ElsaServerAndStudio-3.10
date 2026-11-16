using System.Net;
using Microsoft.AspNetCore.Mvc;
using Tajan.Workflows.Extensions.Api.Models;
using Tajan.Workflows.Extensions.Dtos;
using Tajan.Workflows.Extensions.Services;

namespace Tajan.Workflows.Extensions.Api.Controllers;

[ApiController]
[Route("extension/api/[controller]")]
// Preserved for backwards compatibility with clients using the original (typo'd) route.
[Route("extention/api/[controller]")]
public class WorkflowInstancesController : ControllerBase
{
    private readonly IWorkflowInstanceService _workflowInstanceService;

    public WorkflowInstancesController(IWorkflowInstanceService workflowInstanceService)
    {
        _workflowInstanceService = workflowInstanceService;
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetWorkflowInstanceHistory(string id, CancellationToken cancellationToken)
    {
        var result = await _workflowInstanceService.GetWorkflowInstanceHistory(id, cancellationToken);

        // Original behavior: always HTTP 200; empty history is reported inside the envelope.
        if (result is null || result.Count == 0)
        {
            return Ok(new StandardResponse<WorkflowInstanceHistory>(
                HttpStatusCode.NoContent,
                "There is not any history for this workflow instance id.",
                null));
        }

        return Ok(new StandardResponse<WorkflowInstanceHistory>(result));
    }

    [HttpGet("{id}/business-history")]
    public async Task<IActionResult> GetWorkflowInstanceBusinessHistory(string id, CancellationToken cancellationToken)
    {
        var result = await _workflowInstanceService.GetWorkflowInstanceBusinessHistory(id, cancellationToken);

        if (result is null || result.Count == 0)
        {
            return Ok(new StandardResponse<WorkflowInstanceBusinessHistory>(
                HttpStatusCode.NoContent,
                "There is not any history for this workflow instance id.",
                null));
        }

        return Ok(new StandardResponse<WorkflowInstanceBusinessHistory>(result));
    }

    [HttpGet("{id}/history-definition-auditing")]
    public async Task<IActionResult> GetWorkflowInstanceHistoryWithAuditing(string id, CancellationToken cancellationToken)
    {
        // Original behavior: HTTP 200 with an array (empty array when nothing is found).
        var result = await _workflowInstanceService.GetWorkflowInstancesWithAuditingAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/business-history-definition-auditing")]
    public async Task<IActionResult> GetBusinessWorkflowInstanceHistoryWithAuditing(string id, CancellationToken cancellationToken)
    {
        var result = await _workflowInstanceService.GetBusinessWorkflowInstancesWithAuditingAsync(id, cancellationToken);
        return Ok(result);
    }
}
