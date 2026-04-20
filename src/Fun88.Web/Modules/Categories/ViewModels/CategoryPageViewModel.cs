namespace Fun88.Web.Modules.Categories.ViewModels;

using Fun88.Web.Modules.Games.ViewModels;

public record CategoryPageViewModel(
    string Slug,
    string Name,
    IReadOnlyList<GameCardViewModel> Games,
    int TotalCount,
    int CurrentPage,
    int PageSize
);
