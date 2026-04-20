namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("game_translations")]
public class GameTranslation : BaseModel
{
    [PrimaryKey("game_id", false)]
    public Guid GameId { get; set; }

    [PrimaryKey("language_code", false)]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("control_description")]
    public string? ControlDescription { get; set; }

    [Column("meta_title")]
    public string? MetaTitle { get; set; }

    [Column("meta_description")]
    public string? MetaDescription { get; set; }
}
