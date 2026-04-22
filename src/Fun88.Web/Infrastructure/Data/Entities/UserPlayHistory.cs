namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("user_play_history")]
public class UserPlayHistory : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("played_at")]
    public DateTime PlayedAt { get; set; }

    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;
}
