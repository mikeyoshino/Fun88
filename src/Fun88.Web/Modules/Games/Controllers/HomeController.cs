namespace Fun88.Web.Modules.Games.Controllers;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Shared.Constants;
using Fun88.Web.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

public class HomeController(
    IGameQueryService gameQuery,
    IOptions<AuthCookieOptions> cookieOpts
) : Controller
{
    private readonly string _langCookieName = cookieOpts.Value.LanguageCookieName;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var vm = await gameQuery.GetHomeViewModelAsync(lang, ct);
        return View(vm);
    }

    [HttpGet("/language/set")]
    public IActionResult SetLanguage(string lang, string? returnUrl)
    {
        if (!LanguageCode.IsValid(lang)) lang = LanguageCode.English;
        Response.Cookies.Append(_langCookieName, lang, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            SameSite = SameSiteMode.Strict,
            HttpOnly = false
        });
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Microsoft.AspNetCore.Mvc.ProblemDetails());
}
