namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("user_favorites")]
public class UserFavorite : BaseModel
{
    [PrimaryKey("user_id", false)]
    public Guid UserId { get; set; }

    [PrimaryKey("game_id", false)]
    public Guid GameId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
