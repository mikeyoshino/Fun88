namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

[Table("categories")]
public class Category : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Reference(typeof(CategoryTranslation))]
    public List<CategoryTranslation> Translations { get; set; } = [];
}
