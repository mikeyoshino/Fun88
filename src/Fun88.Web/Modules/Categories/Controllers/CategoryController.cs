namespace Fun88.Web.Modules.Categories.Controllers;

using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Categories.ViewModels;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Shared.Constants;
using Fun88.Web.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

[Route("games/category")]
public class CategoryController(
    IGameQueryService gameQuery,
    ICategoryRepository categoryRepo
) : Controller
{
    private const int PageSize = 24;

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug, int page = 1, CancellationToken ct = default)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var cat = await categoryRepo.GetBySlugAsync(slug, ct);
        if (cat is null) return NotFound();

        var translation = cat.Translations?.FirstOrDefault(t => t.LanguageCode == lang)
            ?? cat.Translations?.FirstOrDefault(t => t.LanguageCode == LanguageCode.English);
        var categoryName = translation?.Name ?? slug;

        var (games, total) = await gameQuery.GetCategoryGamesAsync(slug, lang, page, PageSize, ct);

        var vm = new CategoryPageViewModel(slug, categoryName, games, total, page, PageSize);
        ViewData["Title"] = categoryName;
        return View(vm);
    }
}
