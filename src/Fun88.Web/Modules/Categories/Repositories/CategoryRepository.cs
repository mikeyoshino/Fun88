namespace Fun88.Web.Modules.Categories.Repositories;

using Microsoft.EntityFrameworkCore;
using Fun88.Web.Infrastructure.Data;
using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.ViewModels;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryViewModel>> GetAllCategoriesAsync(string languageCode, CancellationToken ct = default)
    {
        return await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Slug = c.Slug,
                Icon = c.Icon,
                Name = c.Translations
                        .Where(t => t.LanguageCode == languageCode)
                        .Select(t => t.Name)
                        .FirstOrDefault() ?? c.Slug
            })
            .ToListAsync(ct);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await db.Categories
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);
    }
}
