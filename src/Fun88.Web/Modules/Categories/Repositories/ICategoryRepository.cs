namespace Fun88.Web.Modules.Categories.Repositories;

using Fun88.Web.Modules.Categories.ViewModels;
using Fun88.Web.Infrastructure.Data.Entities;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryViewModel>> GetAllCategoriesAsync(string languageCode, CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
