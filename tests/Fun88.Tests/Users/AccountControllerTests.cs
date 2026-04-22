namespace Fun88.Tests.Users;

using Fun88.Tests.Infrastructure;
using Fun88.Web.Modules.Users.Controllers;
using Fun88.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

// Smoke tests for AccountController GET actions — no Supabase Auth mock needed.
public class AccountControllerTests
{
    [Fact]
    public async Task Login_Get_ReturnsView()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var userSyncMock = new Mock<IUserSyncService>();
        var controller = new AccountController(stub.Client, userSyncMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Login(returnUrl: null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Register_Get_ReturnsView()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var userSyncMock = new Mock<IUserSyncService>();
        var controller = new AccountController(stub.Client, userSyncMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Register();

        Assert.IsType<ViewResult>(result);
    }
}
