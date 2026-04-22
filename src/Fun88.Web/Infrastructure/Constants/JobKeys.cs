namespace Fun88.Web.Infrastructure.Constants;

using Quartz;

public static class JobKeys
{
    public static readonly JobKey Scraper = new("scraper-gd", "scraper");
    public const int GameDistributionProviderId = 1;
}
