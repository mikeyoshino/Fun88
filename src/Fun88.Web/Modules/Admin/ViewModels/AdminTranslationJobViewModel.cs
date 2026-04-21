namespace Fun88.Web.Modules.Admin.ViewModels;

public record AdminTranslationJobViewModel(
    Guid GameId,
    string GameTitle,
    string Status,
    DateTime? CreatedAt,
    DateTime? CompletedAt,
    string? LastError
);
