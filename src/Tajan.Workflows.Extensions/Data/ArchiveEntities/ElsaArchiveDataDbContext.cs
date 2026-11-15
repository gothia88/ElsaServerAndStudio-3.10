using Microsoft.EntityFrameworkCore;

namespace Tajan.Workflows.Extensions.Data.ArchiveEntities;

/// <summary>
/// Archive tables. They live in the same database as Elsa's main tables, but in the
/// default "dbo" schema (main tables use the "Elsa" schema).
/// </summary>
public class ElsaArchiveDataDbContext(DbContextOptions<ElsaArchiveDataDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<ActivityExecutionRecord> ActivityExecutionRecords => Set<ActivityExecutionRecord>();
    public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords => Set<WorkflowExecutionLogRecord>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<BookmarkQueueItem> BookmarkQueueItems => Set<BookmarkQueueItem>();
    public DbSet<WorkflowInboxMessage> WorkflowInboxMessages => Set<WorkflowInboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowInstance>(e =>
        {
            e.ToTable("WorkflowInstances");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.DefinitionId).HasMaxLength(450);
            e.Property(x => x.CorrelationId).HasMaxLength(450);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Status).HasMaxLength(450);
            e.Property(x => x.SubStatus).HasMaxLength(450);
            e.Property(x => x.TenantId).HasMaxLength(450);
            e.HasIndex(x => x.FinishedAt);
        });
        modelBuilder.Entity<ActivityExecutionRecord>(e =>
        {
            e.ToTable("ActivityExecutionRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityId).HasMaxLength(450);
            e.Property(x => x.ActivityNodeId).HasMaxLength(450);
            e.Property(x => x.ActivityType).HasMaxLength(450);
            e.Property(x => x.ActivityName).HasMaxLength(450);
            e.Property(x => x.Status).HasMaxLength(450);
            e.Property(x => x.TenantId).HasMaxLength(450);
        });
        modelBuilder.Entity<WorkflowExecutionLogRecord>(e =>
        {
            e.ToTable("WorkflowExecutionLogRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
            e.Property(x => x.WorkflowDefinitionId).HasMaxLength(450);
            e.Property(x => x.WorkflowDefinitionVersionId).HasMaxLength(450);
            e.Property(x => x.ActivityInstanceId).HasMaxLength(450);
            e.Property(x => x.ParentActivityInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityId).HasMaxLength(450);
            e.Property(x => x.ActivityName).HasMaxLength(450);
            e.Property(x => x.ActivityNodeId).HasMaxLength(450);
            e.Property(x => x.ActivityType).HasMaxLength(450);
            e.Property(x => x.EventName).HasMaxLength(450);
            e.Property(x => x.TenantId).HasMaxLength(450);
        });
        modelBuilder.Entity<Bookmark>(e =>
        {
            e.ToTable("Bookmarks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityTypeName).HasMaxLength(450);
            e.Property(x => x.Hash).HasMaxLength(450);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.TenantId).HasMaxLength(450);
        });
        modelBuilder.Entity<BookmarkQueueItem>(e =>
        {
            e.ToTable("BookmarkQueueItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityTypeName).HasMaxLength(450);
            e.Property(x => x.BookmarkId).HasMaxLength(450);
            e.Property(x => x.CorrelationId).HasMaxLength(450);
            e.Property(x => x.StimulusHash).HasMaxLength(450);
            e.Property(x => x.TenantId).HasMaxLength(450);
        });
        modelBuilder.Entity<WorkflowInboxMessage>(e =>
        {
            e.ToTable("WorkflowInboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityInstanceId).HasMaxLength(450);
            e.Property(x => x.ActivityTypeName).HasMaxLength(450);
            e.Property(x => x.CorrelationId).HasMaxLength(450);
            e.Property(x => x.Hash).HasMaxLength(450);
            e.Property(x => x.TenantId).HasMaxLength(450);
        });
    }
}
