# ElsaServerAndStudio 3.10

A rebuild of the `ElsaRamezaniTeam/ElsaServerAndStudio` project on **Elsa Workflows 3.10.1** (.NET 10),
keeping the custom hooks, logging, archive and history features.

## Architecture

Two services, one database:

```
┌────────────────────────────┐        ┌──────────────────────────────────────────┐
│ elsa-studio (port 16000)   │        │ tajan-workflows-host (port 15000)        │
│ official image:            │  HTTP  │ custom Elsa 3.10.1 server:               │
│ elsaworkflows/             │ ─────► │  • workflows API (/elsa/api)             │
│ elsa-server-and-studio-v3  │        │  • custom notification hooks             │
└────────────────────────────┘        │  • archive + history extension           │
                                      │  • custom client-credentials auth         │
                                      │  • Serilog                               │
                                      └──────────────────┬───────────────────────┘
                                                         │
                                              ┌──────────▼───────────┐
                                              │ SQL Server (ElsaDb)  │
                                              │ schema Elsa (hot)    │
                                              │ schema dbo  (archive)│
                                              └──────────────────────┘
```

The Studio choice means the UI comes from Elsa's official combined image, while the custom
server keeps all hooks. Studio is pointed at the custom server with `Backend__Url`.

## Projects

| Project | Purpose |
|---|---|
| `src/Tajan.Workflows.Host` | Elsa server: workflows API, identity, multitenancy, Serilog, notification hooks, custom auth |
| `src/Tajan.Workflows.Extensions` | EF Core archive + history services (`ArchiveService`, `WorkflowInstanceService`, DTOs) |
| `src/Tajan.Workflows.Extensions.Api` | Standalone REST API for workflow history/auditing |

## Prerequisites

- .NET SDK 10
- A SQL Server database reachable with the Elsa schema (`Elsa.*`) already created by Elsa

## Configuration

Never commit secrets. The important settings:

| Setting | Required | Notes |
|---|---|---|
| `ConnectionStrings__Default` | yes | Same DB for main (`Elsa` schema) and archive (`dbo` schema) |
| `Identity__Tokens__SigningKey` | yes (outside Development) | ≥32 printable ASCII chars, no whitespace |
| `DefaultAdminUser__AdminUserName` / `AdminPassword` | recommended | Seeds the first admin (3.9+ removed the localhost bootstrap) |
| `AuthServer__BaseUrl` / `ApplicationId` / `ApplicationSecret` | if using custom auth | External token-info endpoint |
| `Archive__PurgeMainAfterArchive` | no | Default `true`: delete main rows after archiving |
| `Archive__AutoCreateTables` | no | Default `true`: create archive tables on startup |
| `Archive__RetentionDays` | no | `0` disables retention cleanup |
| `Hooks__PersistOnActivityExecuted` | no | Default `true`: legacy save-after-every-activity |
| `JavaScript__AllowClrAccess` | no | 3.10 requires opting in to CLR access in JS |

`appsettings.Development.json` contains a dev-only signing key. Production MUST override it.

## Run locally

```bash
export ConnectionStrings__Default="Server=...;Database=ElsaDb;..."
dotnet run --project src/Tajan.Workflows.Host
```

Optional history API:

```bash
export ConnectionStrings__Default="..."
dotnet run --project src/Tajan.Workflows.Extensions.Api
```

## Run with Docker

```bash
export ELSA_DB_CONNECTION="Server=...;Database=ElsaDb;..."
export ELSA_SIGNING_KEY="<at least 32 printable ASCII chars>"
docker compose up --build
```

- Custom server: http://localhost:15000 (API at `/elsa/api`)
- Studio: http://localhost:16000

## Custom hooks (ported from 3.5.1)

| Hook | Location | Behavior |
|---|---|---|
| `WorkflowInstanceSaved` | `NotificationHandlers/WorkflowInstanceSavedHandler.cs` | Archives an instance when `Status == Finished` |
| `ActivityExecuted` | `NotificationHandlers/ActivityExecutedHandler.cs` | Saves instance/variables after each activity (configurable) |
| Client-credentials auth | `Authentication/ClientCredentialsAuthenticationHandler.cs` | Validates Bearer tokens against `AuthServer` |
| Multitenancy | `Program.cs` | Config tenants + `ClaimsTenantResolver` |
| Serilog | `Program.cs` + `appsettings.json` | Console; add an MSSqlServer sink if needed |

## Archive behavior (fixed vs. 3.5.1)

The original copied finished instances into a second schema but never deleted the source and was
not transactional or idempotent. This version:

1. **Idempotent** — skips instances already present in the archive.
2. **Transactional** — copy + optional purge run inside one ambient transaction (`TransactionScope`).
3. **Purges** — deletes the source rows from `Elsa.*` after a successful copy (`PurgeMainAfterArchive`).
4. **Retention** — `ArchiveRetentionService` deletes archived instances older than `RetentionDays`.
5. **Schema bootstrap** — `ArchiveSchemaInitializer` creates the `dbo` archive tables when missing.

## Differences from the 3.5.1 project

- Packages renamed by Elsa: `Elsa.CSharp` → `Elsa.Expressions.CSharp`, `Elsa.JavaScript` →
  `Elsa.Expressions.JavaScript`, `Elsa.Liquid` → `Elsa.Expressions.Liquid`,
  `Elsa.EntityFrameworkCore.*` → `Elsa.Persistence.EFCore.*`, `Elsa.FileStorage` is no longer used.
- Elsa 3.8/3.9/3.10 hardening: signing key required outside Development; permission format is
  `resource:verb` (``*`` still works); new EF migrations (`RevokedSessions`, per-tenant uniqueness);
  JavaScript CLR access is opt-in; Jint 4.17 behavior changes.
- `app.UseWorkflowsApi()` → `app.MapWorkflowsApi(routePrefix)`.
- The custom `ElsaStudio` Blazor WASM project is replaced by the official combined image.

## Known follow-ups

- **Re-sync the hand-written EF entities** in `Data/MainEntities` against Elsa 3.10's schema
  (e.g. new `WorkflowDefinitions` columns from the `WorkflowDefinitionRegistryGeneration` migration).
- The auditing endpoints call `Elsa.sp_GetWorkflowInstanceWithAuditing` /
  `sp_GetBusinessWorkflowInstanceWithAuditing`; those stored procedures are not in this repo.
- CORS is wide open (`AllowAnyOrigin`) and the client-credentials handler calls the auth server on
  every request — both should be hardened before production.
