namespace Fun88.Web.Modules.Games.Services;

using Fun88.Web.Modules.Games.ViewModels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IGameQueryService
{
    Task<HomeViewModel> GetHomeViewModelAsync(string languageCode, CancellationToken ct = default);
    Task<GameDetailViewModel?> GetDetailViewModelAsync(string slug, string languageCode, string embedUrl, CancellationToken ct = default);
    Task<(IReadOnlyList<GameCardViewModel> Games, int TotalCount)> GetCategoryGamesAsync(string categorySlug, string languageCode, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<GameCardViewModel> Games, int TotalCount)> GetAllGamesAsync(string languageCode, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<GameCardViewModel>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default);
}
