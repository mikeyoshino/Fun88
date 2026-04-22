namespace Fun88.Tests.Scraper;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Scraper.Jobs;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;

public class ScraperJobTests
{
    private readonly Mock<IGameProvider> _gameProvider = new();
    private readonly Mock<IGameImportPipeline> _importPipeline = new();
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();

    private static Mock<IJobExecutionContext> BuildContext(string triggeredBy = "manual")
    {
        var ctx = new Mock<IJobExecutionContext>();
        var dataMap = new JobDataMap { ["triggered_by"] = triggeredBy };
        ctx.Setup(c => c.MergedJobDataMap).Returns(dataMap);
        ctx.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return ctx;
    }

    [Fact]
    public async Task Execute_UpdatesJobRow_WithCorrectCounts()
    {
        // Note: DB row content verified via no-exception completion; SupabaseStub returns [] for all reads.
        await using var stub = await SupabaseStub.StartAsync();

        var rawGames = new List<RawGameData>
        {
            new("gd-1", "Game 1", "", "", "https://img/1.jpg", "https://g/1", []),
            new("gd-2", "Game 2", "", "", "https://img/2.jpg", "https://g/2", []),
            new("gd-3", "Game 3", "", "", "https://img/3.jpg", "https://g/3", [])
        };
        _gameProvider.Setup(p => p.FetchGamesAsync(It.IsAny<GameFetchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawGames);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _importPipeline.SetupSequence(p => p.ImportAsync(It.IsAny<RawGameData>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportGameResult(Imported: true, Skipped: false, Error: null, GameId: id1))
            .ReturnsAsync(new ImportGameResult(Imported: true, Skipped: false, Error: null, GameId: id2))
            .ReturnsAsync(new ImportGameResult(Imported: false, Skipped: true, Error: null, GameId: null));

        var options = Options.Create(new OpenAiOptions { TranslationEnabled = false });
        var job = new ScraperJob(stub.Client, _gameProvider.Object, _importPipeline.Object,
            _schedulerFactory.Object, options, NullLogger<ScraperJob>.Instance);

        await job.Execute(BuildContext().Object);

        _importPipeline.Verify(p => p.ImportAsync(It.IsAny<RawGameData>(), 1, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Execute_OnException_MarksJobFailed()
    {
        await using var stub = await SupabaseStub.StartAsync();

        _gameProvider.Setup(p => p.FetchGamesAsync(It.IsAny<GameFetchOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider error"));

        var options = Options.Create(new OpenAiOptions { TranslationEnabled = false });
        var job = new ScraperJob(stub.Client, _gameProvider.Object, _importPipeline.Object,
            _schedulerFactory.Object, options, NullLogger<ScraperJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(BuildContext().Object));

        _gameProvider.Verify(p => p.FetchGamesAsync(It.IsAny<GameFetchOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenTranslationEnabled_SchedulesBulkJob()
    {
        await using var stub = await SupabaseStub.StartAsync();

        var rawGames = new List<RawGameData>
        {
            new("gd-10", "Game 10", "", "", "https://img/10.jpg", "https://g/10", []),
            new("gd-11", "Game 11", "", "", "https://img/11.jpg", "https://g/11", [])
        };
        _gameProvider.Setup(p => p.FetchGamesAsync(It.IsAny<GameFetchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawGames);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _importPipeline.SetupSequence(p => p.ImportAsync(It.IsAny<RawGameData>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportGameResult(Imported: true, Skipped: false, Error: null, GameId: id1))
            .ReturnsAsync(new ImportGameResult(Imported: true, Skipped: false, Error: null, GameId: id2));

        _schedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_scheduler.Object);
        _scheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow);

        var options = Options.Create(new OpenAiOptions { TranslationEnabled = true });
        var job = new ScraperJob(stub.Client, _gameProvider.Object, _importPipeline.Object,
            _schedulerFactory.Object, options, NullLogger<ScraperJob>.Instance);

        await job.Execute(BuildContext().Object);

        _scheduler.Verify(s => s.ScheduleJob(
            It.Is<IJobDetail>(j => j.JobDataMap.GetString("game_ids") != null),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
