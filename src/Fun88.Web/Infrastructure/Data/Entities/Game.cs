namespace Fun88.Web.Infrastructure.Data.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int? ProviderId { get; set; }
    public string? ProviderGameId { get; set; }
    public string GameUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long PlayCount { get; set; }
    public long LikeCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GameProvider? Provider { get; set; }
    public ICollection<GameTranslation> Translations { get; set; } = [];
    public ICollection<GameCategory> GameCategories { get; set; } = [];
}
