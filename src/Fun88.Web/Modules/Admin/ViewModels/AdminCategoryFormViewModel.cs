namespace Fun88.Web.Modules.Admin.ViewModels;

using System.ComponentModel.DataAnnotations;

public class AdminCategoryFormViewModel
{
    public int? Id { get; set; }

    [Required] public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    [Required] public string NameEn { get; set; } = string.Empty;
    public string? NameTh { get; set; }
}
