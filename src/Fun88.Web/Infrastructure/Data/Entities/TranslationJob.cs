namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("translation_jobs")]
public class TranslationJob : BaseModel
{
    [PrimaryKey("game_id", false)]
    public Guid GameId { get; set; }

    [PrimaryKey("language_code", false)]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("attempt_count")]
    public short AttemptCount { get; set; }

    [Column("last_error")]
    public string? LastError { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}
