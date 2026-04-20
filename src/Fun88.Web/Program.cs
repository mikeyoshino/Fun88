using Supabase;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Infrastructure.Clients;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<Supabase.Client>(provider =>
{
    var url = builder.Configuration["Supabase:Url"] ?? string.Empty;
    var key = builder.Configuration["Supabase:Key"] ?? string.Empty;
    var options = new SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = false };
    return new Supabase.Client(url, key, options);
});

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameQueryService, GameQueryService>();
builder.Services.AddScoped<IGameImportPipeline, GameImportPipeline>();
builder.Services.AddScoped<GdEmbedUrlBuilder>();
builder.Services.AddHttpClient<GameDistributionHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GameDistribution:ApiBaseUrl"] ?? "https://api.gamedistribution.com");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["GameDistribution:ApiKey"]}");
});
builder.Services.AddScoped<IGameProvider, GameDistributionProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
