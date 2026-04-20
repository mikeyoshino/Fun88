namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;

[Table("game_providers")]
public class GameProvider : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("api_base_url")]
    public string ApiBaseUrl { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
