namespace Fun88.Web.Modules.Games.Controllers;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Route("games")]
public class GamesController(
    IGameQueryService gameQuery,
    GdEmbedUrlBuilder embedBuilder,
    IOptions<AuthCookieOptions> cookieOpts
) : Controller
{
    private const int DefaultPageSize = 24;
    private readonly string _gdprCookieName = cookieOpts.Value.GdprConsentCookieName;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var (games, total) = await gameQuery.GetAllGamesAsync(lang, page, DefaultPageSize, ct);
        ViewData["Title"] = "All Games";
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / DefaultPageSize);
        return View(games);
    }

    [HttpGet("newest")]
    public async Task<IActionResult> Newest(int page = 1, CancellationToken ct = default)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var (games, total) = await gameQuery.GetAllGamesAsync(lang, page, DefaultPageSize, ct);
        ViewData["Title"] = "New Games";
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / DefaultPageSize);
        return View("Index", games);
    }

    [HttpGet("most-popular")]
    public async Task<IActionResult> MostPopular(int page = 1, CancellationToken ct = default)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var (games, total) = await gameQuery.GetAllGamesAsync(lang, page, DefaultPageSize, ct);
        ViewData["Title"] = "Most Popular";
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / DefaultPageSize);
        return View("Index", games);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var (tracking, targeting, thirdParty) = ReadGdprConsent();
        var referrerUrl = $"{Request.Scheme}://{Request.Host}/games/{slug}";
        var embedUrl = embedBuilder.Build(slug, referrerUrl, tracking, targeting, thirdParty);

        var vm = await gameQuery.GetDetailViewModelAsync(slug, lang, embedUrl, ct);
        if (vm is null) return NotFound();

        ViewData["Title"] = vm.Title;
        return View(vm);
    }

    [HttpGet("/search")]
    public async Task<IActionResult> Search(string? q, int page = 1, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return RedirectToAction("Index");
        var lang = HttpContext.GetCurrentLanguage();
        var games = await gameQuery.SearchAsync(q, lang, page, DefaultPageSize, ct);
        ViewData["Title"] = $"Search: {q}";
        ViewBag.SearchQuery = q;
        ViewBag.Page = page;
        return View("Index", games);
    }

    [HttpPost("{slug}/play")]
    [ValidateAntiForgeryToken]
    public IActionResult RecordPlay(string slug) => Ok();

    private (int tracking, int targeting, int thirdParty) ReadGdprConsent()
    {
        if (!Request.Cookies.TryGetValue(_gdprCookieName, out var raw) || string.IsNullOrEmpty(raw))
            return (0, 0, 0);

        var parts = raw.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1] == "1" ? 1 : 0);

        return (
            parts.GetValueOrDefault("tracking"),
            parts.GetValueOrDefault("targeting"),
            parts.GetValueOrDefault("third_party")
        );
    }
}
