namespace Fun88.Web.Modules.Scraper.Services;

using Fun88.Web.Modules.Scraper.Providers;

public record CustomGameData(
    string Slug,
    string Title,
    string? Description,
    string? ControlDescription,
    string ThumbnailUrl,
    string GameUrl,
    IReadOnlyList<string> CategorySlugs
);

public interface IGameImportPipeline
{
    /// <summary>Import a single game from a provider. Upserts if already exists.</summary>
    Task<ImportGameResult> ImportAsync(RawGameData raw, int providerId, CancellationToken ct = default);

    /// <summary>Import a custom (own) game uploaded by admin. Errors on slug collision.</summary>
    Task<ImportGameResult> ImportCustomAsync(CustomGameData custom, CancellationToken ct = default);
}
