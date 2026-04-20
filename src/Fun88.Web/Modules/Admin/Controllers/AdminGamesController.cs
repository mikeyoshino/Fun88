namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Admin.ViewModels;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Route("admin/games")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminGamesController(
    IGameRepository gameRepo,
    IGameImportPipeline importPipeline,
    IGameProvider gameProvider
) : Controller
{
    private const int PageSize = 20;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var (games, total) = (await gameRepo.GetAllPagedAsync(page, PageSize, ct), await gameRepo.CountAllAsync(ct));
        var items = games.Select(g =>
        {
            var t = g.Translations?.FirstOrDefault(x => x.LanguageCode == LanguageCode.English);
            return new AdminGameListItemViewModel
            {
                Id = g.Id, Slug = g.Slug,
                Title = t?.Title ?? g.Slug,
                ThumbnailUrl = g.ThumbnailUrl,
                IsGdGame = g.ProviderId.HasValue,
                IsActive = g.IsActive,
                PlayCount = g.PlayCount
            };
        }).ToList();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / PageSize);
        ViewData["Title"] = "Games";
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add Custom Game";
        return View("Form", new AdminGameFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminGameFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "Add Custom Game"; return View("Form", model); }

        var custom = new CustomGameData(model.Slug, model.Title, model.Description, model.ControlDescription,
            model.ThumbnailUrl, model.GameUrl, []);
        var result = await importPipeline.ImportCustomAsync(custom, ct);

        if (!result.Imported)
        {
            ModelState.AddModelError("", result.Error ?? "Import failed.");
            ViewData["Title"] = "Add Custom Game";
            return View("Form", model);
        }

        TempData["Success"] = "Game added successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost("trigger-import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerImport(int providerId = 1, CancellationToken ct = default)
    {
        var options = new GameFetchOptions(Page: 1, PageSize: 100);
        var games = await gameProvider.FetchGamesAsync(options, ct);

        int imported = 0, skipped = 0, failed = 0;
        foreach (var g in games)
        {
            var r = await importPipeline.ImportAsync(g, providerId, ct);
            if (r.Imported) imported++;
            else if (r.Skipped) skipped++;
            else failed++;
        }

        return Json(new { imported, skipped, failed });
    }

    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await gameRepo.DeactivateAsync(id, ct);
        TempData["Success"] = "Game deactivated.";
        return RedirectToAction("Index");
    }
}
