namespace Fun88.Web.Modules.Scraper.Services;

using Fun88.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

public class GdEmbedUrlBuilder(IOptions<GameDistributionOptions> options)
{
    private readonly string _baseUrl = options.Value.EmbedBaseUrl;

    public string Build(string providerGameId, string referrerUrl, int tracking, int targeting, int thirdParty)
    {
        var encoded = Uri.EscapeDataString(referrerUrl);
        return $"{_baseUrl}/{providerGameId}/?gd_sdk_referrer_url={encoded}&gdpr-tracking={tracking}&gdpr-targeting={targeting}&gdpr-third-party={thirdParty}";
    }
}
