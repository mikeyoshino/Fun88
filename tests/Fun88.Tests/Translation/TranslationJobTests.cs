namespace Fun88.Tests.Translation;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Translation.Jobs;
using Fun88.Web.Modules.Translation.Services;
using Moq;
using Quartz;

public class TranslationJobTests
{
    [Fact]
    public async Task Execute_OnFailure_UpdatesJobAsFailed()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var svc = new Mock<ITranslationService>();
        svc.Setup(s => s.TranslateAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<Fun88.Web.Modules.Translation.Models.TranslationContext>(), "th", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var job = new TranslationJob(stub.Client, svc.Object);
        var ctx = BuildContext(new JobDataMap { ["game_id"] = Guid.NewGuid().ToString() });

        await job.Execute(ctx);
    }

    private static IJobExecutionContext BuildContext(JobDataMap map)
    {
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.MergedJobDataMap).Returns(map);
        return context.Object;
    }
}
