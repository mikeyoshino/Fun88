namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Modules.Games.ViewModels;

public interface IFavoriteService
{
    Task AddAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<GameCardViewModel>> GetPagedAsync(Guid userId, int page, int pageSize, string lang, CancellationToken ct = default);
}
