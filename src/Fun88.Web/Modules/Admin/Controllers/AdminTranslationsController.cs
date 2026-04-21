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
public class AdminTranslationsController(Client supabaseClient, IScheduler scheduler) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var jobs = await supabaseClient.From<TranslationJobEntity>()
            .Filter("status", Postgrest.Constants.Operator.In, new[] { "pending", "failed" })
            .Get(ct);

        var items = new List<AdminTranslationJobViewModel>();
        foreach (var j in jobs.Models)
        {
            var trans = await supabaseClient.From<GameTranslation>()
                .Filter("game_id", Postgrest.Constants.Operator.Equals, j.GameId.ToString())
                .Filter("language_code", Postgrest.Constants.Operator.Equals, "en")
                .Single(ct);
            var title = trans?.Title ?? j.GameId.ToString();
            items.Add(new AdminTranslationJobViewModel(j.GameId, title, j.Status, j.CreatedAt, j.CompletedAt, j.LastError));
        }

        ViewData["Title"] = "Translations";
        return View(items);
    }

    [HttpPost("retry/{gameId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(Guid gameId, CancellationToken ct)
    {
        await scheduler.TriggerJob(new JobKey($"translation-{gameId}"));
        return RedirectToAction("Index");
    }

    [HttpPost("retry-failed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryFailed(CancellationToken ct)
    {
        var failed = await supabaseClient.From<TranslationJobEntity>()
            .Filter("status", Postgrest.Constants.Operator.Equals, "failed")
            .Get(ct);
        var ids = failed.Models.Select(x => x.GameId.ToString()).ToList();

        if (ids.Count > 0)
        {
            var detail = JobBuilder.Create<BulkTranslationJob>()
                .WithIdentity("bulk-translation-admin")
                .UsingJobData("game_ids", string.Join(",", ids))
                .Build();
            var trigger = TriggerBuilder.Create().StartNow().Build();
            await scheduler.ScheduleJob(detail, trigger, ct);
        }

        return RedirectToAction("Index");
    }
}
