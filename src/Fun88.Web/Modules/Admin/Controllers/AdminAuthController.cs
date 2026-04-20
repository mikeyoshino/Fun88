namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Modules.Admin.Services;
using Fun88.Web.Modules.Admin.ViewModels;
using Fun88.Web.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

[Area("Admin")]
[Route("admin/auth")]
public class AdminAuthController(
    IAdminAuthService adminAuth,
    IOptions<AuthCookieOptions> cookieOpts
) : Controller
{
    private readonly string _schemeName = cookieOpts.Value.AdminSchemeName;

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
        => View(new AdminLoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var token = await adminAuth.SignInAsync(model.Email, model.Password);
        if (token is null)
        {
            model.ErrorMessage = "Invalid email or password.";
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, model.Email),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("supabase_token", token)
        };

        var identity = new ClaimsIdentity(claims, _schemeName);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(_schemeName, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        var returnUrl = model.ReturnUrl;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await adminAuth.SignOutAsync();
        await HttpContext.SignOutAsync(_schemeName);
        return RedirectToAction("Login");
    }
}
