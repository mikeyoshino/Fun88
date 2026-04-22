namespace Fun88.Tests.Scraper;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;

public class GameImportPipelineTests : IAsyncLifetime
{
    private readonly Mock<IGameRepository> _gameRepo = new();
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly IOptions<OpenAiOptions> _openAiOptions = Options.Create(new OpenAiOptions { TranslationEnabled = false });
    private SupabaseStub _stub = null!;
    private GameImportPipeline _pipeline = null!;

    public async Task InitializeAsync()
    {
        _stub = await SupabaseStub.StartAsync();
        _pipeline = new GameImportPipeline(_gameRepo.Object, _stub.Client, _schedulerFactory.Object, _openAiOptions);
    }

    public async Task DisposeAsync() => await _stub.DisposeAsync();

    [Fact]
    public async Task ImportAsync_WhenGameAlreadyExists_ReturnsSkippedWithoutAdding()
    {
        var raw = new RawGameData("gd-123", "Test Game", "Desc", string.Empty, "https://img.com/t.jpg", "https://game.com", []);
        _gameRepo.Setup(r => r.GetByProviderGameIdAsync(1, "gd-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Game { Id = Guid.NewGuid(), Slug = "test-game" });

        var result = await _pipeline.ImportAsync(raw, providerId: 1);

        Assert.False(result.Imported);
        Assert.True(result.Skipped);
        _gameRepo.Verify(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WhenGameIsNew_CallsAddAndReturnsImported()
    {
        var raw = new RawGameData("gd-999", "New Game", "Desc", string.Empty, "https://img.com/t.jpg", "https://game.com", []);
        _gameRepo.Setup(r => r.GetByProviderGameIdAsync(1, "gd-999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);
        _gameRepo.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _pipeline.ImportAsync(raw, providerId: 1);

        Assert.True(result.Imported);
        Assert.False(result.Skipped);
        Assert.NotNull(result.GameId);
        _gameRepo.Verify(r => r.AddAsync(
            It.Is<Game>(g => g.Slug == "new-game" && g.ProviderGameId == "gd-999"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WhenTranslationEnabled_SchedulesQuartzJob()
    {
        var enabledOptions = Options.Create(new OpenAiOptions { TranslationEnabled = true });
        _schedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_scheduler.Object);
        var pipeline = new GameImportPipeline(_gameRepo.Object, _stub.Client, _schedulerFactory.Object, enabledOptions);

        var raw = new RawGameData("gd-777", "Enabled Game", "Desc", string.Empty, "https://img.com/t.jpg", "https://game.com", []);
        _gameRepo.Setup(r => r.GetByProviderGameIdAsync(1, "gd-777", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);
        _gameRepo.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await pipeline.ImportAsync(raw, providerId: 1);

        _scheduler.Verify(s => s.ScheduleJob(
            It.Is<IJobDetail>(j => j.JobDataMap.GetString("game_id") != null),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportCustomAsync_WhenSlugCollides_ReturnsErrorWithoutAdding()
    {
        var custom = new CustomGameData("existing-slug", "Title", null, null, "https://img.com/t.jpg", "https://game.com", []);
        _gameRepo.Setup(r => r.GetBySlugAsync("existing-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Game { Id = Guid.NewGuid(), Slug = "existing-slug" });

        var result = await _pipeline.ImportCustomAsync(custom);

        Assert.False(result.Imported);
        Assert.False(result.Skipped);
        Assert.NotNull(result.Error);
        _gameRepo.Verify(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportCustomAsync_WhenSlugIsNew_CallsAddAndReturnsImported()
    {
        var custom = new CustomGameData("new-slug", "Title", null, null, "https://img.com/t.jpg", "https://game.com", []);
        _gameRepo.Setup(r => r.GetBySlugAsync("new-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);
        _gameRepo.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _pipeline.ImportCustomAsync(custom);

        Assert.True(result.Imported);
        Assert.False(result.Skipped);
        Assert.Null(result.Error);
        Assert.NotNull(result.GameId);
        _gameRepo.Verify(r => r.AddAsync(
            It.Is<Game>(g => g.Slug == "new-slug"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
