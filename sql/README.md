# Auditing stored procedures

The history/auditing endpoints call two stored procedures in the Elsa database:

- `Elsa.sp_GetWorkflowInstanceWithAuditing`
- `Elsa.sp_GetBusinessWorkflowInstanceWithAuditing`

Both scripts live here and are embedded into `Tajan.Workflows.Extensions`. On startup the host
runs them idempotently (`CREATE OR ALTER`) when:

```json
"Archive": { "AutoCreateAuditingProcedures": true }
```

That is the default. Set it to `false` if your DBA manages database objects and you prefer to
deploy the scripts yourself.

## Manual deployment

Run both files against the Elsa database (the same database as `ConnectionStrings:Default`):

```
sqlcmd -S <server> -d <ElsaDb> -i sql/sp_GetWorkflowInstanceWithAuditing.sql
sqlcmd -S <server> -d <ElsaDb> -i sql/sp_GetBusinessWorkflowInstanceWithAuditing.sql
```

The scripts reference both schemas:

- `Elsa.*` — Elsa's live tables
- `dbo.*`  — the archive tables created by `ArchiveSchemaInitializer`

## Requirements

- SQL Server 2016+ for `OPENJSON`.
- The business procedure uses `LAST_VALUE(...) IGNORE NULLS`, which requires **SQL Server 2022+**
  (or Azure SQL Database). On older versions, replace it with a windowed `MAX(...) OVER (...)` or
  a recursive CTE.
