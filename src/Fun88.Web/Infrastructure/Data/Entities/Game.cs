namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("games")]
public class Game : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("provider_id")]
    public int? ProviderId { get; set; }

    [Column("provider_game_id")]
    public string? ProviderGameId { get; set; }

    [Column("game_url")]
    public string GameUrl { get; set; } = string.Empty;

    [Column("thumbnail_url")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [Column("play_count")]
    public long PlayCount { get; set; }

    [Column("like_count")]
    public long LikeCount { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
