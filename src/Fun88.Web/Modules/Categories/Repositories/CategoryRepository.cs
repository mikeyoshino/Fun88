namespace Fun88.Web.Modules.Categories.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.ViewModels;
using Supabase;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class CategoryRepository(Client supabaseClient) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryViewModel>> GetAllCategoriesAsync(string languageCode, CancellationToken ct = default)
    {
        // Fetch categories without embedding to avoid PostgREST aggregate-in-FROM error
        var catResp = await supabaseClient.From<Category>()
            .Select("*")
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
            .Get(ct);

        var categories = catResp.Models;
        if (categories.Count == 0) return new List<CategoryViewModel>();

        // Fetch all translations in one query and join in memory
        var catIds = categories.Select(c => c.Id.ToString()).ToList();
        var transResp = await supabaseClient.From<CategoryTranslation>()
            .Filter("category_id", Postgrest.Constants.Operator.In, catIds)
            .Get(ct);

        var transByCategory = transResp.Models
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return categories.Select(c =>
        {
            var translations = transByCategory.TryGetValue(c.Id, out var ts) ? ts : new List<CategoryTranslation>();
            var name = translations.FirstOrDefault(t => t.LanguageCode == languageCode)?.Name
                    ?? translations.FirstOrDefault(t => t.LanguageCode == "en")?.Name
                    ?? c.Slug;
            return new CategoryViewModel { Id = c.Id, Slug = c.Slug, Icon = c.Icon, Name = name };
        }).ToList();
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await supabaseClient.From<Category>()
            .Select("*, category_translations(*)")
            .Filter("slug", Postgrest.Constants.Operator.Equals, slug)
            .Single(ct);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await supabaseClient.From<Category>()
            .Select("*, category_translations(*)")
            .Filter("id", Postgrest.Constants.Operator.Equals, id)
            .Single(ct);
    }
}
