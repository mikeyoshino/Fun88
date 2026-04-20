using Supabase;
using Fun88.Web.Infrastructure.Clients;
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Middleware;
using Fun88.Web.Modules.Admin.Services;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Shared.Constants;

var builder = WebApplication.CreateBuilder(args);

// Typed options
builder.Services.Configure<GameDistributionOptions>(
    builder.Configuration.GetSection(GameDistributionOptions.Section));
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection(OpenAiOptions.Section));
builder.Services.Configure<AuthCookieOptions>(
    builder.Configuration.GetSection(AuthCookieOptions.Section));

// Supabase client — fail fast if URL or key is missing
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url is required in configuration.");
var supabaseKey = builder.Configuration["Supabase:Key"]
    ?? throw new InvalidOperationException("Supabase:Key is required in configuration.");
builder.Services.AddScoped<Supabase.Client>(_ =>
    new Supabase.Client(supabaseUrl, supabaseKey, new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false
    }));

// Repositories & services
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameQueryService, GameQueryService>();
builder.Services.AddScoped<IGameImportPipeline, GameImportPipeline>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<GdEmbedUrlBuilder>();

// HttpClient for GameDistribution
builder.Services.AddHttpClient<GameDistributionHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GameDistribution:ApiBaseUrl"] ?? "https://api.gamedistribution.com");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["GameDistribution:ApiKey"]}");
});
builder.Services.AddScoped<IGameProvider, GameDistributionProvider>();

// Cookie authentication + AdminOnly policy
var authSection = builder.Configuration.GetSection(AuthCookieOptions.Section);
var schemeName = authSection["AdminSchemeName"] ?? "AdminCookie";
var expiryHours = int.TryParse(authSection["AdminExpiryHours"], out var h) ? h : 8;

builder.Services.AddAuthentication(schemeName)
    .AddCookie(schemeName, o =>
    {
        o.LoginPath = "/admin/auth/login";
        o.ExpireTimeSpan = TimeSpan.FromHours(expiryHours);
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(o =>
    o.AddPolicy(PolicyNames.AdminOnly, p =>
        p.RequireAuthenticatedUser().RequireRole("Admin")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<LanguageResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
