namespace Fun88.Tests.Scraper;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Moq;

public class GameImportPipelineTests
{
    private readonly Mock<IGameRepository> _gameRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly GameImportPipeline _pipeline;

    public GameImportPipelineTests()
    {
        _pipeline = new GameImportPipeline(_gameRepo.Object, _categoryRepo.Object);
    }

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
        _gameRepo.Verify(r => r.AddAsync(
            It.Is<Game>(g => g.Slug == "new-game" && g.ProviderGameId == "gd-999"),
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
}
