namespace Fun88.Web.Modules.Users.ViewModels;

using System.ComponentModel.DataAnnotations;

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [Required]
    [RegularExpression("^(en|th)$", ErrorMessage = "Language must be 'en' or 'th'.")]
    public string PreferredLanguage { get; set; } = "en";
}
