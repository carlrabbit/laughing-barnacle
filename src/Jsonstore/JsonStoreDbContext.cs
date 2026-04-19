using Microsoft.EntityFrameworkCore;

namespace Jsonstore;

public sealed class JsonStoreDbContext(DbContextOptions<JsonStoreDbContext> options) : DbContext(options)
{
    public const int MaxKeyBytes = 4096;

    public DbSet<JsonRecord> Jsons => Set<JsonRecord>();

    public DbSet<JsonChunkRecord> JsonChunks => Set<JsonChunkRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var jsons = modelBuilder.Entity<JsonRecord>();
        jsons.ToTable("json_entries");
        jsons.HasKey(x => x.Id);
        jsons.HasIndex(x => x.KeyHash).IsUnique();
        jsons.Property(x => x.Key).IsRequired();
        jsons.Property(x => x.KeyHash).IsRequired().HasMaxLength(64);
        jsons.Property(x => x.CreatedUtc).IsRequired();
        jsons.Property(x => x.TotalBytes).IsRequired();
        jsons.Property(x => x.ChunkCount).IsRequired();

        var chunks = modelBuilder.Entity<JsonChunkRecord>();
        chunks.ToTable("json_chunks");
        chunks.HasKey(x => x.Id);
        chunks.HasIndex(x => new { x.JsonId, x.ChunkIndex }).IsUnique();
        chunks.Property(x => x.JsonId).IsRequired();
        chunks.Property(x => x.ChunkIndex).IsRequired();
        chunks.Property(x => x.Data).IsRequired();
        chunks.HasOne<JsonRecord>()
            .WithMany()
            .HasForeignKey(x => x.JsonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
