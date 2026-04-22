namespace Fun88.Web.Modules.Translation.Services;

using Fun88.Web.Modules.Translation.Models;
using Microsoft.Extensions.Logging;

// Stub implementation — full OpenAI integration is a Plan 3 deliverable.
// When TranslationEnabled = true in config, this service is called by the Quartz jobs.
public class OpenAiTranslationService(ILogger<OpenAiTranslationService> logger) : ITranslationService
{
    public Task<Dictionary<string, string>> TranslateAsync(
        Dictionary<string, string> fields,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default)
    {
        logger.LogWarning(
            "OpenAiTranslationService is not yet implemented. Translation to '{Language}' skipped.",
            targetLanguage);
        throw new NotImplementedException(
            "OpenAI translation service is not yet implemented. See Plan 3.");
    }

    public Task<List<TranslationResult>> TranslateBatchAsync(
        List<TranslationRequest> requests,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default)
    {
        logger.LogWarning(
            "OpenAiTranslationService is not yet implemented. Batch translation to '{Language}' skipped.",
            targetLanguage);
        throw new NotImplementedException(
            "OpenAI translation service is not yet implemented. See Plan 3.");
    }
}
