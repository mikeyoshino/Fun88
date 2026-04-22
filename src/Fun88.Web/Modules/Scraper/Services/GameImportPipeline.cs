namespace Fun88.Web.Modules.Scraper.Services;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Shared.Constants;
using Microsoft.Extensions.Options;
using Quartz;
using Supabase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TranslationJobEntity = Fun88.Web.Infrastructure.Data.Entities.TranslationJob;

public class GameImportPipeline(
    IGameRepository gameRepo,
    ICategoryRepository categoryRepo,
    Client supabaseClient,
    ISchedulerFactory schedulerFactory,
    IOptions<OpenAiOptions> openAiOpts
) : IGameImportPipeline
{
    public async Task<ImportGameResult> ImportAsync(RawGameData raw, int providerId, CancellationToken ct = default)
    {
        try
        {
            // Skip if already imported
            var existing = await gameRepo.GetByProviderGameIdAsync(providerId, raw.ProviderGameId, ct);
            if (existing is not null)
                return new ImportGameResult(Imported: false, Skipped: true, Error: null, GameId: existing.Id);

            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                Slug = Slugify(raw.Title),
                ProviderId = providerId,
                ProviderGameId = raw.ProviderGameId,
                GameUrl = raw.GameUrl,
                ThumbnailUrl = raw.ThumbnailUrl,
                PlayCount = 0,
                LikeCount = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
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

            // Game row is persisted first. If the subsequent Supabase translation_jobs
            // insert fails, we have a game with no job — recoverable via admin retry.
            // The reverse ordering would risk a translation_jobs FK violation.
            await gameRepo.AddAsync(game, ct);
            await EnqueueTranslationJobAsync(gameId, ct);

            return new ImportGameResult(Imported: true, Skipped: false, Error: null, GameId: gameId);
        }
        catch (Exception ex)
        {
            return new ImportGameResult(Imported: false, Skipped: false, Error: ex.Message, GameId: null);
        }
    }

    public async Task<ImportGameResult> ImportCustomAsync(CustomGameData custom, CancellationToken ct = default)
    {
        try
        {
            // Custom games may not collide on slug
            var existing = await gameRepo.GetBySlugAsync(custom.Slug, ct);
            if (existing is not null)
                return new ImportGameResult(Imported: false, Skipped: false, Error: $"Slug '{custom.Slug}' already exists.", GameId: null);

            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                Slug = custom.Slug,
                ProviderId = null,
                ProviderGameId = null,
                GameUrl = custom.GameUrl,
                ThumbnailUrl = custom.ThumbnailUrl,
                PlayCount = 0,
                LikeCount = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
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

            // Game row is persisted first. If the subsequent Supabase translation_jobs
            // insert fails, we have a game with no job — recoverable via admin retry.
            // The reverse ordering would risk a translation_jobs FK violation.
            await gameRepo.AddAsync(game, ct);
            await EnqueueTranslationJobAsync(gameId, ct);

            return new ImportGameResult(Imported: true, Skipped: false, Error: null, GameId: gameId);
        }
        catch (Exception ex)
        {
            return new ImportGameResult(Imported: false, Skipped: false, Error: ex.Message, GameId: null);
        }
    }

    private static string Slugify(string title)
        => System.Text.RegularExpressions.Regex
            .Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');

    private async Task EnqueueTranslationJobAsync(Guid gameId, CancellationToken ct)
    {
        var job = new TranslationJobEntity
        {
            GameId = gameId,
            LanguageCode = LanguageCode.Thai,
            Status = "pending",
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        await supabaseClient.From<TranslationJobEntity>().Insert(job, cancellationToken: ct);

        if (!openAiOpts.Value.TranslationEnabled) return;

        var detail = JobBuilder.Create<Fun88.Web.Modules.Translation.Jobs.TranslationJobWorker>()
            .WithIdentity($"translation-{gameId}")
            .UsingJobData("game_id", gameId.ToString())
            .Build();
        var scheduler = await schedulerFactory.GetScheduler(ct);
        await scheduler.ScheduleJob(detail, TriggerBuilder.Create().StartNow().Build(), ct);
    }
}
