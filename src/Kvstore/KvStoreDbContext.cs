using Microsoft.EntityFrameworkCore;

namespace Kvstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class KvStoreDbContext(DbContextOptions<KvStoreDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public const int MaxKeyBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Documentation.
    /// </summary>
    public DbSet<KvEntryRecord> Entries => Set<KvEntryRecord>();

    /// <summary>
    /// Documentation.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<KvEntryRecord>();
        entity.ToTable("kv_entries");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.KeyHash).IsUnique();
        entity.Property(x => x.Key).IsRequired();
        entity.Property(x => x.KeyHash).IsRequired().HasMaxLength(64);
        entity.Property(x => x.VersionId).IsRequired().IsConcurrencyToken().HasMaxLength(64);
        entity.Property(x => x.ValueBytes).IsRequired();
        entity.Property(x => x.Kind).IsRequired();
    }
}
