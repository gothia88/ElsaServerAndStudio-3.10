# Elsa 3 AI Agents Guide (`Elsa.Agents` & Semantic Kernel)

This guide covers configuring and orchestrating autonomous AI agents inside **Elsa Workflows 3** using the `Elsa.Agents` packages built on Microsoft Semantic Kernel.

---

## 1. Required NuGet Packages
Install the core agent packages along with your chosen LLM provider and persistence store:

```bash
dotnet add package Elsa.Agents
dotnet add package Elsa.Agents.Api
dotnet add package Elsa.Agents.OpenAI        # Or Elsa.Agents.AzureOpenAI
dotnet add package Elsa.Agents.Persistence.EntityFrameworkCore
dotnet add package Elsa.Agents.Persistence.EntityFrameworkCore.SqlServer
```

For **Elsa Studio** UI management of agents, skills, and API keys:
```bash
dotnet add package Elsa.Studio.Agents
```

## 2. Service Registration (`Program.cs`)
Configure `AddElsa` with the Agents feature, EF Core persistence, and LLM provider connectors:

```csharp
builder.Services.AddElsa(elsa =>
{
    // Enable Elsa Agents & REST API
    elsa.UseAgents(agents =>
    {
        agents.UseAgentPersistence(persistence =>
            persistence.UseEntityFrameworkCore(ef =>
                ef.UseSqlServer(connectionString)));
    });

    elsa.UseAgentsApi();

    // Configure OpenAI / OpenAI-Compatible Endpoint
    elsa.UseOpenAI(openai =>
    {
        openai.ApiKey = builder.Configuration["OpenAI:ApiKey"]!;
        // Optional: custom OpenAI-compatible gateway URL
        // openai.Endpoint = new Uri("https://api.example.com/v1");
    });
});
```

### Registering in Elsa Studio (`Program.cs`)
```csharp
builder.Services.AddAgentsModule();
```

## 3. Core Concepts in `Elsa.Agents`
1. **Services (Connectors)**: Define connection credentials and endpoints (e.g., OpenAI, Azure OpenAI, custom OpenAI-compatible gateways).
2. **Agents**: Named AI personas configured with:
   - **System Prompt / Instructions**: Defines role, constraints, and behavior.
   - **Prompt Template**: Supports **Handlebars** syntax (e.g., `Analyze this request: {{InputText}}`).
   - **Input Variables**: Automatically exposed as strongly-typed activity inputs in the Elsa workflow designer.
   - **Output Schema / Structured JSON**: Maps LLM JSON responses into typed workflow variables or activity outputs.
   - **Execution Settings**: Temperature, TopP, MaxTokens, and Model ID.
3. **Plugins & Tools (Semantic Kernel Functions)**: Attach C# functions or HTTP/OpenAPI plugins so the agent can query databases, invoke APIs, or trigger sub-workflows during reasoning.

## 4. Dynamic Activity Generation
- Every agent saved in Elsa (via Elsa Studio or the `/elsa/api/agents` endpoint) is dynamically projected as a first-class **Workflow Activity** in the activity registry.
- When placed on a workflow canvas or referenced in a workflow definition:
  - Its configured input variables appear as standard Elsa `Input<T>` fields (supporting C#, JavaScript, or Liquid expressions).
  - Its response is emitted to `Output` and can drive conditional branching (`If`, `Switch`, `FlowSwitch`).

## 5. Multi-Agent Workflow Patterns
- **Sequential Pipeline**: `ExtractorAgent` -> `ValidatorAgent` -> `SummarizerAgent` passing structured JSON outputs via workflow variables.
- **Human-in-the-Loop (HITL)**: `DraftingAgent` generates a proposal -> `RequestApproval` (Bookmark / HTTP Endpoint) pauses for human review -> `PublisherAgent` executes upon approval.
- **Router / Triage Pattern**: `ClassifierAgent` outputs an intent category -> `Switch` activity routes execution to specialized domain workflows.
