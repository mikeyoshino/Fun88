namespace Fun88.Web.Modules.Translation.Jobs;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Quartz;
using Supabase;
using TranslationJobEntity = Fun88.Web.Infrastructure.Data.Entities.TranslationJob;

[DisallowConcurrentExecution]
public class TranslationJob(Client supabaseClient, ITranslationService translationService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var gameId = Guid.Parse(context.MergedJobDataMap.GetString("game_id") ?? throw new InvalidOperationException("game_id missing"));

        var en = await supabaseClient.From<GameTranslation>()
            .Filter("game_id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Filter("language_code", Postgrest.Constants.Operator.Equals, "en")
            .Single(context.CancellationToken);

        var fields = new Dictionary<string, string>
        {
            ["title"] = en?.Title ?? "",
            ["description"] = en?.Description ?? "",
            ["control_description"] = en?.ControlDescription ?? ""
        };

        try
        {
            var translated = await translationService.TranslateAsync(fields, TranslationContext.Game, "th", context.CancellationToken);
            if (translated.Count == 0)
                throw new InvalidOperationException("Translation returned empty results.");

            var thRow = new GameTranslation
            {
                GameId = gameId,
                LanguageCode = "th",
                Title = translated.GetValueOrDefault("title", fields["title"]),
                Description = translated.GetValueOrDefault("description", fields["description"]),
                ControlDescription = translated.GetValueOrDefault("control_description", fields["control_description"])
            };
            await supabaseClient.From<GameTranslation>().Upsert(thRow, cancellationToken: context.CancellationToken);

            var job = new TranslationJobEntity { GameId = gameId, LanguageCode = "th", Status = "completed", CompletedAt = DateTime.UtcNow };
            await supabaseClient.From<TranslationJobEntity>().Update(job, cancellationToken: context.CancellationToken);
        }
        catch (Exception ex)
        {
            var job = new TranslationJobEntity { GameId = gameId, LanguageCode = "th", Status = "failed", LastError = ex.Message, AttemptCount = 1 };
            await supabaseClient.From<TranslationJobEntity>().Update(job, cancellationToken: context.CancellationToken);
        }
    }
}
