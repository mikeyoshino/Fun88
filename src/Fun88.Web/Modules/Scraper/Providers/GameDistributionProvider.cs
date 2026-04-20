namespace Fun88.Web.Modules.Scraper.Providers;

using Fun88.Web.Infrastructure.Clients;

public class GameDistributionProvider(GameDistributionHttpClient client) : IGameProvider
{
    public string ProviderSlug => "game-distribution";

    public Task<IReadOnlyList<RawGameData>> FetchGamesAsync(GameFetchOptions options, CancellationToken ct = default)
        => client.GetGamesAsync(options.Page, options.PageSize, ct);

    public Task<RawGameData?> FetchGameByIdAsync(string providerGameId, CancellationToken ct = default)
        => client.GetGameByIdAsync(providerGameId, ct);
}
