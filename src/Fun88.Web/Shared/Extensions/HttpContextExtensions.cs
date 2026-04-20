namespace Fun88.Web.Shared.Extensions;

using Fun88.Web.Shared.Constants;

public static class HttpContextExtensions
{
    public static string GetCurrentLanguage(this HttpContext ctx)
        => ctx.Items.TryGetValue(HttpContextKeys.CurrentLanguage, out var lang) && lang is string s
            ? s
            : LanguageCode.English;
}
