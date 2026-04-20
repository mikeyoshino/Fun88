namespace Fun88.Web.Modules.Scraper.Providers;

public record RawGameData(
    string ProviderGameId,
    string Title,
    string Description,
    string ControlDescription,
    string ThumbnailUrl,
    string GameUrl,
    IReadOnlyList<string> CategorySlugs
);
