using Fun88.Tests.Infrastructure;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Users.Controllers;
using Fun88.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

// Smoke tests for AccountController GET actions — no Supabase Auth mock needed.
public class AccountControllerTests
{
    private static AccountController BuildController(Supabase.Client client, IUserSyncService userSync)
        => new(client, userSync, Options.Create(new AuthCookieOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task Login_Get_ReturnsView()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var controller = BuildController(stub.Client, new Mock<IUserSyncService>().Object);

        var result = controller.Login(returnUrl: null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Register_Get_ReturnsView()
    {
        await using var stub = await SupabaseStub.StartAsync();
        var controller = BuildController(stub.Client, new Mock<IUserSyncService>().Object);

        var result = controller.Register();

        Assert.IsType<ViewResult>(result);
    }
}
