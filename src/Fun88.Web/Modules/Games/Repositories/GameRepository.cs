namespace Fun88.Web.Modules.Games.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;
using Supabase;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GameRepository(Client supabaseClient) : IGameRepository
{
    // Full embed (categories + translations) — detail page only
    private const string SelectFull = "*, game_translations(*), game_categories(*, categories(*, category_translations(*)))";
    // Translations only — list/card queries (avoids PostgREST aggregate-in-FROM error with ORDER+LIMIT)
    private const string SelectForCard = "*, game_translations(*)";

    public async Task<Game?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Game>()
            .Select(SelectFull)
            .Filter("slug", Postgrest.Constants.Operator.Equals, slug)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Single(ct);
        return response;
    }

    public async Task<Game?> GetByProviderGameIdAsync(int providerId, string providerGameId, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Game>()
            .Select(SelectForCard)
            .Filter("provider_id", Postgrest.Constants.Operator.Equals, providerId)
            .Filter("provider_game_id", Postgrest.Constants.Operator.Equals, providerGameId)
            .Single(ct);
        return response;
    }

    public async Task<IReadOnlyList<Game>> GetNewestAsync(int count, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Game>()
            .Select(SelectForCard)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get(ct);
        return response.Models.ToList();
    }

    public async Task<IReadOnlyList<Game>> GetMostPopularAsync(int count, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Game>()
            .Select(SelectForCard)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order("play_count", Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get(ct);
        return response.Models.ToList();
    }

    public async Task<IReadOnlyList<Game>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize, CancellationToken ct = default)
    {
        // For PostgREST, filtering by deep nested rels is tricky unless using custom RPC or doing two queries.
        // We will fetch GameCategories first, then map them, or rely on fetching the category's game_categories!
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

        var gameIds = gcResponse.Models.Select(x => x.GameId).ToList();
        if (gameIds.Count == 0) return new List<Game>();

        var gamesResponse = await supabaseClient.From<Game>()
            .Select(SelectForCard)
            .Filter("id", Postgrest.Constants.Operator.In, gameIds)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Get(ct);

        return gamesResponse.Models.ToList();
    }

    public async Task<IReadOnlyList<Game>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var rangeStart = (page - 1) * pageSize;
        var rangeEnd = rangeStart + pageSize - 1;

        // Using ILike on embedded column requires inner join logic or similar.
        // Simplest fallback for a demo codebase using Supabase SDK without complex views:
        // We'll search titles in game_translations.
        var transResponse = await supabaseClient.From<GameTranslation>()
            .Filter("language_code", Postgrest.Constants.Operator.Equals, languageCode)
            .Filter("title", Postgrest.Constants.Operator.ILike, $"%{query}%")
            .Range(rangeStart, rangeEnd)
            .Get(ct);
        
        var gameIds = transResponse.Models.Select(t => t.GameId).ToList();
        if (gameIds.Count == 0) return new List<Game>();

        var gamesResponse = await supabaseClient.From<Game>()
            .Select(SelectForCard)
            .Filter("id", Postgrest.Constants.Operator.In, gameIds)
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Get(ct);

        return gamesResponse.Models.ToList();
    }

    public async Task<IReadOnlyList<Game>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var rangeStart = (page - 1) * pageSize;
        var rangeEnd = rangeStart + pageSize - 1;

        var response = await supabaseClient.From<Game>()
            .Select(SelectForCard)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Range(rangeStart, rangeEnd)
            .Get(ct);

        return response.Models.ToList();
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

