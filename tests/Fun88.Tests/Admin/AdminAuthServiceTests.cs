namespace Fun88.Tests.Admin;

using Fun88.Web.Modules.Admin.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Supabase;

public class AdminAuthServiceTests
{
    // Uses a real Supabase.Client pointed at a non-existent host to guarantee
    // SignIn throws a network exception, which the service must catch and return null.
    private static AdminAuthService BuildService() =>
        new AdminAuthService(
            new Client("https://invalid.example.invalid", "fake-key",
                new SupabaseOptions { AutoRefreshToken = false, AutoConnectRealtime = false }),
            NullLogger<AdminAuthService>.Instance);

    [Fact]
    public async Task SignInAsync_WhenNetworkFails_ReturnsNull()
    {
        var service = BuildService();

        var result = await service.SignInAsync("admin@example.com", "wrong-password");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignOutAsync_WhenNotSignedIn_DoesNotThrow()
    {
        var service = BuildService();

        var ex = await Record.ExceptionAsync(() => service.SignOutAsync());

        Assert.Null(ex);
    }
}
