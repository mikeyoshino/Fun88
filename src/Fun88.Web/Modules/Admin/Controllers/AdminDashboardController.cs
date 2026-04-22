namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;

[Area("Admin")]
[Route("admin")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminDashboardController(
    IGameRepository gameRepo,
    ICategoryRepository categoryRepo,
    Client supabaseClient
) : Controller
{
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var totalGames = await gameRepo.CountAllAsync(ct);
        var categories = await categoryRepo.GetAllCategoriesAsync(LanguageCode.English, ct);
        var pendingJobs = await supabaseClient.From<TranslationJob>()
            .Filter("status", Postgrest.Constants.Operator.In, new List<string> { "pending", "failed" })
            .Get(ct);
        ViewBag.TotalGames = totalGames;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.PendingTranslations = pendingJobs.Models.Count;
        return View();
    }
}
