namespace Tajan.Workflows.Extensions.Options;

public class ArchiveOptions
{
    /// <summary>
    /// Delete the source rows from Elsa's main tables after they are copied to the archive.
    /// </summary>
    public bool PurgeMainAfterArchive { get; set; } = false;

    /// <summary>
    /// Create the archive tables on startup when they do not exist.
    /// </summary>
    public bool AutoCreateTables { get; set; } = true;

    /// <summary>
    /// Create (or update) the auditing stored procedures on startup.
    /// </summary>
    public bool AutoCreateAuditingProcedures { get; set; } = true;

    /// <summary>
    /// Delete archived workflow instances older than this many days. 0 disables retention cleanup.
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// How often the retention background service runs.
    /// </summary>
    public int RetentionScanHours { get; set; } = 24;
}

public class HooksOptions
{
    /// <summary>
    /// Persist the workflow instance after every executed activity (heavy; legacy behavior of the 3.5 project).
    /// </summary>
    public bool PersistOnActivityExecuted { get; set; } = true;
}
