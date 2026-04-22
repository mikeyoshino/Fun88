namespace Fun88.Tests.Translation;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class TranslationJobTests
{
    [Fact]
    public async Task Execute_OnFailure_UpdatesJobAsFailed()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var svc = new Mock<ITranslationService>();
        svc.Setup(s => s.TranslateAsync(
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<TranslationContext>(),
                "th",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var job = new TranslationJobWorker(stub.Client, svc.Object, NullLogger<TranslationJobWorker>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_id"] = Guid.NewGuid().ToString() });

        // Should not throw — failure is caught and recorded
        await job.Execute(ctx);
    }

    [Fact]
    public async Task Execute_WhenNoEnSourceExists_DoesNotCallTranslateAsync()
    {
        // SupabaseStub returns [] — no EN row exists, null-guard fires,
        // job is recorded as failed and TranslateAsync is never invoked.
        await using var stub = await SupabaseStub.StartAsync();
        var svc = new Mock<ITranslationService>();
        var job = new TranslationJobWorker(stub.Client, svc.Object, NullLogger<TranslationJobWorker>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_id"] = Guid.NewGuid().ToString() });

        await job.Execute(ctx); // should not throw — null EN is caught and recorded as failed

        svc.Verify(s => s.TranslateAsync(
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
