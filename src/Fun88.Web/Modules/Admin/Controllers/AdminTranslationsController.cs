namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Admin.ViewModels;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Supabase;
using TranslationJobEntity = Fun88.Web.Infrastructure.Data.Entities.TranslationJob;

[Area("Admin")]
[Route("admin/translations")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminTranslationsController(Client supabaseClient, ISchedulerFactory schedulerFactory) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var jobs = await supabaseClient.From<TranslationJobEntity>()
            .Filter("status", Postgrest.Constants.Operator.Equals, "failed")
            .Get(ct);

        var gameIdStrings = jobs.Models.Select(j => j.GameId.ToString()).ToList();

        Dictionary<Guid, string> titleMap = new();
        if (gameIdStrings.Count > 0)
        {
            var translations = await supabaseClient.From<GameTranslation>()
                .Filter("game_id", Postgrest.Constants.Operator.In, gameIdStrings)
                .Filter("language_code", Postgrest.Constants.Operator.Equals, "en")
                .Get(ct);
            titleMap = translations.Models.ToDictionary(t => t.GameId, t => t.Title ?? "");
        }

        var items = jobs.Models.Select(j => new AdminTranslationJobViewModel(
            j.GameId,
            j.LanguageCode,
            titleMap.GetValueOrDefault(j.GameId, j.GameId.ToString()),
            j.Status,
            j.AttemptCount,
            j.CreatedAt,
            j.CompletedAt,
            j.LastError
        )).ToList();


        ViewData["Title"] = "Translations";
        return View(items);
    }

    [HttpPost("retry/{gameId:guid}/{languageCode}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(Guid gameId, string languageCode, CancellationToken ct)
    {
        var scheduler = await schedulerFactory.GetScheduler(ct);
        var detail = JobBuilder.Create<TranslationJobWorker>()
            .WithIdentity($"retry-{gameId}-{languageCode}-{DateTime.UtcNow.Ticks}")
            .UsingJobData("game_id", gameId.ToString())
            .UsingJobData("language_code", languageCode)
            .Build();
        var trigger = TriggerBuilder.Create().StartNow().Build();
        await scheduler.ScheduleJob(detail, trigger, ct);
        TempData["Success"] = "Retry queued.";
        return RedirectToAction("Index");
    }

    [HttpPost("retry-failed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryFailed(CancellationToken ct)
    {
        var failed = await supabaseClient.From<TranslationJobEntity>()
            .Filter("status", Postgrest.Constants.Operator.Equals, "failed")
            .Get(ct);

        if (failed.Models.Count > 0)
        {
            var scheduler = await schedulerFactory.GetScheduler(ct);
            var ticks = DateTime.UtcNow.Ticks;
            foreach (var job in failed.Models)
            {
                var detail = JobBuilder.Create<TranslationJobWorker>()
                    .WithIdentity($"retry-{job.GameId}-{job.LanguageCode}-{ticks++}")
                    .UsingJobData("game_id", job.GameId.ToString())
                    .UsingJobData("language_code", job.LanguageCode)
                    .Build();
                await scheduler.ScheduleJob(detail, TriggerBuilder.Create().StartNow().Build(), ct);
            }
            TempData["Success"] = $"Retry queued for {failed.Models.Count} failed jobs.";
        }

        return RedirectToAction("Index");
    }
}
