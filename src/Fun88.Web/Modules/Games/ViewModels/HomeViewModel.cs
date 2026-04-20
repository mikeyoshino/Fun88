namespace Fun88.Web.Modules.Games.ViewModels;

using System.Collections.Generic;

public record HomeCategorySection(string CategoryName, string CategorySlug, IReadOnlyList<GameCardViewModel> Games);

public record HomeViewModel(
    IReadOnlyList<GameCardViewModel> FeaturedGames,
    IReadOnlyList<GameCardViewModel> NewestGames,
    IReadOnlyList<GameCardViewModel> MostPopularGames,
    IReadOnlyList<HomeCategorySection> CategorySections
);
