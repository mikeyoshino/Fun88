namespace Fun88.Tests.Translation;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Modules.Translation.Models;
using Fun88.Web.Modules.Translation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class BulkTranslationJobTests : IAsyncLifetime
{
    private SupabaseStub _stub = null!;
    private readonly Mock<ITranslationService> _svc = new();

    public async Task InitializeAsync() => _stub = await SupabaseStub.StartAsync();
    public async Task DisposeAsync() => await _stub.DisposeAsync();

    [Fact]
    public async Task Execute_WithNoIds_DoesNothing()
    {
        var job = new BulkTranslationJob(_stub.Client, _svc.Object, NullLogger<BulkTranslationJob>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_ids"] = "" });

        await job.Execute(ctx);

        _svc.Verify(s => s.TranslateBatchAsync(
            It.IsAny<List<TranslationRequest>>(),
            It.IsAny<TranslationContext>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WithValidIds_CallsTranslateBatch()
    {
        var id = Guid.NewGuid();
        _svc.Setup(s => s.TranslateBatchAsync(
                It.IsAny<List<TranslationRequest>>(),
                It.IsAny<TranslationContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TranslationResult(id.ToString(), new Dictionary<string, string>
            {
                ["title"] = "แปลแล้ว",
                ["description"] = "คำอธิบาย",
                ["control_description"] = "วิธีเล่น"
            })]);

        var job = new BulkTranslationJob(_stub.Client, _svc.Object, NullLogger<BulkTranslationJob>.Instance);
        var ctx = BuildContext(new JobDataMap { ["game_ids"] = id.ToString() });

        await job.Execute(ctx);

        _svc.Verify(s => s.TranslateBatchAsync(
            It.IsAny<List<TranslationRequest>>(),
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
