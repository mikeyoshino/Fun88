namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("scraper_jobs")]
public class ScraperJob : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Column("triggered_by")]
    public string TriggeredBy { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("games_found")]
    public int GamesFound { get; set; }

    [Column("games_imported")]
    public int GamesImported { get; set; }

    [Column("games_skipped")]
    public int GamesSkipped { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}
