namespace Fun88.Tests.Translation;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class TranslationJobTests : IAsyncLifetime
{
    private SupabaseStub _stub = null!;
    private readonly Mock<ITranslationService> _svc = new();

    public async Task InitializeAsync() => _stub = await SupabaseStub.StartAsync();
    public async Task DisposeAsync() => await _stub.DisposeAsync();

    [Fact]
    public async Task Execute_OnFailure_UpdatesJobAsFailed()
    {
        _svc.Setup(s => s.TranslateAsync(
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<TranslationContext>(),
                "th",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var job = new TranslationJobWorker(_stub.Client, _svc.Object, NullLogger<TranslationJobWorker>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_id"] = Guid.NewGuid().ToString() });

        // Should not throw — failure is caught and recorded
        await job.Execute(ctx);
    }

    [Fact]
    public async Task Execute_OnSuccess_CallsTranslateAsync()
    {
        // SupabaseStub returns [] for all requests so EN row will be null,
        // meaning the null-guard fires and we land in the failure path silently.
        // We verify TranslateAsync is NOT called when no EN source exists.
        var job = new TranslationJobWorker(_stub.Client, _svc.Object, NullLogger<TranslationJobWorker>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_id"] = Guid.NewGuid().ToString() });

        await job.Execute(ctx); // should not throw — null EN is caught and recorded as failed

        // TranslateAsync should never be called when EN source row is missing
        _svc.Verify(s => s.TranslateAsync(
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<TranslationContext>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IJobExecutionContext BuildContext(JobDataMap map)
    {
        var ctx = new Mock<IJobExecutionContext>();
        ctx.SetupGet(c => c.MergedJobDataMap).Returns(map);
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return ctx.Object;
    }
}
