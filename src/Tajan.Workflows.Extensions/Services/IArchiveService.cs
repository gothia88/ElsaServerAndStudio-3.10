namespace Tajan.Workflows.Extensions.Services;

public interface IArchiveService
{
    /// <summary>
    /// Copies a finished workflow instance and its dependencies into the archive tables.
    /// Optionally purges the source rows from Elsa's main tables. Idempotent and transactional.
    /// </summary>
    /// <returns>True when the instance was archived; false when it was missing or already archived.</returns>
    Task<bool> ArchiveWorkflowInstanceAndDependencies(string workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes archived workflow instances (and their dependencies) finished before <paramref name="cutoff"/>.
    /// </summary>
    Task<int> PurgeArchivedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
