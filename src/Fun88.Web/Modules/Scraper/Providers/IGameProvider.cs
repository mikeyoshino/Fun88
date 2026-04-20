namespace Fun88.Web.Modules.Scraper.Providers;

public interface IGameProvider
{
    string ProviderSlug { get; }
    Task<IReadOnlyList<RawGameData>> FetchGamesAsync(GameFetchOptions options, CancellationToken ct = default);
    Task<RawGameData?> FetchGameByIdAsync(string providerGameId, CancellationToken ct = default);
}
