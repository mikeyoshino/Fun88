namespace Fun88.Tests.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Supabase;
using System.Net;
using System.Net.Http;

public sealed class SupabaseStub : IAsyncDisposable
{
    public Client Client { get; }
    private readonly WebApplication _app;

    private SupabaseStub(WebApplication app, Client client)
    {
        _app = app;
        Client = client;
    }

    public static async Task<SupabaseStub> StartAsync()
    {
        HttpClient.DefaultProxy = new WebProxy { BypassProxyOnLocal = true };
        Environment.SetEnvironmentVariable("NO_PROXY", "127.0.0.1,localhost");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapFallback(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("[]");
        });
        await app.StartAsync();

        var url = app.Urls.First();
        var client = new Client(url, "key", new SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false
        });

        return new SupabaseStub(app, client);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
