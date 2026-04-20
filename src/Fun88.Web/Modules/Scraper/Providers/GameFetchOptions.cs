namespace Fun88.Web.Modules.Scraper.Providers;

public record GameFetchOptions(
    int Page = 1,
    int PageSize = 100,
    string? CategoryFilter = null
);
