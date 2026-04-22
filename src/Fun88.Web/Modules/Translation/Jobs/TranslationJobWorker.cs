// Placeholder — real implementation added in Task 2
namespace Fun88.Web.Modules.Translation.Jobs;

using Quartz;

public class TranslationJobWorker : IJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}
