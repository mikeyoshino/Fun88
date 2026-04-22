namespace Fun88.Web.Modules.Users.Services;

public interface IGameRatingService
{
    Task UpsertAsync(Guid userId, Guid gameId, int rating, CancellationToken ct = default);
    Task<double> GetAverageAsync(Guid gameId, CancellationToken ct = default);
    Task<int?> GetUserRatingAsync(Guid userId, Guid gameId, CancellationToken ct = default);
}
