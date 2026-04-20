namespace Fun88.Web.Infrastructure.Data;

using Fun88.Web.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameTranslation> GameTranslations => Set<GameTranslation>();
    public DbSet<GameProvider> GameProviders => Set<GameProvider>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<GameCategory> GameCategories => Set<GameCategory>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ScraperJob> ScraperJobs => Set<ScraperJob>();
    public DbSet<TranslationJob> TranslationJobs => Set<TranslationJob>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Game>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.ProviderId, x.ProviderGameId }).IsUnique().HasFilter("provider_game_id IS NOT NULL");
            e.HasOne(x => x.Provider).WithMany(p => p.Games).HasForeignKey(x => x.ProviderId).IsRequired(false);
            e.HasMany(x => x.Translations).WithOne(t => t.Game).HasForeignKey(t => t.GameId);
        });

        builder.Entity<GameTranslation>(e =>
        {
            e.HasKey(x => new { x.GameId, x.LanguageCode });
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasMany(x => x.Translations).WithOne(t => t.Category).HasForeignKey(t => t.CategoryId);
        });

        builder.Entity<CategoryTranslation>(e =>
        {
            e.HasKey(x => new { x.CategoryId, x.LanguageCode });
        });

        builder.Entity<GameCategory>(e =>
        {
            e.HasKey(x => new { x.GameId, x.CategoryId });
            e.HasOne(x => x.Game).WithMany(g => g.GameCategories).HasForeignKey(x => x.GameId);
            e.HasOne(x => x.Category).WithMany(c => c.GameCategories).HasForeignKey(x => x.CategoryId);
        });

        builder.Entity<AdminUser>(e => e.HasKey(x => x.Id));

        builder.Entity<ScraperJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId);
        });

        builder.Entity<TranslationJob>(e =>
        {
            e.HasKey(x => new { x.GameId, x.LanguageCode });
            e.HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId);
        });
    }
}
