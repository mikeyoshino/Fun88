namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Route("admin")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminDashboardController(
    IGameRepository gameRepo,
    ICategoryRepository categoryRepo
) : Controller
{
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var totalGames = await gameRepo.CountAllAsync(ct);
        var categories = await categoryRepo.GetAllCategoriesAsync(LanguageCode.English, ct);
        ViewBag.TotalGames = totalGames;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.PendingTranslations = 0; // Plan 2
        return View();
    }
}
