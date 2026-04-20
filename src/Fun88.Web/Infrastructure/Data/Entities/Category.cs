namespace Fun88.Web.Infrastructure.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CategoryTranslation> Translations { get; set; } = [];
    public ICollection<GameCategory> GameCategories { get; set; } = [];
}
