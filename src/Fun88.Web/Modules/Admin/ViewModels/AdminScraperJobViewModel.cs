namespace Fun88.Web.Modules.Admin.ViewModels;

public record AdminScraperJobViewModel(
    Guid Id,
    string Status,
    string TriggeredBy,
    int GamesFound,
    int GamesImported,
    int GamesSkipped,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt);
