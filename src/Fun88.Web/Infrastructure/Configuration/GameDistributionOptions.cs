namespace Fun88.Web.Infrastructure.Configuration;

public sealed class GameDistributionOptions
{
    public const string Section = "GameDistribution";

    public string ApiKey { get; init; } = string.Empty;
    public string PublisherId { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.gamedistribution.com";
    public string EmbedBaseUrl { get; init; } = "https://html5.gamedistribution.com";
    public int PageSize { get; init; } = 100;
    public int TimeoutSeconds { get; init; } = 30;
}
