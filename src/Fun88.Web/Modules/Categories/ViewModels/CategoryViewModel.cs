namespace Fun88.Web.Modules.Categories.ViewModels;

public class CategoryViewModel
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Icon { get; init; }
}
