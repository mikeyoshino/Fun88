namespace Fun88.Web.Infrastructure.Configuration;

public sealed class AuthCookieOptions
{
    public const string Section = "AuthCookie";

    public string AdminSchemeName { get; init; } = "AdminCookie";
    public string UserSchemeName { get; init; } = "UserAuth";
    public int AdminExpiryHours { get; init; } = 8;
    public string GdprConsentCookieName { get; init; } = "gdpr_consent";
    public string LanguageCookieName { get; init; } = "lang";
}
