using Microsoft.EntityFrameworkCore;

namespace Tajan.Workflows.Extensions.Data.MainEntities;

/// <summary>
/// Read-only-ish mapping over Elsa's own tables in the "Elsa" schema.
/// Used by the archive/history services. Re-sync with Elsa's schema on upgrade.
/// </summary>
public class ElsaMainDataDbContext(DbContextOptions<ElsaMainDataDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<ActivityExecutionRecord> ActivityExecutionRecords => Set<ActivityExecutionRecord>();
    public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords => Set<WorkflowExecutionLogRecord>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<BookmarkQueueItem> BookmarkQueueItems => Set<BookmarkQueueItem>();
    public DbSet<WorkflowInboxMessage> WorkflowInboxMessages => Set<WorkflowInboxMessage>();
    public DbSet<Trigger> Triggers => Set<Trigger>();
    public DbSet<KeyValuePair> KeyValuePairs => Set<KeyValuePair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowInstance>(e =>
        {
            e.ToTable("WorkflowInstances", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
        });
        modelBuilder.Entity<WorkflowDefinition>(e =>
        {
            e.ToTable("WorkflowDefinitions", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
        });
        modelBuilder.Entity<ActivityExecutionRecord>(e =>
        {
            e.ToTable("ActivityExecutionRecords", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
        });
        modelBuilder.Entity<WorkflowExecutionLogRecord>(e =>
        {
            e.ToTable("WorkflowExecutionLogRecords", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
        });
        modelBuilder.Entity<Bookmark>(e =>
        {
            e.ToTable("Bookmarks", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
        });
        modelBuilder.Entity<BookmarkQueueItem>(e =>
        {
            e.ToTable("BookmarkQueueItems", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
        });
        modelBuilder.Entity<WorkflowInboxMessage>(e =>
        {
            e.ToTable("WorkflowInboxMessages", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.WorkflowInstanceId).HasMaxLength(450);
        });
        modelBuilder.Entity<Trigger>(e =>
        {
            e.ToTable("Triggers", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
        });
        modelBuilder.Entity<KeyValuePair>(e =>
        {
            e.ToTable("KeyValuePairs", "Elsa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(450);
            e.Property(x => x.Key).HasMaxLength(450);
        });
    }
}
