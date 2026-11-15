# AGENTS.md — ElsaServerAndStudio 3.10

Rebuild of the `ElsaRamezaniTeam/ElsaServerAndStudio` project on **Elsa Workflows 3.10.1 / .NET 10**,
keeping the custom hooks, logging, archive and history features. See `README.md` for the full picture.

This is a **standalone folder** under `/development`, not part of the `ElsaRamezaniTeam` git repo.

## Layout

| Project | Purpose |
|---|---|
| `src/Tajan.Workflows.Host` | Custom Elsa server: workflows API, identity, multitenancy, Serilog, notification hooks, custom auth |
| `src/Tajan.Workflows.Extensions` | EF Core archive + history services, entities, DTOs |
| `src/Tajan.Workflows.Extensions.Api` | Standalone workflow history/auditing REST API |

**Studio is not a project here.** It runs from the official image
`elsaworkflows/elsa-server-and-studio-v3:3.10.1` (docker-compose service `elsa-studio`, port 16000),
pointed at the custom server with `Backend__Url=http://tajan-workflows-host:8080/elsa/api`.

## Build & run (verified)

- Build: `dotnet build ElsaServerAndStudio.sln` → succeeds with **0 warnings / 0 errors**.
- Host: `dotnet run --project src/Tajan.Workflows.Host` (http://localhost:15000). Needs SQL Server via
  `ConnectionStrings__Default`; it registers all Elsa modules and only fails at the DB connection otherwise.
- History API: `dotnet run --project src/Tajan.Workflows.Extensions.Api` (http://localhost:15001).
- Docker: `docker compose up --build` (requires `ELSA_DB_CONNECTION` and `ELSA_SIGNING_KEY`).
- **No test project** exists — `dotnet test` runs nothing.

## Elsa 3.10 API facts (do not "fix" these to older APIs)

- Packages are pinned centrally in `Directory.Packages.props`; all Elsa/Studio packages must stay on **3.10.1**.
- Module package names changed: `Elsa.Expressions.CSharp/JavaScript/Liquid`, `Elsa.Persistence.EFCore.*`
  (`Elsa.CSharp`/`Elsa.JavaScript`/`Elsa.Liquid`/`Elsa.EntityFrameworkCore.*` stop at 3.5.3).
  `Elsa.FileStorage` is not republished — not referenced.
- APIs use: `app.MapWorkflowsApi(routePrefix)`, `elsa.UseTenants(...)`, and
  `options.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder().Append<ClaimsTenantResolver>()`
  (note: assignment, not `.Append` on an existing builder).
- `System.Security.Cryptography.Pkcs` is overridden to 10.0.15 in central package management to remove a
  vulnerable transitive from `Microsoft.Data.SqlClient` — keep it.

## Registration / lifetime gotchas

- `ArchiveSchemaInitializer` and `ArchiveRetentionService` are **singleton hosted services**. They must
  resolve `DbContext`s through `IServiceScopeFactory`; injecting a scoped `DbContext` directly fails
  startup validation ("Cannot consume scoped service from singleton").
- Notification handlers are discovered via `services.AddNotificationHandlersFrom<Program>()`
  (`Elsa.Mediator`). They are registered under `INotificationHandler`, not `INotificationHandler<T>`.
- `services.AddHttpClient()` is required for `ClientCredentialsAuthenticationHandler`'s `IHttpClientFactory`.

## Data model (critical)

- Both `ElsaMainDataDbContext` and `ElsaArchiveDataDbContext` use the **same** `ConnectionStrings:Default`.
- Main entities map to schema **`Elsa`**; archive entities map to default **`dbo`**. "Archive" is a second
  schema in the same database.
- The `Data/MainEntities/*` and `Data/ArchiveEntities/*` POCOs are **hand-written mirrors of Elsa's EF schema**.
  Re-sync them after any Elsa upgrade (3.10 added columns such as `WorkflowDefinitionRegistryGeneration`).
- `ArchiveService` is idempotent, transactional (`TransactionScope`), purges `Elsa.*` when
  `Archive:PurgeMainAfterArchive=true`, and supports retention. `ArchiveSchemaInitializer` creates the
  `dbo` archive tables when missing.
- The auditing endpoints call stored procedures `Elsa.sp_GetWorkflowInstanceWithAuditing` and
  `Elsa.sp_GetBusinessWorkflowInstanceWithAuditing`; they exist only in the database, not the repo.

## Security / config

- Never commit secrets. Production requires `Identity__Tokens__SigningKey` (≥32 printable ASCII chars);
  `appsettings.Development.json` contains a dev-only key.
- 3.9+ removed the localhost/security-root bootstrap — seed an admin via `DefaultAdminUser` or an admin API key.
- Known weaknesses inherited from the original (documented in README): wide-open CORS and per-request auth
  server calls.
