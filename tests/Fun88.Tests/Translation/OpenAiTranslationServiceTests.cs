namespace Fun88.Tests.Translation;

using System.Net;
using System.Net.Http;
using System.Text;
using Fun88.Web.Infrastructure.Clients;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class OpenAiTranslationServiceTests
{
    [Fact]
    public async Task TranslateBatchAsync_WhenDisabled_ReturnsEmpty()
    {
        var svc = BuildService(enabled: false, "[]");
        var result = await svc.TranslateBatchAsync(
            [new TranslationRequest("1", new Dictionary<string, string> { ["title"] = "Test" })],
            TranslationContext.Game,
            "th");
        Assert.Empty(result);
    }

    [Fact]
    public async Task TranslateBatchAsync_SplitsIntoChunksOf20()
    {
        var responseJson = """{"choices":[{"message":{"content":"[]"}}]}""";
        var handler = new CountingHandler(responseJson);
        var svc = BuildService(enabled: true, responseJson, handler);

        var requests = Enumerable.Range(1, 21)
            .Select(i => new TranslationRequest(i.ToString(), new Dictionary<string, string> { ["title"] = $"G{i}" }))
            .ToList();

        await svc.TranslateBatchAsync(requests, TranslationContext.Game, "th");
        Assert.Equal(2, handler.CallCount);
    }

    private static OpenAiTranslationService BuildService(bool enabled, string responseJson, CountingHandler? handler = null)
    {
        handler ??= new CountingHandler(responseJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var openAi = new OpenAiHttpClient(client);
        var opts = Options.Create(new OpenAiOptions
        {
            TranslationEnabled = enabled,
            Model = "gpt-4o-mini",
            MaxTokensPerRequest = 1000,
            TranslationTimeoutSeconds = 30
        });
        return new OpenAiTranslationService(openAi, opts, NullLogger<OpenAiTranslationService>.Instance);
    }

    private sealed class CountingHandler(string responseJson) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
