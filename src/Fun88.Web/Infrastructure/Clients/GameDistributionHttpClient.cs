namespace Fun88.Web.Infrastructure.Clients;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Scraper.Providers;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

public class GameDistributionHttpClient(HttpClient http, IOptions<GameDistributionOptions> options)
{
    private readonly GameDistributionOptions _opts = options.Value;

    public async Task<IReadOnlyList<RawGameData>> GetGamesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = $"/publisher/{_opts.PublisherId}/games?page={page}&per_page={pageSize}";
        var response = await http.GetFromJsonAsync<GdGameListResponse>(url, ct)
            ?? throw new InvalidOperationException("GD API returned null response");

        return response.Data.Select(MapToRawGameData).ToList();
    }

    public async Task<RawGameData?> GetGameByIdAsync(string providerGameId, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<GdGameResponse>($"/publisher/game/{providerGameId}", ct);
        return response is null ? null : MapToRawGameData(response.Data);
    }

    private static RawGameData MapToRawGameData(GdGameDto dto) => new(
        dto.Md5,
        dto.Title,
        dto.Description ?? string.Empty,
        dto.Instructions ?? string.Empty,
        dto.Thumb ?? string.Empty,
        $"https://html5.gamedistribution.com/{dto.Md5}/",
        dto.Tags?.Select(t => t.Slug).ToList() ?? []
    );

    // GD API response DTOs — internal to this client only
    private record GdGameListResponse(IReadOnlyList<GdGameDto> Data);
    private record GdGameResponse(GdGameDto Data);
    private record GdGameDto(string Md5, string Title, string? Description, string? Instructions, string? Thumb, IReadOnlyList<GdTagDto>? Tags);
    private record GdTagDto(string Slug, string Name);
}
