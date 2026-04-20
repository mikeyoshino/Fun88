namespace Fun88.Web.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string Section = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxTokensPerRequest { get; init; } = 1000;
    public int TranslationTimeoutSeconds { get; init; } = 30;
    public bool TranslationEnabled { get; init; } = false;
}
