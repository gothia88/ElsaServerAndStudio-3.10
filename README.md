# ElsaServerAndStudio 3.10

A rebuild of the `ElsaRamezaniTeam/ElsaServerAndStudio` project on **Elsa Workflows 3.10.1** (.NET 10)
that keeps the original behavior: a single host serving the Studio UI and the Elsa API, the custom
notification hooks, SQL application logging, file storage, the archive/history extension, and the
complete set of configured identities.

## Architecture

Single host, one database, plus the history API:

```
┌──────────────────────────────────────────────────────────────┐
│ tajan-workflows-host (port 15000)                            │
│  • Elsa 3.10.1 workflows API        (/elsa/api)              │
│  • Elsa Studio (Blazor WASM)        (/)                      │
│  • custom notification hooks                                 │
│  • archive + history extension                               │
│  • client-credentials authentication                         │
│  • Serilog (Console + optional SQL Server sink)              │
│  • file storage (Elsa.Storage.Files)                         │
└───────────────────────────┬──────────────────────────────────┘
                            │
                 ┌──────────▼───────────┐        ┌──────────────────────────────┐
                 │ SQL Server (ElsaDb)  │        │ tajan-workflows-extensions-  │
                 │  Elsa.*  (live)      │◄───────│ api (port 16000)             │
                 │  dbo.*   (archive)   │        │  workflow history/auditing   │
                 └──────────────────────┘        └──────────────────────────────┘
```

## Projects

| Project | Purpose |
|---|---|
| `src/Tajan.Workflows.Host` | Elsa server + in-process Studio UI, identity, multitenancy, Serilog, hooks, custom auth |
| `src/ElsaStudio` | Blazor WebAssembly Studio UI, served by the host |
| `src/Tajan.Workflows.Extensions` | EF Core archive + history services, entities, DTOs |
| `src/Tajan.Workflows.Extensions.Api` | Standalone workflow history/auditing REST API (port 16000) |

## Prerequisites

- .NET SDK 10
- A SQL Server database reachable with the Elsa schema (`Elsa.*`) already created by Elsa

## Configuration

Never commit secrets. Key settings:

| Setting | Required | Notes |
|---|---|---|
| `ConnectionStrings__Default` | yes | Same DB for main (`Elsa` schema) and archive (`dbo` schema) |
| `ConnectionStrings__Logs` | no | Enables the Serilog SQL Server sink (`dbo.ElsaServerLogs`) |
| `Identity__Tokens__SigningKey` | yes (outside Development) | ≥32 printable ASCII chars, no whitespace |
| `DefaultAdminUser__AdminUserName` / `AdminPassword` | recommended | Seeds the first admin |
| `AuthServer__BaseUrl` / `ApplicationId` / `ApplicationSecret` | if using custom auth | External token-info endpoint |
| `Archive__PurgeMainAfterArchive` | no | Default `false`: keep finished instances in Elsa's tables so Studio still lists them |
| `Archive__AutoCreateTables` | no | Default `true`: create archive tables on startup |
| `Archive__RetentionDays` | no | `0` disables retention cleanup |
| `Hooks__PersistOnActivityExecuted` | no | Default `true`: legacy save-after-every-activity |
| `JavaScript__AllowClrAccess` | no | 3.10 requires opting in to CLR access in JS |

`appsettings.Development.json` contains a dev-only signing key. Production MUST override it.

### Identities (restored)

The original three users (`rahman`, `admin`, `alice`) and one application (`Postman`) are provided via
`appsettings.Identity.json`, which is **gitignored** and loaded when present:

```
src/Tajan.Workflows.Host/appsettings.Identity.json
```

A committed template is at `appsettings.Identity.example.json`. Provide the real file locally or mount it
into the container; never commit it. The `admin` application role was corrected from the legacy
non-existent `admin` role to `admin-1`.

## Run locally

```bash
export ConnectionStrings__Default="Server=...;Database=ElsaDb;..."
dotnet run --project src/Tajan.Workflows.Host
```

- Studio + API: http://localhost:15000
- History API: `dotnet run --project src/Tajan.Workflows.Extensions.Api` → http://localhost:15001

## Run with Docker

```bash
export ELSA_DB_CONNECTION="Server=...;Database=ElsaDb;..."
export ELSA_SIGNING_KEY="<at least 32 printable ASCII chars>"
# optional: export ELSA_LOG_DB_CONNECTION="Server=...;Database=LogDb;..."
docker compose up --build
```

- Host + Studio: http://localhost:15000
- History API: http://localhost:16000 (restored to its original port)

## Custom hooks (ported from 3.5.1)

| Hook | Location | Behavior |
|---|---|---|
| `WorkflowInstanceSaved` | `NotificationHandlers/WorkflowInstanceSavedHandler.cs` | Archives an instance when `Status == Finished` |
| `ActivityExecuted` | `NotificationHandlers/ActivityExecutedHandler.cs` | Saves instance/variables after each activity (configurable) |
| Client-credentials auth | `Authentication/ClientCredentialsAuthenticationHandler.cs` | Validates Bearer tokens against `AuthServer` |
| Multitenancy | `Program.cs` | Config tenants + `ClaimsTenantResolver` |
| Serilog | `Program.cs` + `appsettings.json` | Console + optional SQL Server sink |
| File storage | `Program.cs` | `.UseFileStorage()` via `Elsa.Storage.Files` |

## Archive behavior (restored)

By default `Archive__PurgeMainAfterArchive` is **false**, matching the original: finished instances are
**copied** to the archive and remain in Elsa's live tables, so Studio's normal instance views still work.
Set it to `true` if you want a purge-after-copy workflow. The service also adds:

1. **Idempotency** — skips instances already archived.
2. **Transactions** — copy (and optional purge) run in one ambient transaction.
3. **Retention** — `ArchiveRetentionService` can delete old archived instances.
4. **Schema bootstrap** — `ArchiveSchemaInitializer` creates the `dbo` archive tables if missing.

## History API responses (restored)

- `.../history` and `.../business-history`: HTTP **200** with a `StandardResponse<T>` envelope; missing
  history is reported inside the envelope (status code `204` in the body), matching the original.
- `.../history-definition-auditing` and `.../business-history-definition-auditing`: HTTP **200** with an
  array (empty array when nothing is found).

## Differences from the 3.5.1 project

- Packages renamed by Elsa: `Elsa.CSharp` → `Elsa.Expressions.CSharp`, `Elsa.JavaScript` →
  `Elsa.Expressions.JavaScript`, `Elsa.Liquid` → `Elsa.Expressions.Liquid`,
  `Elsa.EntityFrameworkCore.*` → `Elsa.Persistence.EFCore.*`, `Elsa.FileStorage` → `Elsa.Storage.Files`.
- Elsa 3.8/3.9/3.10 hardening: signing key required outside Development; permission format is
  `resource:verb` (`*` still works); new EF migrations; JavaScript CLR access is opt-in.
- `app.UseWorkflowsApi()` → `app.MapWorkflowsApi(routePrefix)`.
- The custom Studio UI is a Blazor WASM project served by the host (single process), as in the original.

## Known follow-ups

- Re-sync the hand-written EF entities in `Data/MainEntities` against Elsa 3.10's schema.
- The auditing endpoints call `Elsa.sp_GetWorkflowInstanceWithAuditing` /
  `sp_GetBusinessWorkflowInstanceWithAuditing`; those stored procedures are not in this repo.
- CORS is wide open and the client-credentials handler calls the auth server on every request.
- Add automated tests for archive idempotency, purge, and retention.
