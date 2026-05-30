using Microsoft.EntityFrameworkCore;

namespace Binstore;
/// <summary>
/// Represents bin store db context.
/// </summary>

public sealed class BinStoreDbContext(DbContextOptions<BinStoreDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Performs the operation operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public const int MaxKeyBytes = 4096;
    /// <summary>
    /// Performs the operation operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    public DbSet<BinRecord> Bins => Set<BinRecord>();
    /// <summary>
    /// Performs the operation operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    public DbSet<BinChunkRecord> Chunks => Set<BinChunkRecord>();
    /// <summary>
    /// Performs the on model creating operation.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>

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
