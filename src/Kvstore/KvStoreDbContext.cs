using Microsoft.EntityFrameworkCore;

namespace Kvstore;

public sealed class KvStoreDbContext(DbContextOptions<KvStoreDbContext> options) : DbContext(options)
{
    public const int MaxKeyBytes = 10 * 1024 * 1024;

    public DbSet<KvEntryRecord> Entries => Set<KvEntryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<KvEntryRecord>();
        entity.ToTable("kv_entries");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Key).IsUnique();
        entity.Property(x => x.Key).IsRequired();
        entity.Property(x => x.VersionId).IsRequired().IsConcurrencyToken().HasMaxLength(64);
        entity.Property(x => x.ValueBytes).IsRequired();
        entity.Property(x => x.Kind).IsRequired();
    }
}
