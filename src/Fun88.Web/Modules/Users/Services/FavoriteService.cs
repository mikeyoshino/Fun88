namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Games.ViewModels;
using Fun88.Web.Shared.Constants;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class FavoriteService(Client supabase) : IFavoriteService
{
    public async Task AddAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        await supabase.From<UserFavorite>()
            .Upsert(new UserFavorite { UserId = userId, GameId = gameId, CreatedAt = DateTime.UtcNow },
                cancellationToken: ct);
    }

    public async Task RemoveAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        await supabase.From<UserFavorite>()
            .Match(new Dictionary<string, string> { ["user_id"] = userId.ToString(), ["game_id"] = gameId.ToString() })
            .Delete(cancellationToken: ct);
    }

    public async Task<bool> IsFavoriteAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var result = await supabase.From<UserFavorite>()
            .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
            .Filter("game_id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Single(ct);
        return result != null;
    }

    public async Task<IReadOnlyList<GameCardViewModel>> GetPagedAsync(
        Guid userId, int page, int pageSize, string lang, CancellationToken ct = default)
    {
        var rangeStart = (page - 1) * pageSize;
        var rangeEnd = rangeStart + pageSize - 1;

        var favResponse = await supabase.From<UserFavorite>()
            .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
            .Range(rangeStart, rangeEnd)
            .Get(ct);

        var gameIds = favResponse.Models.Select(f => f.GameId).ToList();
        if (gameIds.Count == 0) return [];

        var gamesResponse = await supabase.From<Game>()
            .Select("*, game_translations(*)")
            .Filter("id", Postgrest.Constants.Operator.In, gameIds)
            .Get(ct);

        return gamesResponse.Models.Select(g => ToCard(g, lang)).ToList();
    }

    private static GameCardViewModel ToCard(Game game, string lang)
    {
        var t = game.Translations?.FirstOrDefault(x => x.LanguageCode == lang)
             ?? game.Translations?.FirstOrDefault(x => x.LanguageCode == LanguageCode.English)
             ?? new GameTranslation { Title = game.Slug };

        var isNew = game.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-7);
        var isHot = game.PlayCount > 10_000;
        var isTop = game.LikeCount > 1_000;

        return new GameCardViewModel(game.Slug, t.Title, game.ThumbnailUrl, game.PlayCount, isNew, isHot, isTop);
    }
}
