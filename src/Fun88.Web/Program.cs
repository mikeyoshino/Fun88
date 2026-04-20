using Supabase;
using Fun88.Web.Modules.Categories.Repositories;

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
