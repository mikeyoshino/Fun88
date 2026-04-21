namespace Fun88.Web.Modules.Translation.Services;

using Fun88.Web.Modules.Translation.Models;

public interface ITranslationService
{
    Task<Dictionary<string, string>> TranslateAsync(
        Dictionary<string, string> fields,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default);

    Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
        IReadOnlyList<TranslationRequest> requests,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default);
}
