namespace Fun88.Web.Modules.Translation.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Fun88.Web.Infrastructure.Clients;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class OpenAiTranslationService(
    OpenAiHttpClient openAiClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiTranslationService> logger) : ITranslationService
{
    private const int BatchSize = 20;
    private const string SystemPrompt = """
You are a professional Thai game localization engine.
- Keep game titles and proper nouns in English.
- Preserve all HTML tags exactly as-is.
- Use common Thai gaming terminology.
- Output only valid JSON array matching input structure, same ordering.
""";

    private readonly OpenAiOptions _opts = options.Value;

    public async Task<Dictionary<string, string>> TranslateAsync(
        Dictionary<string, string> fields,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default)
    {
        if (!_opts.TranslationEnabled)
        {
            logger.LogWarning("Translation disabled via OpenAiOptions.");
            return new Dictionary<string, string>();
        }

        var payload = BuildPayload(new[] { new TranslationRequest("single", fields) }, context, targetLanguage);
        var response = await openAiClient.Client.PostAsJsonAsync("chat/completions", payload, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var content = ExtractContent(json);
        var results = JsonSerializer.Deserialize<List<TranslationResult>>(content) ?? [];
        return results.FirstOrDefault()?.TranslatedFields ?? new Dictionary<string, string>();
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
        IReadOnlyList<TranslationRequest> requests,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default)
    {
        if (!_opts.TranslationEnabled)
        {
            logger.LogWarning("Translation disabled via OpenAiOptions.");
            return [];
        }

        var results = new List<TranslationResult>();
        foreach (var chunk in requests.Chunk(BatchSize))
        {
            var payload = BuildPayload(chunk, context, targetLanguage);
            var response = await openAiClient.Client.PostAsJsonAsync("chat/completions", payload, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var content = ExtractContent(json);
            var chunkResults = JsonSerializer.Deserialize<List<TranslationResult>>(content) ?? [];
            results.AddRange(chunkResults);
        }

        return results;
    }

    private object BuildPayload(IEnumerable<TranslationRequest> requests, TranslationContext context, string targetLang)
    {
        var body = new
        {
            model = _opts.Model,
            max_tokens = _opts.MaxTokensPerRequest,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = JsonSerializer.Serialize(new
                {
                    context = context.ToString(),
                    targetLanguage = targetLang,
                    items = requests
                }) }
            }
        };
        return body;
    }

    private static string ExtractContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "[]";
    }
}
