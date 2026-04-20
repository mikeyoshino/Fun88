namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;

[Table("category_translations")]
public class CategoryTranslation : BaseModel
{
    [PrimaryKey("category_id", false)]
    public int CategoryId { get; set; }

    [PrimaryKey("language_code", false)]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
