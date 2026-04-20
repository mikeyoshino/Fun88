namespace Fun88.Web.Infrastructure.Data.Entities;

public class CategoryTranslation
{
    public int CategoryId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Category Category { get; set; } = null!;
}
