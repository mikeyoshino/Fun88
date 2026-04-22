namespace Fun88.Web.Modules.Admin.ViewModels;

public record AdminTranslationJobViewModel(
    Guid GameId,
    string LanguageCode,
    string GameTitle,
    string Status,
    short AttemptCount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? LastError
);
