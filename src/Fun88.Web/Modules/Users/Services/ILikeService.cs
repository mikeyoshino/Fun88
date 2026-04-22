namespace Fun88.Web.Modules.Users.Services;

public interface ILikeService
{
    Task<(long NewCount, bool Liked)> ToggleAsync(Guid userId, Guid gameId, CancellationToken ct = default);
}
