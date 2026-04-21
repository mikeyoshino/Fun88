namespace Fun88.Tests.Translation;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Modules.Translation.Services;
using Moq;
using Quartz;

public class BulkTranslationJobTests
{
    [Fact]
    public async Task Execute_WithNoIds_DoesNothing()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var svc = new Mock<ITranslationService>();
        var job = new BulkTranslationJob(stub.Client, svc.Object);
        var ctx = BuildContext(new JobDataMap { ["game_ids"] = "" });
        await job.Execute(ctx);
    }

    private static IJobExecutionContext BuildContext(JobDataMap map)
    {
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.MergedJobDataMap).Returns(map);
        return context.Object;
    }
}
