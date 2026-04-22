namespace Fun88.Web.Modules.Scraper.Jobs;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Infrastructure.Constants;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Modules.Translation.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Supabase;
using ScraperJobEntity = Fun88.Web.Infrastructure.Data.Entities.ScraperJob;
using ScraperScheduleEntity = Fun88.Web.Infrastructure.Data.Entities.ScraperSchedule;

[DisallowConcurrentExecution]
public class ScraperJob(
    Client supabaseClient,
    IGameProvider gameProvider,
    IGameImportPipeline importPipeline,
    ISchedulerFactory schedulerFactory,
    IOptions<OpenAiOptions> openAiOpts,
    ILogger<ScraperJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var triggeredBy = context.MergedJobDataMap.GetString("triggered_by") ?? "schedule";

        // Load schedule to get provider configuration
        var schedule = await supabaseClient.From<ScraperScheduleEntity>()
            .Filter("provider_id", Postgrest.Constants.Operator.Equals,
                    JobKeys.GameDistributionProviderId.ToString())
            .Single(ct);
        var providerId = schedule?.ProviderId ?? 1;

        var jobRowId = Guid.NewGuid();
        var jobRow = new ScraperJobEntity
        {
            Id = jobRowId,
            ProviderId = providerId,
            TriggeredBy = triggeredBy,
            Status = "running",
            StartedAt = DateTime.UtcNow
        };
        await supabaseClient.From<ScraperJobEntity>().Insert(jobRow, cancellationToken: ct);

        try
        {
            var rawGames = await gameProvider.FetchGamesAsync(new GameFetchOptions(), ct);

            int gamesFound = rawGames.Count;
            int gamesImported = 0;
            int gamesSkipped = 0;
            var importedGameIds = new List<Guid>();

            foreach (var rawGame in rawGames)
            {
                var result = await importPipeline.ImportAsync(rawGame, providerId: providerId, ct);
                if (result.Imported)
                {
                    gamesImported++;
                    if (result.GameId.HasValue)
                        importedGameIds.Add(result.GameId.Value);
                }
                else if (result.Skipped)
                {
                    gamesSkipped++;
                }
            }

            var completedRow = new ScraperJobEntity
            {
                Id = jobRowId,
                Status = "completed",
                CompletedAt = DateTime.UtcNow,
                GamesFound = gamesFound,
                GamesImported = gamesImported,
                GamesSkipped = gamesSkipped
            };
            await supabaseClient.From<ScraperJobEntity>()
                .Match(new Dictionary<string, string> { ["id"] = jobRowId.ToString() })
                .Update(completedRow, cancellationToken: ct);

            if (openAiOpts.Value.TranslationEnabled && importedGameIds.Count > 0)
            {
                var scheduler = await schedulerFactory.GetScheduler(ct);
                var bulkJob = JobBuilder.Create<BulkTranslationJob>()
                    .WithIdentity($"bulk-translation-{jobRowId}")
                    .UsingJobData("game_ids", string.Join(",", importedGameIds))
                    .Build();
                var trigger = TriggerBuilder.Create().StartNow().Build();
                await scheduler.ScheduleJob(bulkJob, trigger, ct);
            }

            var scheduleRow = new ScraperScheduleEntity { LastRunAt = DateTime.UtcNow };
            await supabaseClient.From<ScraperScheduleEntity>()
                .Match(new Dictionary<string, string> { ["provider_id"] = JobKeys.GameDistributionProviderId.ToString() })
                .Update(scheduleRow, cancellationToken: ct);

            logger.LogInformation("Scraper job completed: {Found} found, {Imported} imported, {Skipped} skipped",
                gamesFound, gamesImported, gamesSkipped);
        }
        catch (OperationCanceledException)
        {
            await supabaseClient.From<ScraperJobEntity>()
                .Match(new Dictionary<string, string> { ["id"] = jobRowId.ToString() })
                .Update(new ScraperJobEntity { Id = jobRowId, Status = "cancelled" },
                        cancellationToken: CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scraper job failed");

            var failedRow = new ScraperJobEntity
            {
                Id = jobRowId,
                Status = "failed",
                ErrorMessage = ex.Message
            };
            await supabaseClient.From<ScraperJobEntity>()
                .Match(new Dictionary<string, string> { ["id"] = jobRowId.ToString() })
                .Update(failedRow, cancellationToken: CancellationToken.None);

            throw;
        }
    }
}
