namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Infrastructure.Data.Entities;
using Supabase;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class LikeService(Client supabase) : ILikeService
{
    public async Task<(long NewCount, bool Liked)> ToggleAsync(
        Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var existingLike = await supabase.From<UserLike>()
            .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
            .Filter("game_id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Single(ct);

        var game = await supabase.From<Game>()
            .Filter("id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Single(ct);

        if (game is null)
            return (0, false); // game not found — no-op

        var currentLikeCount = game.LikeCount;

        if (existingLike is null)
        {
            await supabase.From<UserLike>()
                .Insert(new UserLike { UserId = userId, GameId = gameId, CreatedAt = DateTime.UtcNow },
                    new Postgrest.QueryOptions(), ct);

            // NOTE: like_count update is non-atomic (read-then-write via PostgREST).
            // For true atomicity, use a Supabase RPC: SELECT increment_like_count(game_id).
            // Concurrent requests may cause minor count drift — acceptable for a non-financial counter.
            game.LikeCount = currentLikeCount + 1;
            await supabase.From<Game>()
                .Match(new Dictionary<string, string> { ["id"] = gameId.ToString() })
                .Update(game, cancellationToken: ct);

            return (currentLikeCount + 1, true);
        }
        else
        {
            await supabase.From<UserLike>()
                .Match(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["game_id"] = gameId.ToString()
                })
                .Delete(cancellationToken: ct);

            // NOTE: like_count update is non-atomic (read-then-write via PostgREST).
            // For true atomicity, use a Supabase RPC: SELECT increment_like_count(game_id).
            // Concurrent requests may cause minor count drift — acceptable for a non-financial counter.
            game.LikeCount = Math.Max(0, currentLikeCount - 1);
            await supabase.From<Game>()
                .Match(new Dictionary<string, string> { ["id"] = gameId.ToString() })
                .Update(game, cancellationToken: ct);

            return (Math.Max(0, currentLikeCount - 1), false);
        }
    }
}
