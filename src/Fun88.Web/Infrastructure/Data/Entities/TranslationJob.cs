namespace Fun88.Web.Infrastructure.Data.Entities;

public class TranslationJob
{
    public Guid GameId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";          // "pending"|"completed"|"failed"
    public short AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Game Game { get; set; } = null!;
}
