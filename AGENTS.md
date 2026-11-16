# AGENTS.md — ElsaServerAndStudio 3.10

Rebuild of the `ElsaRamezaniTeam/ElsaServerAndStudio` project on **Elsa Workflows 3.10.1 / .NET 10**,
restoring the original behavior: a single host serves Studio + the Elsa API, plus the custom hooks,
SQL logging, file storage, archive/history, and the configured identities. See `README.md` for details.

This is a **standalone folder** under `/development`, not part of the `ElsaRamezaniTeam` git repo.

## Layout

| Project | Purpose |
|---|---|
| `src/Tajan.Workflows.Host` | Elsa server **and in-process Studio UI**, identity, multitenancy, Serilog, hooks, custom auth |
| `src/ElsaStudio` | Blazor WebAssembly Studio UI, served by the host via `Pages/_Host.cshtml` |
| `src/Tajan.Workflows.Extensions` | EF Core archive + history services, entities, DTOs |
| `src/Tajan.Workflows.Extensions.Api` | Standalone workflow history/auditing REST API (port 16000) |

Studio is **not** a separate container. The host references `ElsaStudio`, calls
`UseStaticWebAssets()` / `UseBlazorFrameworkFiles()` / `MapFallbackToPage("/_Host")` and serves it on
port 15000 together with `/elsa/api`.

## Build & run (verified)

- Build: `dotnet build ElsaServerAndStudio.sln` → **0 warnings / 0 errors**.
- Host (API + Studio): `dotnet run --project src/Tajan.Workflows.Host` → http://localhost:15000.
  Needs SQL Server via `ConnectionStrings__Default`; Elsa's tenant activation blocks startup without it.
- History API: `dotnet run --project src/Tajan.Workflows.Extensions.Api` → http://localhost:15001.
- Studio standalone dev: `dotnet run --project src/ElsaStudio` (uses `wwwroot/index.html`, which injects
  `getClientConfig` pointing at `http://localhost:15000/elsa/api`).
- Docker: `docker compose up --build` → host+Studio 15000, history API 16000.
- **No test project** exists — `dotnet test` runs nothing.

## Elsa 3.10 API facts (do not "fix" these to older APIs)

- Packages are pinned centrally in `Directory.Packages.props`; all Elsa/Studio packages must stay on **3.10.1**.
- Module package names changed: `Elsa.Expressions.CSharp/JavaScript/Liquid`, `Elsa.Persistence.EFCore.*`.
  File storage is now `Elsa.Storage.Files` and still exposes `.UseFileStorage()`.
- APIs use: `app.MapWorkflowsApi(routePrefix)`, `elsa.UseTenants(...)`, and
  `options.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder().Append<ClaimsTenantResolver>()`
  (assignment, not `.Append` on an existing builder).
- `System.Security.Cryptography.Pkcs` is overridden to 10.0.15 in central package management to remove a
  vulnerable transitive from `Microsoft.Data.SqlClient` — keep it.

## Registration / lifetime gotchas

- `ArchiveSchemaInitializer`, `AuditingProceduresInitializer` and `ArchiveRetentionService` are
  **singleton hosted services**. They must resolve `DbContext`s through `IServiceScopeFactory`;
  injecting a scoped `DbContext` directly fails startup validation.
- Notification handlers are discovered via `services.AddNotificationHandlersFrom<Program>()`.
- `services.AddHttpClient()` is required for `ClientCredentialsAuthenticationHandler`.
- The Studio WASM client calls `getClientConfig` at startup. `Pages/_Host.cshtml` defines it with the
  host's API URL; `src/ElsaStudio/wwwroot/index.html` defines a localhost fallback for standalone dev.

## Data model (critical)

- Both `ElsaMainDataDbContext` and `ElsaArchiveDataDbContext` use the **same** `ConnectionStrings:Default`.
- Main entities map to schema **`Elsa`**; archive entities map to default **`dbo`**.
- `Archive:PurgeMainAfterArchive` defaults to **false** so finished instances stay in Elsa's live tables
  (Studio instance views keep working). Set it to true only if you intend to purge after copying.
- `ArchiveService` is idempotent and transactional, and supports retention. `ArchiveSchemaInitializer`
  creates the `dbo` archive tables when missing.
- The `Data/MainEntities/*` and `Data/ArchiveEntities/*` POCOs are **hand-written mirrors of Elsa's EF schema**.
  Re-sync them after any Elsa upgrade.
- The auditing stored procedures live in `sql/*.sql`, are embedded into `Tajan.Workflows.Extensions`, and
  are created/updated at startup by `AuditingProceduresInitializer`
  (`Archive:AutoCreateAuditingProcedures=true`). The business procedure needs SQL Server 2022+
  (`LAST_VALUE ... IGNORE NULLS`).

## Logging / storage

- Serilog reads `appsettings.json`; the SQL Server sink (`dbo.ElsaServerLogs`) is added in code **only**
  when `ConnectionStrings:Logs` is non-empty. No connection string is committed.
- `.UseFileStorage()` is registered (defaults to a temp directory via `Elsa.Storage.Files`).

## Security / config

- Never commit secrets. Production requires `Identity__Tokens__SigningKey` (≥32 printable ASCII chars).
- The three legacy users and the `Postman` application live in the **gitignored**
  `src/Tajan.Workflows.Host/appsettings.Identity.json` (loaded when present). The committed
  `appsettings.Identity.example.json` is a template only.
- 3.9+ removed the localhost/security-root bootstrap — seed an admin via `DefaultAdminUser` or an API key.
- The history API returns HTTP 200 with a `StandardResponse<T>` envelope (empty history reported in the
  body) and HTTP 200 arrays for auditing, matching the original. Do not change these to 204.
- Known weaknesses inherited from the original: wide-open CORS and per-request auth server calls.
