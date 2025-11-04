using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class UrlShortenerContext : DbContext
{
    public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options)
        : base(options) { }

    public DbSet<URLShorted> ShortenedUrls { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<URLShorted>(resourse =>
        {
            resourse.ToTable("URLShorted");
            resourse.HasKey(u => u.Id);
            resourse.Property(u => u.Url).IsRequired().HasMaxLength(300);
            resourse.Property(u => u.ShortCode).IsRequired(false).HasMaxLength(50);
            resourse.Property(u => u.CreatedAt).IsRequired();
            resourse.Property(u => u.UpdatedAt).IsRequired();
            resourse.Property(u => u.Clicks).IsRequired(false);
        });
    }
}