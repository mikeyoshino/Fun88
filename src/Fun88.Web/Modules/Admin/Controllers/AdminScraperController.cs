namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Infrastructure.Constants;
using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Admin.ViewModels;
using Fun88.Web.Modules.Scraper.Jobs;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Supabase;
using ScraperJobEntity = Fun88.Web.Infrastructure.Data.Entities.ScraperJob;

[Area("Admin")]
[Route("admin/scraper")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminScraperController(Client supabaseClient, ISchedulerFactory schedulerFactory) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var jobsResponse = await supabaseClient.From<ScraperJobEntity>()
            .Order("started_at", Postgrest.Constants.Ordering.Descending)
            .Limit(20)
            .Get(ct);

        var scheduleResponse = await supabaseClient.From<ScraperSchedule>()
            .Filter("provider_id", Postgrest.Constants.Operator.Equals,
                    JobKeys.GameDistributionProviderId.ToString())
            .Single(ct);

        var scheduler = await schedulerFactory.GetScheduler(ct);
        var triggers = await scheduler.GetTriggersOfJob(JobKeys.Scraper, ct);

        var viewModels = jobsResponse.Models.Select(j => new AdminScraperJobViewModel(
            j.Id, j.Status, j.TriggeredBy,
            j.GamesFound, j.GamesImported, j.GamesSkipped,
            j.ErrorMessage, j.StartedAt, j.CompletedAt
        )).ToList();

        ViewBag.Schedule = scheduleResponse;
        ViewBag.NextFireTime = triggers.FirstOrDefault()?.GetNextFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Not scheduled";
        ViewData["Title"] = "Scraper";
        return View(viewModels);
    }

    [HttpPost("run-now")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunNow(CancellationToken ct = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(ct);
        var data = new JobDataMap { ["triggered_by"] = "manual" };
        await scheduler.TriggerJob(JobKeys.Scraper, data, ct);
        TempData["Success"] = "Scraper job queued.";
        return RedirectToAction("Index");
    }

    [HttpPost("update-schedule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSchedule([FromForm] string cronExpression, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cronExpression) || !CronExpression.IsValidExpression(cronExpression))
        {
            TempData["Error"] = "Invalid cron expression.";
            return RedirectToAction("Index");
        }

        await supabaseClient.From<ScraperSchedule>()
            .Match(new Dictionary<string, string> { ["provider_id"] = JobKeys.GameDistributionProviderId.ToString() })
            .Update(new ScraperSchedule { CronExpression = cronExpression, IsEnabled = true }, cancellationToken: ct);

        var scheduler = await schedulerFactory.GetScheduler(ct);
        var jobKey = JobKeys.Scraper;
        var oldTriggers = await scheduler.GetTriggersOfJob(jobKey, ct);
        foreach (var t in oldTriggers)
            await scheduler.UnscheduleJob(t.Key, ct);

        var newTrigger = TriggerBuilder.Create()
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression)
            .Build();
        await scheduler.ScheduleJob(newTrigger, ct);

        TempData["Success"] = "Schedule updated.";
        return RedirectToAction("Index");
    }
}
