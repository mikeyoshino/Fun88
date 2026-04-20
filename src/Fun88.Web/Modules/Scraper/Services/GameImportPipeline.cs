namespace Fun88.Web.Modules.Scraper.Services;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Shared.Constants;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GameImportPipeline(
    IGameRepository gameRepo,
    ICategoryRepository categoryRepo
) : IGameImportPipeline
{
    public async Task<ImportGameResult> ImportAsync(RawGameData raw, int providerId, CancellationToken ct = default)
    {
        try
        {
            // Skip if already imported
            var existing = await gameRepo.GetByProviderGameIdAsync(providerId, raw.ProviderGameId, ct);
            if (existing is not null)
                return new ImportGameResult(Imported: false, Skipped: true, Error: null);

            var slug = Slugify(raw.Title);
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Slug = slug,
                ProviderId = providerId,
                ProviderGameId = raw.ProviderGameId,
                GameUrl = raw.GameUrl,
                ThumbnailUrl = raw.ThumbnailUrl,
                PlayCount = 0,
                LikeCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Translations =
                [
                    new GameTranslation
                    {
                        LanguageCode = LanguageCode.English,
                        Title = raw.Title,
                        Description = raw.Description,
                        ControlDescription = raw.ControlDescription
                    }
                ]
            };

            await gameRepo.AddAsync(game, ct);
            return new ImportGameResult(Imported: true, Skipped: false, Error: null);
        }
        catch (Exception ex)
        {
            return new ImportGameResult(Imported: false, Skipped: false, Error: ex.Message);
        }
    }

    public async Task<ImportGameResult> ImportCustomAsync(CustomGameData custom, CancellationToken ct = default)
    {
        try
        {
            // Custom games may not collide on slug
            var existing = await gameRepo.GetBySlugAsync(custom.Slug, ct);
            if (existing is not null)
                return new ImportGameResult(Imported: false, Skipped: false, Error: $"Slug '{custom.Slug}' already exists.");

            var game = new Game
            {
                Id = Guid.NewGuid(),
                Slug = custom.Slug,
                ProviderId = null,
                ProviderGameId = null,
                GameUrl = custom.GameUrl,
                ThumbnailUrl = custom.ThumbnailUrl,
                PlayCount = 0,
                LikeCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Translations =
                [
                    new GameTranslation
                    {
                        LanguageCode = LanguageCode.English,
                        Title = custom.Title,
                        Description = custom.Description,
                        ControlDescription = custom.ControlDescription
                    }
                ]
            };

            await gameRepo.AddAsync(game, ct);
            return new ImportGameResult(Imported: true, Skipped: false, Error: null);
        }
        catch (Exception ex)
        {
            return new ImportGameResult(Imported: false, Skipped: false, Error: ex.Message);
        }
    }

    private static string Slugify(string title)
        => System.Text.RegularExpressions.Regex
            .Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');
}
