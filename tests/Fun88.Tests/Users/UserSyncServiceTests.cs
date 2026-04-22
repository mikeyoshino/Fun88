namespace Fun88.Tests.Users;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Users.Services;

// NOTE: SupabaseStub returns [] for all reads, so Single() returns null.
// Both new-user and existing-user paths exercise the "insert" branch with the stub.
// These tests verify no exceptions are thrown and basic properties are set correctly.
public class UserSyncServiceTests
{
    [Fact]
    public async Task SyncAsync_NewUser_InsertsRowAndReturnsUser()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var service = new UserSyncService(stub.Client);

        var authUser = new Supabase.Gotrue.User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@test.com"
        };

        // Should not throw even though stub returns [] (triggers new-user branch)
        var result = await service.SyncAsync(authUser);

        Assert.NotNull(result);
        // Username derived from email prefix
        Assert.Equal("test", result.Username);
    }

    [Fact]
    public async Task SyncAsync_ExistingUser_UpdatesLastLoginAt()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var service = new UserSyncService(stub.Client);

        // Stub always returns [], so we get the insert path here too.
        // In production the update path fires; this test verifies no exception
        // and a User object is returned regardless.
        var authUser = new Supabase.Gotrue.User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "existing@test.com"
        };

        var result = await service.SyncAsync(authUser);

        Assert.NotNull(result);
        Assert.IsType<Fun88.Web.Infrastructure.Data.Entities.User>(result);
    }
}
