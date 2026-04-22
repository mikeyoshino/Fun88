namespace Fun88.Web.Modules.Games.Controllers;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Modules.Users.Services;
using Fun88.Web.Shared.Constants;
using Fun88.Web.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

[Route("games")]
public class GamesController(
    IGameQueryService gameQuery,
    GdEmbedUrlBuilder embedBuilder,
    IOptions<AuthCookieOptions> cookieOpts,
    IFavoriteService favoriteService,
    IGameRatingService ratingService,
    ILikeService likeService,
    IPlayHistoryService playHistoryService
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

        var userId = GetUserId();
        bool? isFav = userId.HasValue ? await favoriteService.IsFavoriteAsync(userId.Value, vm.Id, ct) : null;
        int? userRating = userId.HasValue ? await ratingService.GetUserRatingAsync(userId.Value, vm.Id, ct) : null;
        double avgRating = await ratingService.GetAverageAsync(vm.Id, ct);
        vm = vm with { IsFavorite = isFav, UserRating = userRating, AverageRating = avgRating };

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
    public async Task<IActionResult> RecordPlay(string slug, CancellationToken ct = default)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var vm = await gameQuery.GetDetailViewModelAsync(slug, lang, "", ct);
        if (vm is null) return NotFound();
        var userId = GetUserId();
        var sessionId = Request.Cookies["session_id"] ?? Guid.NewGuid().ToString();
        if (!Request.Cookies.ContainsKey("session_id"))
            Response.Cookies.Append("session_id", sessionId, new CookieOptions { HttpOnly = true, MaxAge = TimeSpan.FromDays(365) });
        await playHistoryService.RecordAsync(userId, vm.Id, sessionId, ct);
        return Ok();
    }

    [HttpPost("{slug}/favorite")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.UserOnly)]
    public async Task<IActionResult> Favorite(string slug, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var vm = await gameQuery.GetDetailViewModelAsync(slug, "en", "", ct);
        if (vm is null) return NotFound();
        var isFav = await favoriteService.IsFavoriteAsync(userId.Value, vm.Id, ct);
        if (isFav) await favoriteService.RemoveAsync(userId.Value, vm.Id, ct);
        else await favoriteService.AddAsync(userId.Value, vm.Id, ct);
        return Json(new { favorited = !isFav });
    }

    [HttpPost("{slug}/rate")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.UserOnly)]
    public async Task<IActionResult> Rate(string slug, [FromForm] int rating, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var vm = await gameQuery.GetDetailViewModelAsync(slug, "en", "", ct);
        if (vm is null) return NotFound();
        await ratingService.UpsertAsync(userId.Value, vm.Id, rating, ct);
        var avg = await ratingService.GetAverageAsync(vm.Id, ct);
        return Json(new { averageRating = avg });
    }

    [HttpPost("{slug}/like")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.UserOnly)]
    public async Task<IActionResult> Like(string slug, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();
        var vm = await gameQuery.GetDetailViewModelAsync(slug, "en", "", ct);
        if (vm is null) return NotFound();
        var (newCount, liked) = await likeService.ToggleAsync(userId.Value, vm.Id, ct);
        return Json(new { likeCount = newCount, liked });
    }

    private Guid? GetUserId() =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

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
