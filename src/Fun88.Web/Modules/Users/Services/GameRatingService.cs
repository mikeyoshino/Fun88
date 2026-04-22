namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Infrastructure.Data.Entities;
using Supabase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GameRatingService(Client supabase) : IGameRatingService
{
    public async Task UpsertAsync(Guid userId, Guid gameId, int rating, CancellationToken ct = default)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        await supabase.From<GameRating>()
            .Upsert(new GameRating
            {
                UserId = userId,
                GameId = gameId,
                Rating = (short)rating,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken: ct);
    }

    public async Task<double> GetAverageAsync(Guid gameId, CancellationToken ct = default)
    {
        var response = await supabase.From<GameRating>()
            .Filter("game_id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Get(ct);

        if (response.Models.Count == 0) return 0.0;
        return response.Models.Average(r => (double)r.Rating);
    }

    public async Task<int?> GetUserRatingAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var row = await supabase.From<GameRating>()
            .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
            .Filter("game_id", Postgrest.Constants.Operator.Equals, gameId.ToString())
            .Single(ct);
        return (int?)row?.Rating;
    }
}
