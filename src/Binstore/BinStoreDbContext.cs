using Microsoft.EntityFrameworkCore;

namespace Binstore;

/// <summary>
/// Documentation.
/// </summary>
public sealed class BinStoreDbContext(DbContextOptions<BinStoreDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Documentation.
    /// </summary>
    public const int MaxKeyBytes = 4096;

    /// <summary>
    /// Documentation.
    /// </summary>
    public DbSet<BinRecord> Bins => Set<BinRecord>();

    /// <summary>
    /// Documentation.
    /// </summary>
    public DbSet<BinChunkRecord> Chunks => Set<BinChunkRecord>();

    /// <summary>
    /// Documentation.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var bins = modelBuilder.Entity<BinRecord>();
        bins.ToTable("bin_entries");
        bins.HasKey(x => x.Id);
        bins.HasIndex(x => x.KeyHash).IsUnique();
        bins.Property(x => x.Key).IsRequired();
        bins.Property(x => x.KeyHash).IsRequired().HasMaxLength(64);
        bins.Property(x => x.CreatedUtc).IsRequired();
        bins.Property(x => x.TotalBytes).IsRequired();
        bins.Property(x => x.ChunkCount).IsRequired();

        var chunks = modelBuilder.Entity<BinChunkRecord>();
        chunks.ToTable("bin_chunks");
        chunks.HasKey(x => x.Id);
        chunks.HasIndex(x => new { x.BinId, x.ChunkIndex }).IsUnique();
        chunks.Property(x => x.BinId).IsRequired();
        chunks.Property(x => x.ChunkIndex).IsRequired();
        chunks.Property(x => x.Data).IsRequired();
        chunks.HasOne<BinRecord>()
            .WithMany()
            .HasForeignKey(x => x.BinId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
