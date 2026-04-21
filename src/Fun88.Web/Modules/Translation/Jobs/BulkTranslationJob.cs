namespace Fun88.Web.Modules.Translation.Jobs;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Quartz;
using Supabase;
using TranslationJobEntity = Fun88.Web.Infrastructure.Data.Entities.TranslationJob;

[DisallowConcurrentExecution]
public class BulkTranslationJob(Client supabaseClient, ITranslationService translationService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var raw = context.MergedJobDataMap.GetString("game_ids") ?? "";
        var gameIds = raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();
        if (gameIds.Count == 0) return;

        try
        {
            var gameIdStrings = gameIds.Select(id => id.ToString()).ToList();
            var enRows = await supabaseClient.From<GameTranslation>()
                .Filter("game_id", Postgrest.Constants.Operator.In, gameIdStrings)
                .Filter("language_code", Postgrest.Constants.Operator.Equals, "en")
                .Get(context.CancellationToken);

            var requests = enRows.Models.Select(t => new TranslationRequest(
                t.GameId.ToString(),
                new Dictionary<string, string>
                {
                    ["title"] = t.Title,
                    ["description"] = t.Description ?? "",
                    ["control_description"] = t.ControlDescription ?? ""
                })).ToList();

            var results = await translationService.TranslateBatchAsync(requests, TranslationContext.Game, "th", context.CancellationToken);
            if (results.Count == 0)
                throw new InvalidOperationException("Translation returned empty results.");

            foreach (var r in results)
            {
                var thRow = new GameTranslation
                {
                    GameId = Guid.Parse(r.Id),
                    LanguageCode = "th",
                    Title = r.TranslatedFields.GetValueOrDefault("title", ""),
                    Description = r.TranslatedFields.GetValueOrDefault("description", ""),
                    ControlDescription = r.TranslatedFields.GetValueOrDefault("control_description", "")
                };
                await supabaseClient.From<GameTranslation>().Upsert(thRow, cancellationToken: context.CancellationToken);

                var job = new TranslationJobEntity { GameId = Guid.Parse(r.Id), LanguageCode = "th", Status = "completed", CompletedAt = DateTime.UtcNow };
                await supabaseClient.From<TranslationJobEntity>().Update(job, cancellationToken: context.CancellationToken);
            }
        }
        catch (Exception ex)
        {
            foreach (var id in gameIds)
            {
                var job = new TranslationJobEntity { GameId = id, LanguageCode = "th", Status = "failed", LastError = ex.Message, AttemptCount = 1 };
                await supabaseClient.From<TranslationJobEntity>().Update(job, cancellationToken: context.CancellationToken);
            }
        }
    }
}
