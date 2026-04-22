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

public class PlayHistoryService(Client supabase) : IPlayHistoryService
{
    public async Task RecordAsync(Guid? userId, Guid gameId, string sessionId, CancellationToken ct = default)
    {
        await supabase.From<UserPlayHistory>()
            .Insert(new UserPlayHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameId = gameId,
                SessionId = sessionId,
                PlayedAt = DateTime.UtcNow
            }, new Postgrest.QueryOptions(), ct);

        var game = await supabase.From<Game>()
            .Filter("id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Single(ct);

        if (game != null)
        {
            // NOTE: play_count update is non-atomic (read-then-write via PostgREST).
            // For true atomicity, use a Supabase RPC: SELECT increment_play_count(game_id).
            // Concurrent requests may cause minor count drift — acceptable for a non-financial counter.
            game.PlayCount += 1;
            await supabase.From<Game>()
                .Match(new Dictionary<string, string> { ["id"] = gameId.ToString() })
                .Update(game, cancellationToken: ct);
        }
    }

    public async Task<IReadOnlyList<GameCardViewModel>> GetRecentAsync(
        Guid userId, int limit, string lang, CancellationToken ct = default)
    {
        var historyResponse = await supabase.From<UserPlayHistory>()
            .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
            .Order("played_at", Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get(ct);

        var gameIds = historyResponse.Models.Select(h => h.GameId).ToList();
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
