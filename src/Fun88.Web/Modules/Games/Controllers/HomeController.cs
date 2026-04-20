namespace Fun88.Web.Modules.Games.Controllers;

using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

public class HomeController(IGameQueryService gameQuery) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var vm = await gameQuery.GetHomeViewModelAsync(lang, ct);
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Microsoft.AspNetCore.Mvc.ProblemDetails());
}
