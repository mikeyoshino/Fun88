namespace Fun88.Web.Middleware;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Shared.Constants;
using Fun88.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public class LanguageResolutionMiddleware(RequestDelegate next, IOptions<AuthCookieOptions> cookieOpts)
{
    private readonly string _langCookieName = cookieOpts.Value.LanguageCookieName;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var lang = await ResolveLangAsync(ctx);
        ctx.Items[HttpContextKeys.CurrentLanguage] = lang;

        if (!ctx.Request.Cookies.ContainsKey(_langCookieName))
            ctx.Response.Cookies.Append(_langCookieName, lang, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                SameSite = SameSiteMode.Strict,
                HttpOnly = false
            });

        await next(ctx);
    }

    private async Task<string> ResolveLangAsync(HttpContext ctx)
    {
        // Step 1: Check authenticated user's preferred_language
        var user = ctx.User;
        if (user.Identity?.IsAuthenticated == true && user.Identity.AuthenticationType == "UserAuth")
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                using var scope = ctx.RequestServices.CreateScope();
                var supabase = scope.ServiceProvider.GetRequiredService<Supabase.Client>();
                var dbUser = await supabase.From<User>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                    .Single();
                if (dbUser != null && LanguageCode.IsValid(dbUser.PreferredLanguage))
                    return dbUser.PreferredLanguage;
            }
        }

        // Step 2: Language cookie
        if (ctx.Request.Cookies.TryGetValue(_langCookieName, out var cookie) && LanguageCode.IsValid(cookie))
            return cookie;

        // Step 3: Accept-Language header
        var acceptLang = ctx.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(acceptLang))
        {
            var first = acceptLang.Split(',')[0].Trim().Split(';')[0].Trim().ToLowerInvariant();
            return first.StartsWith("th") ? LanguageCode.Thai : LanguageCode.English;
        }

        return LanguageCode.English;
    }
}
