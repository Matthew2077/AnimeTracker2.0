using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Tag> Tags => Set<Tag>();

    private static readonly string[] GenreNames =
    [
        "Isekai", "Hentai", "Romance", "Fantasy", "Shounen", "Action", "Comedy",
        "Slice of life", "Sci-Fi", "Horror", "Supernatural", "Sports", "Mystery"
    ];

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Genre>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(GenreNames.Select((name, i) => new Genre { Id = i + 1, Name = name }));
        });

        b.Entity<Series>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Genre).WithMany(g => g.Series)
                .HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Entry>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Label).HasMaxLength(100);
            e.Property(x => x.MediaType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.UnitLabel).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.MediaGroup);

            e.HasOne(x => x.Series).WithMany(s => s.Entries)
                .HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SeriesId, x.SortOrder });

            e.HasMany(x => x.Tags).WithMany(t => t.Entries);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Entry_TotalUnits_Positive", "\"TotalUnits\" > 0");
                t.HasCheckConstraint("CK_Entry_WatchedUnits_Range", "\"WatchedUnits\" >= 0 AND \"WatchedUnits\" <= \"TotalUnits\"");
                t.HasCheckConstraint("CK_Entry_Completed_Full", "\"Status\" <> 'Completed' OR \"WatchedUnits\" = \"TotalUnits\"");
            });
        });

        b.Entity<Tag>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(50).UseCollation("NOCASE");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Color).HasMaxLength(20);
        });
    }
}
