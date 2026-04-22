namespace Fun88.Tests.Translation;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class BulkTranslationJobTests
{
    [Fact]
    public async Task Execute_WithNoIds_DoesNothing()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var svc = new Mock<ITranslationService>();
        var job = new BulkTranslationJob(stub.Client, svc.Object, NullLogger<BulkTranslationJob>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_ids"] = "" });

        await job.Execute(ctx);

        svc.Verify(s => s.TranslateBatchAsync(
            It.IsAny<IReadOnlyList<TranslationRequest>>(),
            It.IsAny<TranslationContext>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WithValidIds_CallsTranslateBatch()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var id = Guid.NewGuid();
        var svc = new Mock<ITranslationService>();
        svc.Setup(s => s.TranslateBatchAsync(
                It.IsAny<IReadOnlyList<TranslationRequest>>(),
                It.IsAny<TranslationContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TranslationResult(id.ToString(), new Dictionary<string, string>
            {
                ["title"] = "แปลแล้ว",
                ["description"] = "คำอธิบาย",
                ["control_description"] = "วิธีเล่น"
            })]);

        var job = new BulkTranslationJob(stub.Client, svc.Object, NullLogger<BulkTranslationJob>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_ids"] = id.ToString() });

        await job.Execute(ctx);

        svc.Verify(s => s.TranslateBatchAsync(
            It.IsAny<IReadOnlyList<TranslationRequest>>(),
            It.IsAny<TranslationContext>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IJobExecutionContext BuildContext(JobDataMap map)
    {
        var ctx = new Mock<IJobExecutionContext>();
        ctx.SetupGet(c => c.MergedJobDataMap).Returns(map);
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return ctx.Object;
    }
}
