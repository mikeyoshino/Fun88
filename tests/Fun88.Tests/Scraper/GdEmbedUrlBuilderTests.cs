namespace Fun88.Tests.Scraper;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Scraper.Services;
using Microsoft.Extensions.Options;

public class GdEmbedUrlBuilderTests
{
    private readonly GdEmbedUrlBuilder _builder;

    public GdEmbedUrlBuilderTests()
    {
        var opts = Options.Create(new GameDistributionOptions
        {
            ApiKey = "test",
            PublisherId = "pub123",
            EmbedBaseUrl = "https://html5.gamedistribution.com"
        });
        _builder = new GdEmbedUrlBuilder(opts);
    }

    [Fact]
    public void Build_WithAllConsent_ReturnsCorrectUrl()
    {
        var url = _builder.Build("abc123", "https://fun88.com/games/test-game", tracking: 1, targeting: 1, thirdParty: 1);

        Assert.Equal(
            "https://html5.gamedistribution.com/abc123/?gd_sdk_referrer_url=https%3A%2F%2Ffun88.com%2Fgames%2Ftest-game&gdpr-tracking=1&gdpr-targeting=1&gdpr-third-party=1",
            url);
    }

    [Fact]
    public void Build_WithNoConsent_AllFlagsZero()
    {
        var url = _builder.Build("abc123", "https://fun88.com/games/test-game", tracking: 0, targeting: 0, thirdParty: 0);

        Assert.Contains("gdpr-tracking=0", url);
        Assert.Contains("gdpr-targeting=0", url);
        Assert.Contains("gdpr-third-party=0", url);
    }

    [Fact]
    public void Build_EncodesReferrerUrl()
    {
        var url = _builder.Build("abc123", "https://fun88.com/games/test game?foo=bar&baz=1", tracking: 1, targeting: 1, thirdParty: 1);

        Assert.Contains("gd_sdk_referrer_url=https%3A%2F%2Ffun88.com%2Fgames%2Ftest%20game%3Ffoo%3Dbar%26baz%3D1", url);
    }
}
