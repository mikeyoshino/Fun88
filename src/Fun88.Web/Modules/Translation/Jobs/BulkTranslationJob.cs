namespace Fun88.Web.Modules.Translation.Jobs;

using System.Collections.Concurrent;
using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using Supabase;
using TranslationJobEntity = Fun88.Web.Infrastructure.Data.Entities.TranslationJob;

[DisallowConcurrentExecution]
public class BulkTranslationJob(
    Client supabaseClient,
    ITranslationService translationService,
    ILogger<BulkTranslationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var raw = context.MergedJobDataMap.GetString("game_ids") ?? "";
        var gameIds = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
        if (gameIds.Count == 0) return;

        var targetLanguage = context.MergedJobDataMap.ContainsKey("language_code")
            ? context.MergedJobDataMap.GetString("language_code") ?? "th"
            : "th";

        var completedIds = new ConcurrentDictionary<Guid, bool>();

        try
        {
            var enRows = await supabaseClient.From<GameTranslation>()
                .Filter("game_id", Postgrest.Constants.Operator.In, gameIds.Select(g => g.ToString()).ToList())
                .Filter("language_code", Postgrest.Constants.Operator.Equals, "en")
                .Get(context.CancellationToken);

            var requests = enRows.Models.Select(t => new TranslationRequest(
                t.GameId.ToString(),
                new Dictionary<string, string>
                {
                    // meta_title and meta_description are excluded — no EN source values are set during import
                    ["title"] = t.Title ?? "",
                    ["description"] = t.Description ?? "",
                    ["control_description"] = t.ControlDescription ?? ""
                })).ToList();

            var results = await translationService.TranslateBatchAsync(
                requests, TranslationContext.Game, targetLanguage, context.CancellationToken);

            if (results.Count == 0)
                throw new InvalidOperationException("Batch translation returned empty results.");

            var enByGameId = enRows.Models.ToDictionary(t => t.GameId.ToString());

            var tasks = results.Select(async r =>
            {
                var source = enByGameId.GetValueOrDefault(r.Id);
                var gameId = Guid.Parse(r.Id);
                var thRow = new GameTranslation
                {
                    GameId = gameId,
                    LanguageCode = targetLanguage,
                    Title = r.TranslatedFields.GetValueOrDefault("title", source?.Title ?? ""),
                    Description = r.TranslatedFields.GetValueOrDefault("description", source?.Description ?? ""),
                    ControlDescription = r.TranslatedFields.GetValueOrDefault("control_description", source?.ControlDescription ?? "")
                };
                await supabaseClient.From<GameTranslation>().Upsert(thRow, cancellationToken: context.CancellationToken);

                var job = new TranslationJobEntity
                {
                    GameId = gameId, LanguageCode = targetLanguage, Status = "completed", CompletedAt = DateTime.UtcNow
                };
                await supabaseClient.From<TranslationJobEntity>()
                    .Match(new Dictionary<string, string> { ["game_id"] = r.Id, ["language_code"] = targetLanguage })
                    .Update(job, cancellationToken: context.CancellationToken);

                completedIds.TryAdd(gameId, true);
            });
            await Task.WhenAll(tasks);

            logger.LogInformation("Bulk translation completed for {Count} games", completedIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk translation failed");

            var failedIds = gameIds.Where(id => !completedIds.ContainsKey(id)).ToList();
            foreach (var id in failedIds)
            {
                var existing = await supabaseClient.From<TranslationJobEntity>()
                    .Match(new Dictionary<string, string> { ["game_id"] = id.ToString(), ["language_code"] = targetLanguage })
                    .Single(CancellationToken.None);

                var job = new TranslationJobEntity
                {
                    GameId = id, LanguageCode = targetLanguage, Status = "failed",
                    LastError = ex.Message,
                    AttemptCount = (short)((existing?.AttemptCount ?? 0) + 1)
                };
                await supabaseClient.From<TranslationJobEntity>()
                    .Match(new Dictionary<string, string> { ["game_id"] = id.ToString(), ["language_code"] = targetLanguage })
                    .Update(job, cancellationToken: CancellationToken.None);
            }
        }
    }
}
