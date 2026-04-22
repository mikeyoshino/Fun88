namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Modules.Games.ViewModels;

public interface IPlayHistoryService
{
    Task RecordAsync(Guid? userId, Guid gameId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<GameCardViewModel>> GetRecentAsync(Guid userId, int limit, string lang, CancellationToken ct = default);
}
