namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("preferred_language")]
    public string PreferredLanguage { get; set; } = "en";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
}
