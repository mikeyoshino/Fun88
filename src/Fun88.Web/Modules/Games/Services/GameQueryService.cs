namespace Fun88.Web.Modules.Games.Services;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Games.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fun88.Web.Shared.Constants;

public class GameQueryService(IGameRepository gameRepo, ICategoryRepository categoryRepo) : IGameQueryService
{
    public async Task<HomeViewModel> GetHomeViewModelAsync(string languageCode, CancellationToken ct = default)
    {
        var newest = await gameRepo.GetNewestAsync(12, ct);
        var popular = await gameRepo.GetMostPopularAsync(12, ct);
        
        var sections = new List<HomeCategorySection>();
        var topCats = await categoryRepo.GetAllCategoriesAsync(languageCode, ct);
        foreach(var c in topCats.Take(3))
        {
            var cg = await gameRepo.GetByCategorySlugAsync(c.Slug, 1, 10, ct);
            sections.Add(new HomeCategorySection(c.Name, c.Slug, cg.Select(g => ToCard(g, languageCode)).ToList()));
        }

        return new HomeViewModel(
            popular.Take(4).Select(g => ToCard(g, languageCode)).ToList(), // Featured (just using popular logic for now)
            newest.Select(g => ToCard(g, languageCode)).ToList(),
            popular.Select(g => ToCard(g, languageCode)).ToList(),
            sections
        );
    }

    public async Task<GameDetailViewModel?> GetDetailViewModelAsync(string slug, string languageCode, string embedUrl, CancellationToken ct = default)
    {
        var game = await gameRepo.GetBySlugAsync(slug, ct);
        if (game == null) return null;

        var t = game.Translations?.FirstOrDefault(x => x.LanguageCode == languageCode)
             ?? game.Translations?.FirstOrDefault(x => x.LanguageCode == LanguageCode.English)
             ?? new GameTranslation { Title = game.Slug };

        var catNames = new List<string>();
        foreach(var gc in game.GameCategories ?? Enumerable.Empty<GameCategory>())
        {
            if (gc.Category != null)
            {
                var ctName = gc.Category.Translations?.FirstOrDefault(x => x.LanguageCode == languageCode)?.Name 
                          ?? gc.Category.Translations?.FirstOrDefault(x => x.LanguageCode == LanguageCode.English)?.Name
                          ?? gc.Category.Slug;
                catNames.Add(ctName);
            }
        }

        var relatedGames = await gameRepo.GetByCategorySlugAsync(game.GameCategories?.FirstOrDefault()?.Category?.Slug ?? "", 1, 6, ct);

        return new GameDetailViewModel(
            game.Id,
            game.Slug,
            t.Title,
            t.Description,
            t.ControlDescription,
            game.ThumbnailUrl,
            embedUrl,
            game.PlayCount,
            game.LikeCount,
            catNames,
            relatedGames.Where(g => g.Id != game.Id).Select(g => ToCard(g, languageCode)).ToList()
        );
    }

    public async Task<(IReadOnlyList<GameCardViewModel> Games, int TotalCount)> GetCategoryGamesAsync(string categorySlug, string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var count = await gameRepo.CountByCategorySlugAsync(categorySlug, ct);
        var games = await gameRepo.GetByCategorySlugAsync(categorySlug, page, pageSize, ct);
        return (games.Select(g => ToCard(g, languageCode)).ToList(), count);
    }

    public async Task<(IReadOnlyList<GameCardViewModel> Games, int TotalCount)> GetAllGamesAsync(string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var count = await gameRepo.CountAllAsync(ct);
        var games = await gameRepo.GetAllPagedAsync(page, pageSize, ct);
        return (games.Select(g => ToCard(g, languageCode)).ToList(), count);
    }

    public async Task<IReadOnlyList<GameCardViewModel>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var games = await gameRepo.SearchAsync(query, languageCode, page, pageSize, ct);
        return games.Select(g => ToCard(g, languageCode)).ToList();
    }

    private static GameCardViewModel ToCard(Game game, string languageCode)
    {
        var t = game.Translations?.FirstOrDefault(x => x.LanguageCode == languageCode)
             ?? game.Translations?.FirstOrDefault(x => x.LanguageCode == LanguageCode.English)
             ?? new GameTranslation { Title = game.Slug };

        var isNew = game.CreatedAt >= DateTime.UtcNow.AddDays(-7);
        var isHot = game.PlayCount > 10_000;
        var isTop = game.LikeCount > 1_000;

        return new GameCardViewModel(game.Slug, t.Title, game.ThumbnailUrl, game.PlayCount, isNew, isHot, isTop);
    }
}
