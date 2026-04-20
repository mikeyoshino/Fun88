namespace Fun88.Web.Infrastructure.Data.Entities;

public class GameTranslation
{
    public Guid GameId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ControlDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public Game Game { get; set; } = null!;
}
