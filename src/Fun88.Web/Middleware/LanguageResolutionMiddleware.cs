namespace Fun88.Web.Middleware;

using Fun88.Web.Shared.Constants;
using Fun88.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

public class LanguageResolutionMiddleware(RequestDelegate next, IOptions<AuthCookieOptions> cookieOpts)
{
    private readonly string _langCookieName = cookieOpts.Value.LanguageCookieName;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var lang = ResolveLang(ctx);
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

    private string ResolveLang(HttpContext ctx)
    {
        if (ctx.Request.Cookies.TryGetValue(_langCookieName, out var cookie) && LanguageCode.IsValid(cookie))
            return cookie;

        var acceptLang = ctx.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(acceptLang))
        {
            var first = acceptLang.Split(',')[0].Trim().Split(';')[0].Trim().ToLowerInvariant();
            return first.StartsWith("th") ? LanguageCode.Thai : LanguageCode.English;
        }

        return LanguageCode.English;
    }
}
