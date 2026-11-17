# Agent Guide: Enable BPMN 2.0 Import/Export & Runtime Support in Elsa 3.10 Server

## Objective
Configure the Elsa 3.10 ASP.NET Core Server host to support BPMN 2.0 XML analysis, import, runtime execution, and export (`Elsa.Bpmn.Interchange`), and verify that the `/elsa/api/bpmn/analyze` endpoint returns `200 OK` instead of `400 Bad Request`.

## Target Architecture & Context
- **Framework:** .NET / ASP.NET Core with Elsa Workflows `3.10.x`
- **Problem:** Elsa Studio displays the **Import BPMN** UI, but `POST /elsa/api/bpmn/analyze` fails with `400 Bad Request` because `Elsa.Bpmn.Interchange` is not installed or registered in the Elsa Server DI container.
- **Constraints:**
  - Do not break existing Elsa persistence, API (`UseWorkflowsApi`), Identity, or custom activity registrations.
  - Match the existing Elsa NuGet package version (`3.10.*`) used across the solution.

---

## Step-by-Step Execution Plan

### 1. Locate the Elsa Server Host & Check Package Versions
1. Search the solution for `.csproj` files referencing `Elsa` or `Elsa.Workflows.Core` (typically the Elsa Server host project).
2. Inspect the exact `Elsa.*` package version used in the project (or `Directory.Packages.props` if Central Package Management is enabled).
3. Check whether `Elsa.Bpmn` or `Elsa.Bpmn.Interchange` is already referenced.

### 2. Install `Elsa.Bpmn.Interchange`
- If `Elsa.Bpmn.Interchange` is missing, add it using the **exact same version** as the existing `Elsa.*` packages:
  ```bash
  dotnet add <PathToElsaServer.csproj> package Elsa.Bpmn.Interchange --version <InstalledElsaVersion>
  ```
  *(Note: `Elsa.Bpmn.Interchange` transitively brings in `Elsa.Bpmn`.)*

### 3. Register BPMN Services in Elsa Server Startup
1. Locate where `services.AddElsa(elsa => { ... })` is configured (usually in `Program.cs`, `Startup.cs`, or a dedicated Elsa service extension method).
2. Ensure `elsa.UseWorkflowsApi();` is present (required for Studio API endpoints).
3. Add `elsa.UseBpmnInterchange();` inside the `AddElsa` configuration lambda:
   ```csharp
   services.AddElsa(elsa =>
   {
       // Existing persistence, identity, and workflow configurations...
       elsa.UseWorkflowsApi();

       // Enable BPMN 2.0 runtime engine and Studio analyze/import/export endpoints
       elsa.UseBpmnInterchange();
   });
   ```
   *(Note: `UseBpmnInterchange()` automatically calls `UseBpmn()`, so do not duplicate `UseBpmn()` unless specifically required by custom overrides.)*
4. If the server parses external BPMN files exported from Enterprise Architect (`windows-1252` encoding declarations), also register the code pages encoding provider at the top of `Program.cs`:
   ```csharp
   System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
   ```

### 4. Build & Verify Endpoint Availability
1. Run `dotnet build` on the Elsa Server project and confirm zero compilation or package version conflict errors.
2. Start or restart the Elsa Server host (e.g., on `http://localhost:15000`).
3. Verify via Swagger (`/swagger/v1/swagger.json`) or a direct HTTP check that the following routes are registered:
   - `POST /elsa/api/bpmn/analyze`
   - `POST /elsa/api/bpmn/import`
   - `GET /elsa/api/bpmn/definitions/{definitionId}/export`

### 5. Smoke-Test `POST /elsa/api/bpmn/analyze`
1. Create a temporary minimal BPMN 2.0 XML file (`test-smoke.bpmn`, UTF-8 without BOM) with `isExecutable="true"`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL"
                     id="Definitions_Smoke"
                     targetNamespace="http://bpmn.io/schema/bpmn">
     <bpmn:process id="Process_Smoke" name="Smoke Test" isExecutable="true">
       <bpmn:startEvent id="Start_1" />
       <bpmn:userTask id="Task_1" name="Approve" />
       <bpmn:endEvent id="End_1" />
       <bpmn:sequenceFlow id="Flow_1" sourceRef="Start_1" targetRef="Task_1" />
       <bpmn:sequenceFlow id="Flow_2" sourceRef="Task_1" targetRef="End_1" />
     </bpmn:process>
   </bpmn:definitions>
   ```
2. Send a `multipart/form-data` `POST` request to `http://localhost:15000/elsa/api/bpmn/analyze` (including the Bearer token if authentication is enabled).
3. Confirm the response status is `200 OK` and returns a valid analysis JSON payload.
4. Remove `test-smoke.bpmn` once verification passes.

---

## Enterprise Architect `.bpmn` Export Pre-Flight Checklist
When importing `.bpmn` files exported from Enterprise Architect into Elsa Studio:
- **Root Element:** Must be `<bpmn:definitions ...>`, never `<xmi:XMI>`.
- **Executable Flag:** `<bpmn:process ...>` must include `isExecutable="true"` (EA omits this by default, which causes BPMN analyzers to treat the process as non-executable).
- **Encoding:** Must be valid `UTF-8` without BOM (and no non-breaking spaces `\u00A0` in the `<?xml ...?>` declaration).