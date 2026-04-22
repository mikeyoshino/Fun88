namespace Fun88.Web.Modules.Translation.Jobs;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using Supabase;
using TranslationJobEntity = Fun88.Web.Infrastructure.Data.Entities.TranslationJob;

[DisallowConcurrentExecution]
public class TranslationJobWorker(
    Client supabaseClient,
    ITranslationService translationService,
    ILogger<TranslationJobWorker> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        if (!Guid.TryParse(context.MergedJobDataMap.GetString("game_id"), out var gameId))
            throw new InvalidOperationException("game_id missing or malformed");

        var targetLanguage = context.MergedJobDataMap.ContainsKey("language_code")
            ? context.MergedJobDataMap.GetString("language_code") ?? "th"
            : "th";

        try
        {
            var en = await supabaseClient.From<GameTranslation>()
                .Filter("game_id", Postgrest.Constants.Operator.Equals, gameId.ToString())
                .Filter("language_code", Postgrest.Constants.Operator.Equals, "en")
                .Single(context.CancellationToken);

            if (en is null)
                throw new InvalidOperationException($"No EN translation found for game {gameId}.");

            // meta_title and meta_description are excluded — no EN source values are set during import
            var fields = new Dictionary<string, string>
            {
                ["title"] = en.Title ?? "",
                ["description"] = en.Description ?? "",
                ["control_description"] = en.ControlDescription ?? ""
            };

            var translated = await translationService.TranslateAsync(
                fields, TranslationContext.Game, targetLanguage, context.CancellationToken);

            if (translated.Count == 0)
                throw new InvalidOperationException("Translation returned empty results.");

            var thRow = new GameTranslation
            {
                GameId = gameId,
                LanguageCode = targetLanguage,
                Title = translated.GetValueOrDefault("title", fields["title"]),
                Description = translated.GetValueOrDefault("description", fields["description"]),
                ControlDescription = translated.GetValueOrDefault("control_description", fields["control_description"])
            };
            await supabaseClient.From<GameTranslation>().Upsert(thRow, cancellationToken: context.CancellationToken);

            var job = new TranslationJobEntity
            {
                GameId = gameId, LanguageCode = targetLanguage, Status = "completed", CompletedAt = DateTime.UtcNow
            };
            await supabaseClient.From<TranslationJobEntity>()
                .Match(new Dictionary<string, string> { ["game_id"] = gameId.ToString(), ["language_code"] = targetLanguage })
                .Update(job, cancellationToken: context.CancellationToken);

            logger.LogInformation("Translation completed for game {GameId}", gameId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Translation failed for game {GameId}", gameId);

            var existing = await supabaseClient.From<TranslationJobEntity>()
                .Match(new Dictionary<string, string> { ["game_id"] = gameId.ToString(), ["language_code"] = targetLanguage })
                .Single(CancellationToken.None);

            var job = new TranslationJobEntity
            {
                GameId = gameId, LanguageCode = targetLanguage, Status = "failed",
                LastError = ex.Message,
                AttemptCount = (short)((existing?.AttemptCount ?? 0) + 1)
            };
            await supabaseClient.From<TranslationJobEntity>()
                .Match(new Dictionary<string, string> { ["game_id"] = gameId.ToString(), ["language_code"] = targetLanguage })
                .Update(job, cancellationToken: CancellationToken.None);
        }
    }
}
