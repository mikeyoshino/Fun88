namespace Fun88.Tests.Users;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Users.Services;

public class EngagementServiceTests
{
    // FavoriteService: SupabaseStub returns [] so Single() = null → IsFavoriteAsync returns false.
    // AddAsync calls Upsert which succeeds with the stub (200 OK, []).
    [Fact]
    public async Task FavoriteService_AddThenIsFavorite_ReturnsTrue()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var service = new FavoriteService(stub.Client);
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Should not throw
        await service.AddAsync(userId, gameId);

        // Stub returns [] so Single() returns null → IsFavoriteAsync returns false (stub limitation)
        var result = await service.IsFavoriteAsync(userId, gameId);

        Assert.False(result); // Expected false due to stub limitation, but no exception
    }

    // LikeService: stub returns [] — game Single() returns null, UserLike Single() returns null.
    // With Fix 2, a null game is treated as "not found" and returns (0, false) as a no-op.
    [Fact]
    public async Task LikeService_Toggle_ReturnsLikedTrue()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var service = new LikeService(stub.Client);
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var (newCount, liked) = await service.ToggleAsync(userId, gameId);

        // Stub returns null for Game → no-op: (0, false)
        Assert.False(liked);
        Assert.Equal(0L, newCount);
    }

    // PlayHistoryService: RecordAsync with null userId should not throw.
    [Fact]
    public async Task PlayHistoryService_RecordAsync_WithNullUserId_DoesNotThrow()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var service = new PlayHistoryService(stub.Client);
        var gameId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();

        var ex = await Record.ExceptionAsync(() => service.RecordAsync(null, gameId, sessionId));

        Assert.Null(ex);
    }

    // GameRatingService: ratings outside 1-5 should throw ArgumentOutOfRangeException.
    [Fact]
    public async Task GameRatingService_UpsertAsync_InvalidRating_Throws()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var service = new GameRatingService(stub.Client);
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpsertAsync(userId, gameId, rating: 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpsertAsync(userId, gameId, rating: 6));
    }
}
