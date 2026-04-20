namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("game_categories")]
public class GameCategory : BaseModel
{
    [PrimaryKey("game_id", false)]
    public Guid GameId { get; set; }

    [PrimaryKey("category_id", false)]
    public int CategoryId { get; set; }
}
