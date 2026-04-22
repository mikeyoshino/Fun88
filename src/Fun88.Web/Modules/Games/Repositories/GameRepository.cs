namespace Fun88.Web.Modules.Games.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;
using Supabase;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GameRepository(Client supabaseClient) : IGameRepository
{
    // Full embed (categories + translations) — single-row detail only, no ORDER/LIMIT
    private const string SelectFull = "*, game_translations(*), game_categories(*, categories(*, category_translations(*)))";

    // Fetch translations for a list of games and attach them in memory.
    // Avoids PostgREST "aggregate in FROM clause" error that occurs when embedding
    // one-to-many relations on queries that also use ORDER + LIMIT.
    private async Task AttachTranslationsAsync(IReadOnlyList<Game> games, CancellationToken ct)
    {
        if (games.Count == 0) return;
        var ids = games.Select(g => g.Id.ToString()).ToList();
        var resp = await supabaseClient.From<GameTranslation>()
            .Filter("game_id", Postgrest.Constants.Operator.In, ids)
            .Get(ct);
        var byGame = resp.Models.GroupBy(t => t.GameId).ToDictionary(g => g.Key, g => (List<GameTranslation>)g.ToList());
        foreach (var game in games)
            game.Translations = byGame.TryGetValue(game.Id, out var ts) ? ts : new List<GameTranslation>();
    }

    public async Task<Game?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await supabaseClient.From<Game>()
            .Select(SelectFull)
            .Filter("slug", Postgrest.Constants.Operator.Equals, slug)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Single(ct);
    }

    public async Task<Game?> GetByProviderGameIdAsync(int providerId, string providerGameId, CancellationToken ct = default)
    {
        return await supabaseClient.From<Game>()
            .Select("*")
            .Filter("provider_id", Postgrest.Constants.Operator.Equals, providerId)
            .Filter("provider_game_id", Postgrest.Constants.Operator.Equals, providerGameId)
            .Single(ct);
    }

    public async Task<IReadOnlyList<Game>> GetNewestAsync(int count, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Game>()
            .Select("*")
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get(ct);
        var games = response.Models;
        await AttachTranslationsAsync(games, ct);
        return games;
    }

    public async Task<IReadOnlyList<Game>> GetMostPopularAsync(int count, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Game>()
            .Select("*")
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order("play_count", Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get(ct);
        var games = response.Models;
        await AttachTranslationsAsync(games, ct);
        return games;
    }

    public async Task<IReadOnlyList<Game>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize, CancellationToken ct = default)
    {
        var catResponse = await supabaseClient.From<Category>()
            .Filter("slug", Postgrest.Constants.Operator.Equals, categorySlug)
            .Single(ct);
        if (catResponse == null) return new List<Game>();

        var rangeStart = (page - 1) * pageSize;
        var rangeEnd = rangeStart + pageSize - 1;

        var gcResponse = await supabaseClient.From<GameCategory>()
            .Filter("category_id", Postgrest.Constants.Operator.Equals, catResponse.Id)
            .Range(rangeStart, rangeEnd)
            .Get(ct);

        var gameIds = gcResponse.Models.Select(x => x.GameId.ToString()).ToList();
        if (gameIds.Count == 0) return new List<Game>();

        var gamesResponse = await supabaseClient.From<Game>()
            .Select("*")
            .Filter("id", Postgrest.Constants.Operator.In, gameIds)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Get(ct);
        var games = gamesResponse.Models;
        await AttachTranslationsAsync(games, ct);
        return games;
    }

    public async Task<IReadOnlyList<Game>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var rangeStart = (page - 1) * pageSize;
        var rangeEnd = rangeStart + pageSize - 1;

        var transResponse = await supabaseClient.From<GameTranslation>()
            .Filter("language_code", Postgrest.Constants.Operator.Equals, languageCode)
            .Filter("title", Postgrest.Constants.Operator.ILike, $"%{query}%")
            .Range(rangeStart, rangeEnd)
            .Get(ct);

        var gameIds = transResponse.Models.Select(t => t.GameId.ToString()).ToList();
        if (gameIds.Count == 0) return new List<Game>();

        var gamesResponse = await supabaseClient.From<Game>()
            .Select("*")
            .Filter("id", Postgrest.Constants.Operator.In, gameIds)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Get(ct);
        var games = gamesResponse.Models;
        await AttachTranslationsAsync(games, ct);
        return games;
    }

    public async Task<IReadOnlyList<Game>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var rangeStart = (page - 1) * pageSize;
        var rangeEnd = rangeStart + pageSize - 1;

        var response = await supabaseClient.From<Game>()
            .Select("*")
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Range(rangeStart, rangeEnd)
            .Get(ct);
        var games = response.Models;
        await AttachTranslationsAsync(games, ct);
        return games;
    }

    public async Task<int> CountByCategorySlugAsync(string categorySlug, CancellationToken ct = default)
    {
        var catResponse = await supabaseClient.From<Category>()
            .Filter("slug", Postgrest.Constants.Operator.Equals, categorySlug)
            .Single(ct);
        if (catResponse == null) return 0;

        return await supabaseClient.From<GameCategory>()
            .Filter("category_id", Postgrest.Constants.Operator.Equals, catResponse.Id)
            .Count(Postgrest.Constants.CountType.Exact);
    }

    public async Task<int> CountAllAsync(CancellationToken ct = default)
    {
        return await supabaseClient.From<Game>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Count(Postgrest.Constants.CountType.Exact);
    }

    public async Task AddAsync(Game game, CancellationToken ct = default)
    {
        await supabaseClient.From<Game>().Insert(game, new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation }, ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var game = await supabaseClient.From<Game>()
            .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
            .Single(ct);
        if (game is null) return;
        game.IsActive = false;
        await supabaseClient.From<Game>().Update(game, cancellationToken: ct);
    }
}
