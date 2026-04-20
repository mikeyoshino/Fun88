namespace Fun88.Web.Modules.Games.ViewModels;

public record GameCardViewModel(
    string Slug,
    string Title,
    string ThumbnailUrl,
    long PlayCount,
    bool IsNew,
    bool IsHot,
    bool IsTop
);
