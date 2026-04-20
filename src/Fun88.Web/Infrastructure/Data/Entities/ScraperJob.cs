namespace Fun88.Web.Infrastructure.Data.Entities;

public class ScraperJob
{
    public Guid Id { get; set; }
    public int ProviderId { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;   // "schedule" | "manual"
    public string Status { get; set; } = "pending";           // "pending"|"running"|"completed"|"failed"
    public int GamesFound { get; set; }
    public int GamesImported { get; set; }
    public int GamesSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public GameProvider Provider { get; set; } = null!;
}
