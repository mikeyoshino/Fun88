namespace Fun88.Web.Modules.Users.Controllers;

using Fun88.Web.Modules.Users.Services;
using Fun88.Web.Modules.Users.ViewModels;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Supabase;
using System.Security.Claims;

[Route("account")]
public class AccountController(
    Client supabaseClient,
    IUserSyncService userSync,
    IOptions<AuthCookieOptions> cookieOpts) : Controller
{
    private readonly string _schemeName = cookieOpts.Value.UserSchemeName;

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var session = await supabaseClient.Auth.SignIn(model.Email, model.Password);
            if (session?.User == null)
            {
                ModelState.AddModelError("", "Invalid credentials.");
                return View(model);
            }

            var dbUser = await userSync.SyncAsync(session.User);

            await SignInUserAsync(session.User.Id!, model.Email, model.RememberMe);

            var returnUrl = model.ReturnUrl;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return Redirect("/");
        }
        catch
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }
    }

    [HttpGet("register")]
    public IActionResult Register()
        => View(new RegisterViewModel());

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var session = await supabaseClient.Auth.SignUp(model.Email, model.Password);
            if (session?.User == null)
            {
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(model);
            }

            await userSync.SyncAsync(session.User);
            await SignInUserAsync(session.User.Id!, model.Email, rememberMe: false);

            return Redirect("/");
        }
        catch
        {
            ModelState.AddModelError("", "Registration failed. The email may already be in use.");
            return View(model);
        }
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(_schemeName);
        return Redirect("/");
    }

    [HttpGet("profile")]
    [Authorize(Policy = PolicyNames.UserOnly)]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        User? dbUser = null;
        if (userId != null)
        {
            dbUser = await supabaseClient.From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                .Single();
        }

        var vm = new ProfileViewModel
        {
            Email = email,
            DisplayName = dbUser?.DisplayName,
            PreferredLanguage = dbUser?.PreferredLanguage ?? "en"
        };

        return View(vm);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.UserOnly)]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            var dbUser = await supabaseClient.From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                .Single();

            if (dbUser != null)
            {
                dbUser.DisplayName = model.DisplayName;
                dbUser.PreferredLanguage = model.PreferredLanguage;
                await supabaseClient.From<User>()
                    .Match(new Dictionary<string, string> { ["id"] = userId })
                    .Update(dbUser);
            }
        }

        return RedirectToAction(nameof(Profile));
    }

    private async Task SignInUserAsync(string userId, string email, bool rememberMe)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, _schemeName);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(_schemeName, principal, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}
