# Elsa Workflows 3 — Expressions, Persistence & API Reference

Quick reference for building, persisting, and controlling Elsa 3 workflows from a .NET backend.

---

## 1. Workflow Expressions
Elsa 3 supports multiple expression languages inside workflow definitions:
- **Literal**: Static values (`"Approved"`, `42`).
- **C# (`CSharp`)**: `Variables.Get<string>("OrderId")` or `Input.Get<int>("RetryCount")`.
- **JavaScript (`JavaScript`)**: `getVariable('OrderId')`, `setVariable('Status', 'Done')`, `input('Amount') > 1000`.
- **Liquid (`Liquid`)**: `{{ Variables.CustomerName }}` (ideal for email bodies and LLM prompts).

## 2. Dispatching & Resuming Workflows via `IWorkflowDispatcher`
Inject `IWorkflowDispatcher` in your application layer to start workflows asynchronously without blocking HTTP requests:

```csharp
public class RequestWorkflowService(IWorkflowDispatcher dispatcher)
{
    public async Task<string> StartApprovalWorkflowAsync(Guid requestId, CancellationToken ct)
    {
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = "ApprovalWorkflow",
            CorrelationId = requestId.ToString(),
            Input = new Dictionary<string, object>
            {
                ["RequestId"] = requestId
            }
        };

        var result = await dispatcher.DispatchAsync(request, ct);
        return result.InstanceId;
    }
}
```

## 3. Resuming Bookmarks
To resume a suspended activity waiting on a custom stimulus:

```csharp
public class ApprovalCallbackService(IBookmarkQueue bookmarkQueue)
{
    public async Task ResumeApprovalAsync(Guid requestId, bool isApproved, CancellationToken ct)
    {
        var stimulus = new ApprovalStimulus(requestId);
        await bookmarkQueue.EnqueueAsync(new NewBookmarkQueueItem
        {
            ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<RequestApproval>(),
            StimulusHash = Hasher.Hash(stimulus),
            Options = new ResumeBookmarkOptions
            {
                Input = new Dictionary<string, object>
                {
                    ["Approved"] = isApproved
                }
            }
        }, ct);
    }
}
```

## 4. Common Elsa 3 REST API Endpoints
Base path is typically `/elsa/api`:
- `GET /elsa/api/workflow-definitions` — List workflow definitions.
- `POST /elsa/api/workflow-definitions/{definitionId}/dispatch` — Dispatch a new workflow instance.
- `GET /elsa/api/workflow-instances?correlationId={id}` — Query instances by business correlation ID.
- `GET /elsa/api/workflow-instances/{instanceId}` — Retrieve full state, variables, and execution status.
- `POST /elsa/api/workflow-instances/{instanceId}/cancel` — Cancel a running or suspended instance.
- `GET /elsa/api/agents` — List configured AI agents (`Elsa.Agents.Api`).
