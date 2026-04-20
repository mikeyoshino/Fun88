namespace Fun88.Web.Modules.Admin.ViewModels;

using System.ComponentModel.DataAnnotations;

public class AdminGameFormViewModel
{
    public Guid? Id { get; set; }

    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ControlDescription { get; set; }

    [Required] public string Slug { get; set; } = string.Empty;
    [Required] public string GameUrl { get; set; } = string.Empty;
    [Required] public string ThumbnailUrl { get; set; } = string.Empty;

    public List<int> SelectedCategoryIds { get; set; } = [];
    public bool IsGdGame { get; set; }
    public bool IsCustomGame => !IsGdGame;
}

public class AdminGameListItemViewModel
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public bool IsGdGame { get; init; }
    public bool IsActive { get; init; }
    public long PlayCount { get; init; }
}
