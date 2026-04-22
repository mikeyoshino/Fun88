namespace Fun88.Web.Infrastructure.BackgroundServices;

using Fun88.Web.Infrastructure.Constants;
using Fun88.Web.Infrastructure.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Supabase;

public class QuartzStartupService(
    Client supabaseClient,
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var result = await supabaseClient.From<ScraperSchedule>()
            .Filter("is_enabled", Postgrest.Constants.Operator.Equals, "true")
            .Get(ct);

        var scheduler = await schedulerFactory.GetScheduler(ct);

        foreach (var schedule in result.Models)
        {
        var key = JobKeys.Scraper;
            var triggers = await scheduler.GetTriggersOfJob(key, ct);
            if (triggers.Count > 0)
            {
                logger.LogInformation("Scraper cron trigger already registered, skipping");
                continue;
            }

            if (string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                logger.LogWarning("ScraperSchedule for provider {ProviderId} has no cron expression, skipping", schedule.ProviderId);
                continue;
            }

            var trigger = TriggerBuilder.Create()
                .ForJob(key)
                .WithCronSchedule(schedule.CronExpression)
                .Build();
            await scheduler.ScheduleJob(trigger, ct);
            logger.LogInformation("Registered scraper cron trigger: {CronExpression}", schedule.CronExpression);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
