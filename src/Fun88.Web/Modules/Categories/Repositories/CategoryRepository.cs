namespace Fun88.Web.Modules.Categories.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.ViewModels;
using Supabase;

public class CategoryRepository(Client supabaseClient) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryViewModel>> GetAllCategoriesAsync(string languageCode, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Category>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, true)
            .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
            .Select("id, slug, icon, sort_order, category_translations(name, language_code)")
            .Get(ct);

        var categories = response.Models;

        var result = new List<CategoryViewModel>();
        foreach (var c in categories)
        {
            var translation = c.Translations?.FirstOrDefault(t => t.LanguageCode == languageCode);
            result.Add(new CategoryViewModel
            {
                Id = c.Id,
                Slug = c.Slug,
                Icon = c.Icon,
                Name = translation?.Name ?? c.Slug
            });
        }

        return result;
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Category>()
            .Select("id, slug, icon, sort_order, is_active, category_translations(name, language_code)")
            .Filter("slug", Postgrest.Constants.Operator.Equals, slug)
            .Single(ct);

        return response;
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await supabaseClient.From<Category>()
            .Select("id, slug, icon, sort_order, is_active, category_translations(name, language_code)")
            .Filter("id", Postgrest.Constants.Operator.Equals, id)
            .Single(ct);

        return response;
    }
}
