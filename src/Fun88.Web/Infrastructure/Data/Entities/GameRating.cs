namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("game_ratings")]
public class GameRating : BaseModel
{
    [PrimaryKey("user_id", false)]
    public Guid UserId { get; set; }

    [PrimaryKey("game_id", false)]
    public Guid GameId { get; set; }

    [Column("rating")]
    public short Rating { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
