namespace Fun88.Web.Shared.Constants;

public static class LanguageCode
{
    public const string English = "en";
    public const string Thai = "th";

    public static readonly IReadOnlyList<string> All = [English, Thai];

    public static bool IsValid(string code) => All.Contains(code);
}
