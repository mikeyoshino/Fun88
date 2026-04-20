# Fun88 Plan 1 — Foundation & Core Game Browsing

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Working ASP.NET MVC site where admins can import games from GameDistribution (or upload custom games), and visitors can browse and play them in English and Thai.

**Architecture:** Modular monolith — single `Fun88.Web` MVC project, feature modules under `Modules/`, Supabase C# PostgREST SDK, repository pattern, typed options, zero magic strings.

**Tech Stack:** .NET 8 · ASP.NET MVC · supabase-csharp · xUnit · Moq · HttpClient (typed) · ASP.NET Cookie Auth for session storage

**Out of scope (Plan 2):** OpenAI translation, Quartz scheduling, public user auth (Supabase), favorites, ratings, likes  
**Out of scope (Plan 3):** Blog, AdSense, full SEO

---

## File Map

```
Fun88/
├── Fun88.sln
├── src/Fun88.Web/
│   ├── Fun88.Web.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json          ← gitignored
│   ├── Shared/
│   │   ├── Constants/
│   │   │   ├── RouteNames.cs
│   │   │   ├── PolicyNames.cs
│   │   │   ├── LanguageCode.cs
│   │   │   ├── HttpContextKeys.cs
│   │   │   └── CacheKeys.cs
│   │   └── Extensions/
│   │       └── HttpContextExtensions.cs
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/                   ← EF generated
│   │   ├── Clients/
│   │   │   └── GameDistributionHttpClient.cs
│   │   └── Configuration/
│   │       ├── GameDistributionOptions.cs
│   │       ├── OpenAiOptions.cs              ← define now, wire in Plan 2
│   │       └── AuthCookieOptions.cs
│   ├── Modules/
│   │   ├── Games/
│   │   │   ├── Controllers/
│   │   │   │   ├── HomeController.cs
│   │   │   │   └── GamesController.cs
│   │   │   ├── Services/
│   │   │   │   ├── IGameQueryService.cs
│   │   │   │   └── GameQueryService.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── IGameRepository.cs
│   │   │   │   └── GameRepository.cs
│   │   │   └── ViewModels/
│   │   │       ├── HomeViewModel.cs
│   │   │       ├── GameCardViewModel.cs
│   │   │       └── GameDetailViewModel.cs
│   │   ├── Categories/
│   │   │   ├── Controllers/
│   │   │   │   └── CategoryController.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── ICategoryRepository.cs
│   │   │   │   └── CategoryRepository.cs
│   │   │   └── ViewModels/
│   │   │       └── CategoryViewModel.cs
│   │   ├── Scraper/
│   │   │   ├── Providers/
│   │   │   │   ├── IGameProvider.cs
│   │   │   │   ├── RawGameData.cs
│   │   │   │   ├── GameFetchOptions.cs
│   │   │   │   └── GameDistributionProvider.cs
│   │   │   └── Services/
│   │   │       ├── IGameImportPipeline.cs
│   │   │       ├── ImportGameResult.cs
│   │   │       ├── GameImportPipeline.cs
│   │   │       └── GdEmbedUrlBuilder.cs
│   │   └── Admin/
│   │       ├── Controllers/
│   │       │   ├── AdminAuthController.cs
│   │       │   ├── AdminDashboardController.cs
│   │       │   ├── AdminGamesController.cs
│   │       │   └── AdminCategoriesController.cs
│   │       ├── Services/
│   │       │   ├── IAdminAuthService.cs
│   │       │   └── AdminAuthService.cs
│   │       └── ViewModels/
│   │           ├── AdminLoginViewModel.cs
│   │           ├── AdminGameListItemViewModel.cs
│   │           ├── AdminGameFormViewModel.cs
│   │           └── AdminCategoryFormViewModel.cs
│   ├── Middleware/
│   │   ├── LanguageResolutionMiddleware.cs
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Views/
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml
│   │   │   ├── _GameCard.cshtml
│   │   │   └── Error.cshtml
│   │   ├── Home/Index.cshtml
│   │   ├── Games/
│   │   │   ├── Index.cshtml
│   │   │   └── Detail.cshtml
│   │   └── Category/Index.cshtml
│   ├── Areas/Admin/Views/
│   │   ├── Shared/_AdminLayout.cshtml
│   │   ├── Auth/Login.cshtml
│   │   ├── Dashboard/Index.cshtml
│   │   ├── Games/
│   │   │   ├── Index.cshtml
│   │   │   └── Form.cshtml
│   │   └── Categories/
│   │       ├── Index.cshtml
│   │       └── Form.cshtml
│   └── wwwroot/
│       ├── css/site.css
│       └── js/game-player.js
├── tests/Fun88.Tests/
│   ├── Fun88.Tests.csproj
│   ├── Scraper/
│   │   ├── GdEmbedUrlBuilderTests.cs
│   │   └── GameImportPipelineTests.cs
│   └── Admin/
│       └── AdminAuthServiceTests.cs
└── docker/
    ├── Dockerfile
    └── docker-compose.yml
```

---

### Task 1: Solution + Project Scaffold

**Files:**
- Create: `Fun88.sln`
- Create: `src/Fun88.Web/Fun88.Web.csproj`
- Create: `tests/Fun88.Tests/Fun88.Tests.csproj`
- Create: `.gitignore`

- [ ] **Step 1: Scaffold solution and projects**

```bash
cd /Users/mikeyoshino/gitRepos/Fun88
dotnet new sln -n Fun88
dotnet new mvc -n Fun88.Web -o src/Fun88.Web --no-https false
dotnet new xunit -n Fun88.Tests -o tests/Fun88.Tests
dotnet sln add src/Fun88.Web/Fun88.Web.csproj
dotnet sln add tests/Fun88.Tests/Fun88.Tests.csproj
cd tests/Fun88.Tests && dotnet add reference ../../src/Fun88.Web/Fun88.Web.csproj
```

- [ ] **Step 2: Add NuGet packages to `src/Fun88.Web/Fun88.Web.csproj`**

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.*" />
<PackageReference Include="EFCore.NamingConventions" Version="8.*" />
<PackageReference Include="Microsoft.AspNetCore.Identity.Core" Version="8.*" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.*" />
```

- [ ] **Step 3: Add NuGet packages to `tests/Fun88.Tests/Fun88.Tests.csproj`**

```xml
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.*" />
```

- [ ] **Step 4: Create `.gitignore` at repo root**

Use `dotnet new gitignore` then append:
```
appsettings.Development.json
appsettings.*.json
!appsettings.json
```

- [ ] **Step 5: Create folder skeleton**

```bash
mkdir -p src/Fun88.Web/{Shared/Constants,Shared/Extensions,Infrastructure/{Data/EntityConfigurations,Clients,Configuration},Modules/{Games/{Controllers,Services,Repositories,ViewModels},Categories/{Controllers,Repositories,ViewModels},Scraper/{Providers,Services},Admin/{Controllers,Services,ViewModels}},Middleware,Areas/Admin/Views/{Shared,Auth,Dashboard,Games,Categories},wwwroot/{css,js}}
mkdir -p tests/Fun88.Tests/{Scraper,Admin}
mkdir -p docker
```

- [ ] **Step 6: Verify build is clean**

```bash
cd /Users/mikeyoshino/gitRepos/Fun88
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git init
git add .
git commit -m "chore: initial solution scaffold with Fun88.Web + Fun88.Tests projects"
```

---

### Task 2: Shared Constants

**Files:**
- Create: `src/Fun88.Web/Shared/Constants/RouteNames.cs`
- Create: `src/Fun88.Web/Shared/Constants/PolicyNames.cs`
- Create: `src/Fun88.Web/Shared/Constants/LanguageCode.cs`
- Create: `src/Fun88.Web/Shared/Constants/HttpContextKeys.cs`
- Create: `src/Fun88.Web/Shared/Constants/CacheKeys.cs`

- [ ] **Step 1: Create `RouteNames.cs`**

```csharp
namespace Fun88.Web.Shared.Constants;

public static class RouteNames
{
    public const string Home = nameof(Home);
    public const string GameIndex = nameof(GameIndex);
    public const string GameDetail = nameof(GameDetail);
    public const string GameCategory = nameof(GameCategory);
    public const string GameNewest = nameof(GameNewest);
    public const string GameMostPopular = nameof(GameMostPopular);
    public const string Search = nameof(Search);
    public const string LanguageSet = nameof(LanguageSet);
    public const string AdminLogin = nameof(AdminLogin);
    public const string AdminLogout = nameof(AdminLogout);
    public const string AdminDashboard = nameof(AdminDashboard);
    public const string AdminGameIndex = nameof(AdminGameIndex);
    public const string AdminGameCreate = nameof(AdminGameCreate);
    public const string AdminGameEdit = nameof(AdminGameEdit);
    public const string AdminGameDelete = nameof(AdminGameDelete);
    public const string AdminCategoryIndex = nameof(AdminCategoryIndex);
    public const string AdminCategoryCreate = nameof(AdminCategoryCreate);
    public const string AdminCategoryEdit = nameof(AdminCategoryEdit);
    public const string AdminCategoryDelete = nameof(AdminCategoryDelete);
    public const string AdminScraperTrigger = nameof(AdminScraperTrigger);
}
```

- [ ] **Step 2: Create `PolicyNames.cs`**

```csharp
namespace Fun88.Web.Shared.Constants;

public static class PolicyNames
{
    public const string AdminOnly = nameof(AdminOnly);
}
```

- [ ] **Step 3: Create `LanguageCode.cs`**

```csharp
namespace Fun88.Web.Shared.Constants;

public static class LanguageCode
{
    public const string English = "en";
    public const string Thai = "th";

    public static readonly IReadOnlyList<string> All = [English, Thai];

    public static bool IsValid(string code) => All.Contains(code);
}
```

- [ ] **Step 4: Create `HttpContextKeys.cs`**

```csharp
namespace Fun88.Web.Shared.Constants;

public static class HttpContextKeys
{
    public const string CurrentLanguage = nameof(CurrentLanguage);
}
```

- [ ] **Step 5: Create `CacheKeys.cs`**

```csharp
namespace Fun88.Web.Shared.Constants;

public static class CacheKeys
{
    public const string AllCategories = nameof(AllCategories);
    public const string FeaturedGames = nameof(FeaturedGames);
    public const string NewestGames = nameof(NewestGames);
    public const string MostPopularGames = nameof(MostPopularGames);
}
```

- [ ] **Step 6: Create `HttpContextExtensions.cs`**

```csharp
namespace Fun88.Web.Shared.Extensions;

using Fun88.Web.Shared.Constants;

public static class HttpContextExtensions
{
    public static string GetCurrentLanguage(this HttpContext ctx)
        => ctx.Items.TryGetValue(HttpContextKeys.CurrentLanguage, out var lang) && lang is string s
            ? s
            : LanguageCode.English;
}
```

- [ ] **Step 7: Verify build**

```bash
dotnet build src/Fun88.Web
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add src/Fun88.Web/Shared
git commit -m "feat: add shared constants (RouteNames, PolicyNames, LanguageCode, HttpContextKeys)"
```

---

### Task 3: Typed Configuration (Options)

**Files:**
- Create: `src/Fun88.Web/Infrastructure/Configuration/GameDistributionOptions.cs`
- Create: `src/Fun88.Web/Infrastructure/Configuration/OpenAiOptions.cs`
- Create: `src/Fun88.Web/Infrastructure/Configuration/AuthCookieOptions.cs`
- Modify: `src/Fun88.Web/appsettings.json`
- Create: `src/Fun88.Web/appsettings.Development.json` (gitignored)

- [ ] **Step 1: Create `GameDistributionOptions.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Configuration;

public sealed class GameDistributionOptions
{
    public const string Section = "GameDistribution";

    public string ApiKey { get; init; } = string.Empty;
    public string PublisherId { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.gamedistribution.com";
    public string EmbedBaseUrl { get; init; } = "https://html5.gamedistribution.com";
    public int PageSize { get; init; } = 100;
    public int TimeoutSeconds { get; init; } = 30;
}
```

- [ ] **Step 2: Create `OpenAiOptions.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string Section = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxTokensPerRequest { get; init; } = 1000;
    public int TranslationTimeoutSeconds { get; init; } = 30;
    public bool TranslationEnabled { get; init; } = false;
}
```

- [ ] **Step 3: Create `AuthCookieOptions.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Configuration;

public sealed class AuthCookieOptions
{
    public const string Section = "AuthCookie";

    public string AdminSchemeName { get; init; } = "AdminCookie";
    public int AdminExpiryHours { get; init; } = 8;
    public string GdprConsentCookieName { get; init; } = "gdpr_consent";
    public string LanguageCookieName { get; init; } = "lang";
}
```

- [ ] **Step 4: Write `appsettings.json`** (non-secret defaults only)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GameDistribution": {
    "ApiBaseUrl": "https://api.gamedistribution.com",
    "EmbedBaseUrl": "https://html5.gamedistribution.com",
    "PageSize": 100,
    "TimeoutSeconds": 30
  },
  "OpenAi": {
    "Model": "gpt-4o-mini",
    "MaxTokensPerRequest": 1000,
    "TranslationTimeoutSeconds": 30,
    "TranslationEnabled": false
  },
  "AuthCookie": {
    "AdminSchemeName": "AdminCookie",
    "AdminExpiryHours": 8,
    "GdprConsentCookieName": "gdpr_consent",
    "LanguageCookieName": "lang"
  }
}
```

- [ ] **Step 5: Create `appsettings.Development.json`** (gitignored — secrets here)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=fun88_dev;Username=postgres;Password=YOUR_PW"
  },
  "GameDistribution": {
    "ApiKey": "YOUR_GD_API_KEY",
    "PublisherId": "YOUR_GD_PUBLISHER_ID"
  },
  "OpenAi": {
    "ApiKey": "YOUR_OPENAI_KEY"
  }
}
```

- [ ] **Step 6: Verify build**

```bash
dotnet build src/Fun88.Web
```

- [ ] **Step 7: Commit**

```bash
git add src/Fun88.Web/Infrastructure/Configuration src/Fun88.Web/appsettings.json
git commit -m "feat: add typed options classes (GameDistribution, OpenAi, AuthCookie)"
```

---

### Task 4: Supabase PostgREST Models

**Files:**
- Create: `src/Fun88.Web/Infrastructure/Data/Entities/` (all entity classes)

> All entities live in `Infrastructure/Data/Entities/`. They are plain C# classes — no domain logic.

- [ ] **Step 1: Create `GameProvider.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class GameProvider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Game> Games { get; set; } = [];
}
```

- [ ] **Step 2: Create `Game.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int? ProviderId { get; set; }
    public string? ProviderGameId { get; set; }
    public string GameUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long PlayCount { get; set; }
    public long LikeCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GameProvider? Provider { get; set; }
    public ICollection<GameTranslation> Translations { get; set; } = [];
    public ICollection<GameCategory> GameCategories { get; set; } = [];
}
```

- [ ] **Step 3: Create `GameTranslation.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class GameTranslation
{
    public Guid GameId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ControlDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public Game Game { get; set; } = null!;
}
```

- [ ] **Step 4: Create `Category.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CategoryTranslation> Translations { get; set; } = [];
    public ICollection<GameCategory> GameCategories { get; set; } = [];
}
```

- [ ] **Step 5: Create `CategoryTranslation.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class CategoryTranslation
{
    public int CategoryId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Category Category { get; set; } = null!;
}
```

- [ ] **Step 6: Create `GameCategory.cs`** (join table)

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class GameCategory
{
    public Guid GameId { get; set; }
    public int CategoryId { get; set; }

    public Game Game { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
```

- [ ] **Step 7: Create `AdminUser.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
```

- [ ] **Step 8: Create `ScraperJob.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class ScraperJob
{
    public Guid Id { get; set; }
    public int ProviderId { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;   // "schedule" | "manual"
    public string Status { get; set; } = "pending";           // "pending"|"running"|"completed"|"failed"
    public int GamesFound { get; set; }
    public int GamesImported { get; set; }
    public int GamesSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public GameProvider Provider { get; set; } = null!;
}
```

- [ ] **Step 9: Create `TranslationJob.cs`**

```csharp
namespace Fun88.Web.Infrastructure.Data.Entities;

public class TranslationJob
{
    public Guid GameId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";          // "pending"|"completed"|"failed"
    public short AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Game Game { get; set; } = null!;
}
```

- [ ] **Step 10: Verify build**

```bash
dotnet build src/Fun88.Web
```

- [ ] **Step 11: Commit**

```bash
git add src/Fun88.Web/Infrastructure/Data/Entities
git commit -m "feat: add Supabase PostgREST entity classes"
```

---

### Task 5: Supabase Client Configuration & Schema SQL

**Files:**
- Create: `docs/supabase/schema.sql`

- [ ] **Step 1: Create `docs/supabase/schema.sql`**

```csharp
using Fun88.Web.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fun88.Web.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameTranslation> GameTranslations => Set<GameTranslation>();
    public DbSet<GameProvider> GameProviders => Set<GameProvider>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<GameCategory> GameCategories => Set<GameCategory>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ScraperJob> ScraperJobs => Set<ScraperJob>();
    public DbSet<TranslationJob> TranslationJobs => Set<TranslationJob>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Game>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.ProviderId, x.ProviderGameId }).IsUnique().HasFilter("provider_game_id IS NOT NULL");
            e.HasOne(x => x.Provider).WithMany(p => p.Games).HasForeignKey(x => x.ProviderId).IsRequired(false);
            e.HasMany(x => x.Translations).WithOne(t => t.Game).HasForeignKey(t => t.GameId);
        });

        builder.Entity<GameTranslation>(e =>
        {
            e.HasKey(x => new { x.GameId, x.LanguageCode });
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasMany(x => x.Translations).WithOne(t => t.Category).HasForeignKey(t => t.CategoryId);
        });

        builder.Entity<CategoryTranslation>(e =>
        {
            e.HasKey(x => new { x.CategoryId, x.LanguageCode });
        });

        builder.Entity<GameCategory>(e =>
        {
            e.HasKey(x => new { x.GameId, x.CategoryId });
            e.HasOne(x => x.Game).WithMany(g => g.GameCategories).HasForeignKey(x => x.GameId);
            e.HasOne(x => x.Category).WithMany(c => c.GameCategories).HasForeignKey(x => x.CategoryId);
        });

        builder.Entity<AdminUser>(e => e.HasKey(x => x.Id));

        builder.Entity<ScraperJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId);
        });

        builder.Entity<TranslationJob>(e =>
        {
            e.HasKey(x => new { x.GameId, x.LanguageCode });
            e.HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId);
        });
    }
}
```

- [ ] **Step 2: Register DbContext in `Program.cs`** (minimal — full wiring in Task 24)

Add to `Program.cs` before `app.Build()`:
```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
       .UseSnakeCaseNamingConventions());
```

- [ ] **Step 3: Create initial migration**

```bash
cd src/Fun88.Web
dotnet ef migrations add InitialSchema --output-dir Infrastructure/Data/Migrations
```
Expected: Migration file created in `Infrastructure/Data/Migrations/`

- [ ] **Step 4: Verify migration is valid SQL**

```bash
dotnet ef migrations script
```
Visually confirm tables match the spec schema (games, game_translations, categories, category_translations, game_categories, game_providers, admin_users, scraper_jobs, translation_jobs).

- [ ] **Step 5: Commit**

```bash
git add src/Fun88.Web/Infrastructure/Data src/Fun88.Web/Program.cs
git commit -m "feat: Supabase client injection and schema generation"
```

---

### Task 6: Category Repository

**Files:**
- Create: `src/Fun88.Web/Modules/Categories/Repositories/ICategoryRepository.cs`
- Create: `src/Fun88.Web/Modules/Categories/Repositories/CategoryRepository.cs`

- [ ] **Step 1: Create `ICategoryRepository.cs`**

```csharp
namespace Fun88.Web.Modules.Categories.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create `CategoryRepository.cs`**

Uses EF Core. Eager-loads `Translations` collection. Returns `null` when not found (never throws for missing entities).

```csharp
namespace Fun88.Web.Modules.Categories.Repositories;

using Fun88.Web.Infrastructure.Data;
using Fun88.Web.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct = default)
        => db.Categories
             .Where(c => c.IsActive)
             .Include(c => c.Translations)
             .OrderBy(c => c.SortOrder)
             .ToListAsync(ct)
             .ContinueWith(t => (IReadOnlyList<Category>)t.Result, ct);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => db.Categories
             .Include(c => c.Translations)
             .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Categories.Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
        => await db.Categories.AddAsync(category, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Fun88.Web/Modules/Categories/Repositories
git commit -m "feat: ICategoryRepository + CategoryRepository with Supabase PostgREST"
```

---

### Task 7: Game Repository + GameQueryService

**Files:**
- Create: `src/Fun88.Web/Modules/Games/Repositories/IGameRepository.cs`
- Create: `src/Fun88.Web/Modules/Games/Repositories/GameRepository.cs`
- Create: `src/Fun88.Web/Modules/Games/Services/IGameQueryService.cs`
- Create: `src/Fun88.Web/Modules/Games/Services/GameQueryService.cs`
- Create: `src/Fun88.Web/Modules/Games/ViewModels/GameCardViewModel.cs`
- Create: `src/Fun88.Web/Modules/Games/ViewModels/GameDetailViewModel.cs`
- Create: `src/Fun88.Web/Modules/Games/ViewModels/HomeViewModel.cs`

- [ ] **Step 1: Create `GameCardViewModel.cs`**

```csharp
namespace Fun88.Web.Modules.Games.ViewModels;

public record GameCardViewModel(
    string Slug,
    string Title,
    string ThumbnailUrl,
    long PlayCount,
    bool IsNew,
    bool IsHot,
    bool IsTop
);
```

`IsNew` = created within 7 days. `IsHot` = play_count top 20%. `IsTop` = like_count top 10%. These are computed at query time.

- [ ] **Step 2: Create `GameDetailViewModel.cs`**

```csharp
namespace Fun88.Web.Modules.Games.ViewModels;

public record GameDetailViewModel(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    string? ControlDescription,
    string ThumbnailUrl,
    string EmbedUrl,
    long PlayCount,
    long LikeCount,
    IReadOnlyList<string> CategoryNames,
    IReadOnlyList<GameCardViewModel> RelatedGames
);
```

- [ ] **Step 3: Create `HomeViewModel.cs`**

```csharp
namespace Fun88.Web.Modules.Games.ViewModels;

public record HomeCategorySection(string CategoryName, string CategorySlug, IReadOnlyList<GameCardViewModel> Games);

public record HomeViewModel(
    IReadOnlyList<GameCardViewModel> FeaturedGames,
    IReadOnlyList<GameCardViewModel> NewestGames,
    IReadOnlyList<GameCardViewModel> MostPopularGames,
    IReadOnlyList<HomeCategorySection> CategorySections
);
```

- [ ] **Step 4: Create `IGameRepository.cs`**

```csharp
namespace Fun88.Web.Modules.Games.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;

public interface IGameRepository
{
    Task<Game?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Game?> GetByProviderGameIdAsync(int providerId, string providerGameId, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetNewestAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetMostPopularAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByCategorySlugAsync(string categorySlug, CancellationToken ct = default);
    Task<int> CountAllAsync(CancellationToken ct = default);
    Task AddAsync(Game game, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 5: Create `GameRepository.cs`**

Supabase implementation. Always eager-load `Translations` and `GameCategories.Category.Translations`. For search, use `EF.Functions.ILike` for case-insensitive PostgreSQL full-text match on title and description joined to translations.

Key query pattern:
```csharp
db.Games
  .Where(g => g.IsActive)
  .Include(g => g.Translations)
  .Include(g => g.GameCategories).ThenInclude(gc => gc.Category).ThenInclude(c => c.Translations)
  .OrderByDescending(g => g.CreatedAt)
  .Take(count)
  .ToListAsync(ct)
```

For search:
```csharp
db.Games
  .Where(g => g.IsActive && g.Translations.Any(t =>
      t.LanguageCode == languageCode &&
      (EF.Functions.ILike(t.Title, $"%{query}%") ||
       EF.Functions.ILike(t.Description ?? "", $"%{query}%"))))
```

- [ ] **Step 6: Create `IGameQueryService.cs`**

```csharp
namespace Fun88.Web.Modules.Games.Services;

using Fun88.Web.Modules.Games.ViewModels;

public interface IGameQueryService
{
    Task<HomeViewModel> GetHomeViewModelAsync(string languageCode, CancellationToken ct = default);
    Task<GameDetailViewModel?> GetDetailViewModelAsync(string slug, string languageCode, string embedUrl, CancellationToken ct = default);
    Task<(IReadOnlyList<GameCardViewModel> Games, int TotalCount)> GetCategoryGamesAsync(string categorySlug, string languageCode, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<GameCardViewModel> Games, int TotalCount)> GetAllGamesAsync(string languageCode, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<GameCardViewModel>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default);
}
```

- [ ] **Step 7: Create `GameQueryService.cs`**

Maps `Game` entities to `GameCardViewModel` and `GameDetailViewModel`. Language fallback: if no translation for `languageCode`, fall back to `LanguageCode.English`.

```csharp
private static GameCardViewModel ToCard(Game game, string languageCode)
{
    var t = game.Translations.FirstOrDefault(x => x.LanguageCode == languageCode)
         ?? game.Translations.First(x => x.LanguageCode == LanguageCode.English);

    var isNew = game.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-7);
    var isHot = game.PlayCount > 10_000;
    var isTop = game.LikeCount > 1_000;

    return new GameCardViewModel(game.Slug, t.Title, game.ThumbnailUrl, game.PlayCount, isNew, isHot, isTop);
}
```

- [ ] **Step 8: Write failing test for `GameQueryService`**

`tests/Fun88.Tests/Games/GameQueryServiceTests.cs`:

```csharp
[Fact]
public async Task GetHomeViewModel_FallsBackToEnglish_WhenThaiTranslationMissing()
{
    // Arrange: game with only EN translation, request TH
    var repo = new Mock<IGameRepository>();
    var catRepo = new Mock<ICategoryRepository>();
    // set up repo to return game with EN translation only
    // ...
    var svc = new GameQueryService(repo.Object, catRepo.Object);

    // Act
    var vm = await svc.GetHomeViewModelAsync(LanguageCode.Thai);

    // Assert
    Assert.NotEmpty(vm.NewestGames);
    Assert.Equal("Test Game EN", vm.NewestGames[0].Title);
}
```

- [ ] **Step 9: Run test — expect fail**

```bash
dotnet test tests/Fun88.Tests --filter "GameQueryServiceTests"
```
Expected: FAIL (GameQueryService not implemented yet)

- [ ] **Step 10: Implement `GameQueryService.cs`** fully (all methods)

- [ ] **Step 11: Run test — expect pass**

```bash
dotnet test tests/Fun88.Tests --filter "GameQueryServiceTests"
```
Expected: PASS

- [ ] **Step 12: Commit**

```bash
git add src/Fun88.Web/Modules/Games tests/Fun88.Tests/Games
git commit -m "feat: IGameRepository + GameRepository + IGameQueryService + GameQueryService with language fallback"
```

---

### Task 8: IGameProvider + RawGameData + GameDistributionHttpClient

**Files:**
- Create: `src/Fun88.Web/Modules/Scraper/Providers/IGameProvider.cs`
- Create: `src/Fun88.Web/Modules/Scraper/Providers/RawGameData.cs`
- Create: `src/Fun88.Web/Modules/Scraper/Providers/GameFetchOptions.cs`
- Create: `src/Fun88.Web/Infrastructure/Clients/GameDistributionHttpClient.cs`

- [ ] **Step 1: Create `IGameProvider.cs`**

```csharp
namespace Fun88.Web.Modules.Scraper.Providers;

public interface IGameProvider
{
    string ProviderSlug { get; }
    Task<IReadOnlyList<RawGameData>> FetchGamesAsync(GameFetchOptions options, CancellationToken ct = default);
    Task<RawGameData?> FetchGameByIdAsync(string providerGameId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create `RawGameData.cs`**

```csharp
namespace Fun88.Web.Modules.Scraper.Providers;

public record RawGameData(
    string ProviderGameId,
    string Title,
    string Description,
    string ControlDescription,
    string ThumbnailUrl,
    string GameUrl,
    IReadOnlyList<string> CategorySlugs
);
```

- [ ] **Step 3: Create `GameFetchOptions.cs`**

```csharp
namespace Fun88.Web.Modules.Scraper.Providers;

public record GameFetchOptions(
    int Page = 1,
    int PageSize = 100,
    string? CategoryFilter = null
);
```

- [ ] **Step 4: Create `GameDistributionHttpClient.cs`**

Typed HttpClient wrapping GD Publisher API. Deserializes GD JSON response into `RawGameData`. Auth via `Authorization: Bearer {ApiKey}` header.

> **Note to implementer:** Verify the exact GD Publisher API endpoint path from your GD dashboard. The assumed endpoint is `GET /publisher/{publisherId}/games?page={n}&per_page={size}`. If different, only `GameDistributionHttpClient` needs updating.

```csharp
namespace Fun88.Web.Infrastructure.Clients;

using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Modules.Scraper.Providers;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

public class GameDistributionHttpClient(HttpClient http, IOptions<GameDistributionOptions> options)
{
    private readonly GameDistributionOptions _opts = options.Value;

    public async Task<IReadOnlyList<RawGameData>> GetGamesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = $"/publisher/{_opts.PublisherId}/games?page={page}&per_page={pageSize}";
        var response = await http.GetFromJsonAsync<GdGameListResponse>(url, ct)
            ?? throw new InvalidOperationException("GD API returned null response");

        return response.Data.Select(MapToRawGameData).ToList();
    }

    public async Task<RawGameData?> GetGameByIdAsync(string providerGameId, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<GdGameResponse>($"/publisher/game/{providerGameId}", ct);
        return response is null ? null : MapToRawGameData(response.Data);
    }

    private static RawGameData MapToRawGameData(GdGameDto dto) => new(
        dto.Md5,
        dto.Title,
        dto.Description ?? string.Empty,
        dto.Instructions ?? string.Empty,
        dto.Thumb ?? string.Empty,
        $"https://html5.gamedistribution.com/{dto.Md5}/",
        dto.Tags?.Select(t => t.Slug).ToList() ?? []
    );

    // GD API response DTOs — internal to this client only
    private record GdGameListResponse(IReadOnlyList<GdGameDto> Data);
    private record GdGameResponse(GdGameDto Data);
    private record GdGameDto(string Md5, string Title, string? Description, string? Instructions, string? Thumb, IReadOnlyList<GdTagDto>? Tags);
    private record GdTagDto(string Slug, string Name);
}
```

- [ ] **Step 5: Commit**

```bash
git add src/Fun88.Web/Modules/Scraper/Providers src/Fun88.Web/Infrastructure/Clients
git commit -m "feat: IGameProvider interface, RawGameData, GameDistributionHttpClient typed client"
```

---

### Task 9: GameDistributionProvider

**Files:**
- Create: `src/Fun88.Web/Modules/Scraper/Providers/GameDistributionProvider.cs`

- [ ] **Step 1: Create `GameDistributionProvider.cs`**

Implements `IGameProvider`. Delegates to `GameDistributionHttpClient`. `ProviderSlug = "game-distribution"` — must match the `slug` value in the `game_providers` DB row (seeded in Task 26).

```csharp
namespace Fun88.Web.Modules.Scraper.Providers;

using Fun88.Web.Infrastructure.Clients;

public class GameDistributionProvider(GameDistributionHttpClient client) : IGameProvider
{
    public string ProviderSlug => "game-distribution";

    public Task<IReadOnlyList<RawGameData>> FetchGamesAsync(GameFetchOptions options, CancellationToken ct = default)
        => client.GetGamesAsync(options.Page, options.PageSize, ct);

    public Task<RawGameData?> FetchGameByIdAsync(string providerGameId, CancellationToken ct = default)
        => client.GetGameByIdAsync(providerGameId, ct);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Fun88.Web/Modules/Scraper/Providers/GameDistributionProvider.cs
git commit -m "feat: GameDistributionProvider implementing IGameProvider"
```

---

### Task 10: GdEmbedUrlBuilder

**Files:**
- Create: `src/Fun88.Web/Modules/Scraper/Services/GdEmbedUrlBuilder.cs`
- Create: `tests/Fun88.Tests/Scraper/GdEmbedUrlBuilderTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/Fun88.Tests/Scraper/GdEmbedUrlBuilderTests.cs`:

```csharp
public class GdEmbedUrlBuilderTests
{
    private readonly GdEmbedUrlBuilder _builder;

    public GdEmbedUrlBuilderTests()
    {
        var opts = Options.Create(new GameDistributionOptions { EmbedBaseUrl = "https://html5.gamedistribution.com" });
        _builder = new GdEmbedUrlBuilder(opts);
    }

    [Fact]
    public void Build_WithAllConsent_ReturnsCorrectUrl()
    {
        var url = _builder.Build("abc123", "https://fun88.com/games/test-game", tracking: 1, targeting: 1, thirdParty: 1);

        Assert.Equal(
            "https://html5.gamedistribution.com/abc123/?gd_sdk_referrer_url=https%3A%2F%2Ffun88.com%2Fgames%2Ftest-game&gdpr-tracking=1&gdpr-targeting=1&gdpr-third-party=1",
            url);
    }

    [Fact]
    public void Build_WithNoConsent_AllFlagsZero()
    {
        var url = _builder.Build("abc123", "https://fun88.com/games/test-game", tracking: 0, targeting: 0, thirdParty: 0);

        Assert.Contains("gdpr-tracking=0", url);
        Assert.Contains("gdpr-targeting=0", url);
        Assert.Contains("gdpr-third-party=0", url);
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

```bash
dotnet test tests/Fun88.Tests --filter "GdEmbedUrlBuilderTests"
```
Expected: FAIL (class doesn't exist yet)

- [ ] **Step 3: Create `GdEmbedUrlBuilder.cs`**

```csharp
namespace Fun88.Web.Modules.Scraper.Services;

using Fun88.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

public class GdEmbedUrlBuilder(IOptions<GameDistributionOptions> options)
{
    private readonly string _baseUrl = options.Value.EmbedBaseUrl;

    public string Build(string providerGameId, string referrerUrl, int tracking, int targeting, int thirdParty)
    {
        var encoded = Uri.EscapeDataString(referrerUrl);
        return $"{_baseUrl}/{providerGameId}/?gd_sdk_referrer_url={encoded}&gdpr-tracking={tracking}&gdpr-targeting={targeting}&gdpr-third-party={thirdParty}";
    }
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/Fun88.Tests --filter "GdEmbedUrlBuilderTests"
```
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Fun88.Web/Modules/Scraper/Services/GdEmbedUrlBuilder.cs tests/Fun88.Tests/Scraper/GdEmbedUrlBuilderTests.cs
git commit -m "feat: GdEmbedUrlBuilder with GDPR consent params + tests"
```

---

### Task 11: IGameImportPipeline + GameImportPipeline

**Files:**
- Create: `src/Fun88.Web/Modules/Scraper/Services/IGameImportPipeline.cs`
- Create: `src/Fun88.Web/Modules/Scraper/Services/ImportGameResult.cs`
- Create: `src/Fun88.Web/Modules/Scraper/Services/GameImportPipeline.cs`
- Create: `tests/Fun88.Tests/Scraper/GameImportPipelineTests.cs`

- [ ] **Step 1: Create `ImportGameResult.cs`**

```csharp
namespace Fun88.Web.Modules.Scraper.Services;

public record ImportGameResult(bool Imported, bool Skipped, string? Error);
```

- [ ] **Step 2: Create `IGameImportPipeline.cs`**

```csharp
namespace Fun88.Web.Modules.Scraper.Services;

using Fun88.Web.Modules.Scraper.Providers;

public interface IGameImportPipeline
{
    /// <summary>Import a single game from a provider. Upserts if already exists.</summary>
    Task<ImportGameResult> ImportAsync(RawGameData raw, int providerId, CancellationToken ct = default);

    /// <summary>Import a custom (own) game uploaded by admin. Errors on slug collision.</summary>
    Task<ImportGameResult> ImportCustomAsync(CustomGameData custom, CancellationToken ct = default);
}

public record CustomGameData(
    string Title,
    string Description,
    string ControlDescription,
    string Slug,
    string GameUrl,
    string ThumbnailUrl,
    IReadOnlyList<int> CategoryIds,
    bool AutoTranslate
);
```

- [ ] **Step 3: Write failing tests**

`tests/Fun88.Tests/Scraper/GameImportPipelineTests.cs`:

```csharp
public class GameImportPipelineTests
{
    [Fact]
    public async Task ImportAsync_NewGame_ReturnsImported()
    {
        var repo = new Mock<IGameRepository>();
        repo.Setup(r => r.GetByProviderGameIdAsync(1, "gd-001", default)).ReturnsAsync((Game?)null);

        var catRepo = new Mock<ICategoryRepository>();
        var pipeline = new GameImportPipeline(repo.Object, catRepo.Object, NullLogger<GameImportPipeline>.Instance);

        var raw = new RawGameData("gd-001", "Test Game", "Desc", "Controls", "http://thumb.jpg", "http://game.url", []);
        var result = await pipeline.ImportAsync(raw, 1);

        Assert.True(result.Imported);
        Assert.False(result.Skipped);
        repo.Verify(r => r.AddAsync(It.IsAny<Game>(), default), Times.Once);
    }

    [Fact]
    public async Task ImportCustomAsync_SlugCollision_ReturnsError()
    {
        var repo = new Mock<IGameRepository>();
        repo.Setup(r => r.GetBySlugAsync("existing-slug", default)).ReturnsAsync(new Game { Slug = "existing-slug" });

        var catRepo = new Mock<ICategoryRepository>();
        var pipeline = new GameImportPipeline(repo.Object, catRepo.Object, NullLogger<GameImportPipeline>.Instance);

        var custom = new CustomGameData("Title", "Desc", "Controls", "existing-slug", "http://url", "http://thumb", [], false);
        var result = await pipeline.ImportCustomAsync(custom);

        Assert.False(result.Imported);
        Assert.NotNull(result.Error);
    }
}
```

- [ ] **Step 4: Run tests — expect fail**

```bash
dotnet test tests/Fun88.Tests --filter "GameImportPipelineTests"
```

- [ ] **Step 5: Create `GameImportPipeline.cs`**

Pipeline steps:
1. For GD games: check `GetByProviderGameIdAsync`. If exists, update metadata fields (title, description, thumbnail) then save and return `Skipped=false, Imported=true` (upsert). 
2. For custom games: check `GetBySlugAsync`. If exists, return error.
3. Generate slug from title if not provided (for GD games: use `providerGameId` slugified).
4. Create `Game` + `GameTranslation` (English) entities.
5. Assign categories from `CategorySlugs` (look up by slug, skip unknown categories silently).
6. Call `AddAsync` + `SaveChangesAsync`.
7. Translation job enqueue is a no-op in Plan 1 (stubbed — log a warning).

- [ ] **Step 6: Run tests — expect pass**

```bash
dotnet test tests/Fun88.Tests --filter "GameImportPipelineTests"
```

- [ ] **Step 7: Commit**

```bash
git add src/Fun88.Web/Modules/Scraper/Services tests/Fun88.Tests/Scraper/GameImportPipelineTests.cs
git commit -m "feat: IGameImportPipeline + GameImportPipeline with upsert for GD and slug-collision guard for custom games"
```

---

### Task 12: Admin Auth Service + Cookie Scheme

**Files:**
- Create: `src/Fun88.Web/Modules/Admin/Services/IAdminAuthService.cs`
- Create: `src/Fun88.Web/Modules/Admin/Services/AdminAuthService.cs`
- Create: `tests/Fun88.Tests/Admin/AdminAuthServiceTests.cs`

- [ ] **Step 1: Create `IAdminAuthService.cs`**

```csharp
namespace Fun88.Web.Modules.Admin.Services;

using Fun88.Web.Infrastructure.Data.Entities;

public interface IAdminAuthService
{
    Task<AdminUser?> ValidateAsync(string email, string password, CancellationToken ct = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] **Step 2: Write failing tests**

```csharp
public class AdminAuthServiceTests
{
    [Fact]
    public async Task ValidateAsync_CorrectPassword_ReturnsUser()
    {
        // Arrange: admin user with hashed password in in-memory DB
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("AdminAuthTest").Options;
        using var db = new AppDbContext(options);
        var hasher = new PasswordHasher<AdminUser>();
        var admin = new AdminUser { Id = Guid.NewGuid(), Email = "admin@fun88.com", CreatedAt = DateTimeOffset.UtcNow };
        admin.PasswordHash = hasher.HashPassword(admin, "SecurePass123!");
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        var svc = new AdminAuthService(db);

        // Act
        var result = await svc.ValidateAsync("admin@fun88.com", "SecurePass123!");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(admin.Email, result.Email);
    }

    [Fact]
    public async Task ValidateAsync_WrongPassword_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("AdminAuthTest2").Options;
        using var db = new AppDbContext(options);
        var hasher = new PasswordHasher<AdminUser>();
        var admin = new AdminUser { Id = Guid.NewGuid(), Email = "admin@fun88.com", CreatedAt = DateTimeOffset.UtcNow };
        admin.PasswordHash = hasher.HashPassword(admin, "CorrectPassword");
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        var svc = new AdminAuthService(db);

        var result = await svc.ValidateAsync("admin@fun88.com", "WrongPassword");

        Assert.Null(result);
    }
}
```

- [ ] **Step 3: Run tests — expect fail**

```bash
dotnet test tests/Fun88.Tests --filter "AdminAuthServiceTests"
```

- [ ] **Step 4: Create `AdminAuthService.cs`**

Uses `IPasswordHasher<AdminUser>` (ASP.NET Identity). Look up by email (case-insensitive), verify hash, update `LastLoginAt` on success.

```csharp
public class AdminAuthService(AppDbContext db) : IAdminAuthService
{
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public async Task<AdminUser?> ValidateAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
        if (user is null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
}
```

- [ ] **Step 5: Run tests — expect pass**

```bash
dotnet test tests/Fun88.Tests --filter "AdminAuthServiceTests"
```

- [ ] **Step 6: Commit**

```bash
git add src/Fun88.Web/Modules/Admin/Services tests/Fun88.Tests/Admin
git commit -m "feat: IAdminAuthService + AdminAuthService with Supabase SDK Auth"
```

---

### Task 13: Admin Auth Controller + Login View

**Files:**
- Create: `src/Fun88.Web/Modules/Admin/Controllers/AdminAuthController.cs`
- Create: `src/Fun88.Web/Modules/Admin/ViewModels/AdminLoginViewModel.cs`
- Create: `src/Fun88.Web/Areas/Admin/Views/Auth/Login.cshtml`

- [ ] **Step 1: Create `AdminLoginViewModel.cs`**

```csharp
namespace Fun88.Web.Modules.Admin.ViewModels;

using System.ComponentModel.DataAnnotations;

public class AdminLoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
```

- [ ] **Step 2: Create `AdminAuthController.cs`**

`[Area("Admin")]`. Routes: `GET /admin/auth/login` → `Login()`, `POST /admin/auth/login` → `LoginPost()`, `POST /admin/auth/logout` → `Logout()`.

On successful login: build `ClaimsPrincipal` with `NameIdentifier` (admin id) and `Role = "Admin"` claims, sign in with `AuthCookieOptions.AdminSchemeName`, redirect to `ReturnUrl` or `/admin`.

On failure: add `ErrorMessage` to viewmodel, return view.

`[ValidateAntiForgeryToken]` on all POST actions.

- [ ] **Step 3: Create `Areas/Admin/Views/Auth/Login.cshtml`**

Simple login form targeting `AdminAuth/Login` POST. Includes `@Html.AntiForgeryToken()`. Show `Model.ErrorMessage` if set. Dark admin theme (plain, functional — no CrazyGames chrome).

- [ ] **Step 4: Verify admin login works manually**

```bash
cd src/Fun88.Web && dotnet run
```
Navigate to `http://localhost:5000/admin/auth/login` — form renders without errors. (Can't fully test without DB seed — that's Task 26.)

- [ ] **Step 5: Commit**

```bash
git add src/Fun88.Web/Modules/Admin/Controllers/AdminAuthController.cs src/Fun88.Web/Modules/Admin/ViewModels/AdminLoginViewModel.cs src/Fun88.Web/Areas/Admin/Views
git commit -m "feat: admin login/logout with HttpOnly cookie auth"
```

---

### Task 14: Admin Dashboard + _AdminLayout

**Files:**
- Create: `src/Fun88.Web/Modules/Admin/Controllers/AdminDashboardController.cs`
- Create: `src/Fun88.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`
- Create: `src/Fun88.Web/Areas/Admin/Views/Dashboard/Index.cshtml`

- [ ] **Step 1: Create `AdminDashboardController.cs`**

`[Area("Admin")]`, `[Authorize(Policy = PolicyNames.AdminOnly)]`. Single `GET /admin` action returning basic stats ViewModel (game count, active categories count, pending translation jobs count).

- [ ] **Step 2: Create `_AdminLayout.cshtml`**

Dark sidebar admin layout. Sidebar links: Dashboard, Games, Categories, Scraper (placeholder for Plan 2), Translations (placeholder). Nav bar with "Logged in as {adminName}" and logout button.

- [ ] **Step 3: Create `Dashboard/Index.cshtml`**

Stats cards: Total Games, Total Categories, Pending Translations. Link to trigger scraper (placeholder until Plan 2).

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Modules/Admin/Controllers/AdminDashboardController.cs src/Fun88.Web/Areas/Admin/Views/Shared src/Fun88.Web/Areas/Admin/Views/Dashboard
git commit -m "feat: admin dashboard with stats and admin layout"
```

---

### Task 15: Admin Games Controller + Views

**Files:**
- Create: `src/Fun88.Web/Modules/Admin/Controllers/AdminGamesController.cs`
- Create: `src/Fun88.Web/Modules/Admin/ViewModels/AdminGameListItemViewModel.cs`
- Create: `src/Fun88.Web/Modules/Admin/ViewModels/AdminGameFormViewModel.cs`
- Create: `src/Fun88.Web/Areas/Admin/Views/Games/Index.cshtml`
- Create: `src/Fun88.Web/Areas/Admin/Views/Games/Form.cshtml`

- [ ] **Step 1: Create `AdminGameFormViewModel.cs`**

```csharp
namespace Fun88.Web.Modules.Admin.ViewModels;

public class AdminGameFormViewModel
{
    public Guid? Id { get; set; }

    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ControlDescription { get; set; }

    [Required] public string Slug { get; set; } = string.Empty;
    [Required] public string GameUrl { get; set; } = string.Empty;
    [Required] public string ThumbnailUrl { get; set; } = string.Empty;

    public List<int> SelectedCategoryIds { get; set; } = [];
    public List<SelectListItem> AllCategories { get; set; } = [];
    public bool AutoTranslate { get; set; } = true;
    public bool IsCustomGame => Id is null || !IsGdGame;
    public bool IsGdGame { get; set; }
}
```

- [ ] **Step 2: Create `AdminGamesController.cs`**

`[Area("Admin")]`, `[Authorize(Policy = PolicyNames.AdminOnly)]`.

Actions:
- `GET /admin/games` — paginated list with search filter
- `GET /admin/games/create` — blank form (custom game only)
- `POST /admin/games/create` — calls `IGameImportPipeline.ImportCustomAsync`, redirects on success
- `GET /admin/games/edit/{id}` — pre-filled form (custom games only — GD games show read-only fields)
- `POST /admin/games/edit/{id}` — update English translation + categories
- `POST /admin/games/delete/{id}` — soft delete (`IsActive = false`)
- `POST /admin/games/trigger-import` — triggers GD import for all pages (loops `IGameProvider.FetchGamesAsync`, calls `IGameImportPipeline.ImportAsync` for each, returns JSON with counts for AJAX)

All POST actions: `[ValidateAntiForgeryToken]`.

- [ ] **Step 3: Create `Areas/Admin/Views/Games/Index.cshtml`**

Table: Thumbnail | Title | Provider | Categories | Status | Actions (Edit / Delete). Search bar at top. "Import from GameDistribution" button that posts to `/admin/games/trigger-import` with JS progress feedback.

- [ ] **Step 4: Create `Areas/Admin/Views/Games/Form.cshtml`**

Fields: Title, Slug (auto-generated from title via JS), Description, Control Description, Game URL, Thumbnail URL, Categories (multi-select), Auto-translate checkbox. For GD games: show provider fields as read-only.

- [ ] **Step 5: Commit**

```bash
git add src/Fun88.Web/Modules/Admin/Controllers/AdminGamesController.cs src/Fun88.Web/Modules/Admin/ViewModels/AdminGame* src/Fun88.Web/Areas/Admin/Views/Games
git commit -m "feat: admin games CRUD with custom game form and GD import trigger"
```

---

### Task 16: Admin Categories Controller + Views

**Files:**
- Create: `src/Fun88.Web/Modules/Admin/Controllers/AdminCategoriesController.cs`
- Create: `src/Fun88.Web/Modules/Admin/ViewModels/AdminCategoryFormViewModel.cs`
- Create: `src/Fun88.Web/Areas/Admin/Views/Categories/Index.cshtml`
- Create: `src/Fun88.Web/Areas/Admin/Views/Categories/Form.cshtml`

- [ ] **Step 1: Create `AdminCategoryFormViewModel.cs`**

Fields: `Id` (nullable int), `Slug`, `Icon`, `SortOrder`, `IsActive`, `NameEn`, `NameTh`.

- [ ] **Step 2: Create `AdminCategoriesController.cs`**

`[Area("Admin")]`, `[Authorize(Policy = PolicyNames.AdminOnly)]`. Standard CRUD: List, Create GET/POST, Edit GET/POST, Delete POST (soft delete). On save: upsert both `CategoryTranslation` rows (`en` and `th`).

- [ ] **Step 3: Create Category views**

Index: table of categories with sort order drag-handle (static for now), name, slug, active toggle, edit/delete links. Form: slug, icon name, sort order, name EN, name TH, active checkbox.

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Modules/Admin/Controllers/AdminCategoriesController.cs src/Fun88.Web/Modules/Admin/ViewModels/AdminCategoryFormViewModel.cs src/Fun88.Web/Areas/Admin/Views/Categories
git commit -m "feat: admin categories CRUD with bilingual name management"
```

---

### Task 17: Language Resolution Middleware

**Files:**
- Create: `src/Fun88.Web/Middleware/LanguageResolutionMiddleware.cs`

- [ ] **Step 1: Create `LanguageResolutionMiddleware.cs`**

Resolution order:
1. If user authenticated: load `preferred_language` from DB (Plan 2 — skip for now, users table not in Plan 1)
2. Cookie named `AuthCookieOptions.LanguageCookieName`
3. `Accept-Language` header (first valid code)
4. Default `LanguageCode.English`

Store result in `HttpContext.Items[HttpContextKeys.CurrentLanguage]`.

```csharp
namespace Fun88.Web.Middleware;

using Fun88.Web.Shared.Constants;
using Fun88.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

public class LanguageResolutionMiddleware(RequestDelegate next, IOptions<AuthCookieOptions> cookieOpts)
{
    private readonly string _langCookieName = cookieOpts.Value.LanguageCookieName;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var lang = ResolveLang(ctx);
        ctx.Items[HttpContextKeys.CurrentLanguage] = lang;

        // Set language cookie for persistence if it changed
        if (!ctx.Request.Cookies.ContainsKey(_langCookieName))
            ctx.Response.Cookies.Append(_langCookieName, lang, new CookieOptions { MaxAge = TimeSpan.FromDays(365), SameSite = SameSiteMode.Strict, HttpOnly = true });

        await next(ctx);
    }

    private string ResolveLang(HttpContext ctx)
    {
        if (ctx.Request.Cookies.TryGetValue(_langCookieName, out var cookie) && LanguageCode.IsValid(cookie))
            return cookie;

        var acceptLang = ctx.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(acceptLang))
        {
            var first = acceptLang.Split(',')[0].Trim().Split(';')[0].Trim().ToLowerInvariant();
            var code = first.StartsWith("th") ? LanguageCode.Thai : LanguageCode.English;
            return code;
        }

        return LanguageCode.English;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Fun88.Web/Middleware/LanguageResolutionMiddleware.cs
git commit -m "feat: LanguageResolutionMiddleware resolves lang from cookie → Accept-Language → default EN"
```

---

### Task 18: Security Headers Middleware

**Files:**
- Create: `src/Fun88.Web/Middleware/SecurityHeadersMiddleware.cs`

- [ ] **Step 1: Create `SecurityHeadersMiddleware.cs`**

```csharp
namespace Fun88.Web.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' https://pagead2.googlesyndication.com; " +
            "frame-src 'self' https://html5.gamedistribution.com; " +
            "img-src 'self' data: https:; " +
            "style-src 'self' 'unsafe-inline';";

        await next(ctx);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Fun88.Web/Middleware/SecurityHeadersMiddleware.cs
git commit -m "feat: SecurityHeadersMiddleware (CSP, X-Frame-Options, nosniff, Referrer-Policy)"
```

---

### Task 19: _Layout.cshtml + site.css

**Files:**
- Create: `src/Fun88.Web/Views/Shared/_Layout.cshtml`
- Create: `src/Fun88.Web/Views/Shared/_GameCard.cshtml`
- Modify: `src/Fun88.Web/wwwroot/css/site.css`

- [ ] **Step 1: Write `site.css`**

Port CSS from the approved mockup at `docs/superpowers/specs/ui/2026-04-20-fun88-ui-mockup.html`. Key sections to extract:
- CSS custom properties (design tokens: `--bg-base`, `--bg-surface`, `--accent`, etc.)
- `.nav` full-width 52px bar styles
- `.sidebar` collapsed (60px) / hover expanded (230px) styles
- `.sidebar svg { stroke-width: 2.5 }`
- `.nav-item .label { opacity: 0; transition: opacity 0.18s; }` + `.sidebar:hover .nav-item .label { opacity: 1; }`
- `.game-card` 180×135px thumbnail styles + badge overlay
- `.featured-card` 280×180px styles
- Category chip row, horizontal scroll row, section headers

Read the mockup file to copy the CSS verbatim:
```bash
cat docs/superpowers/specs/ui/2026-04-20-fun88-ui-mockup.html
```
Copy the `<style>` block content into `site.css`.

- [ ] **Step 2: Write `_Layout.cshtml`**

Structure:
```html
<!DOCTYPE html>
<html lang="@currentLang">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>@ViewData["Title"] - Fun88</title>
  <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
  <nav class="nav"><!-- logo, search, icons, login button --></nav>
  <div class="app-body">
    <aside class="sidebar"><!-- Heroicon nav items with labels --></aside>
    <main class="main-content">@RenderBody()</main>
  </div>
  <script src="~/js/site.js"></script>
  @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Use `HttpContext.GetCurrentLanguage()` for `currentLang`. Sidebar items use inline Heroicon SVGs matching the mockup.

- [ ] **Step 3: Write `_GameCard.cshtml`** partial

```cshtml
@model Fun88.Web.Modules.Games.ViewModels.GameCardViewModel
<div class="game-card">
  <a asp-action="Detail" asp-controller="Games" asp-route-slug="@Model.Slug">
    <div class="card-thumb">
      <img src="@Model.ThumbnailUrl" alt="@Model.Title" loading="lazy" />
      @if (Model.IsNew) { <span class="badge badge-new">NEW</span> }
      @if (Model.IsHot) { <span class="badge badge-hot">HOT</span> }
      @if (Model.IsTop) { <span class="badge badge-top">TOP</span> }
    </div>
    <p class="card-title">@Model.Title</p>
  </a>
</div>
```

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Views/Shared src/Fun88.Web/wwwroot/css/site.css
git commit -m "feat: shared layout with CrazyGames-inspired nav + sidebar + game card partial"
```

---

### Task 20: HomeController + Home View

**Files:**
- Create: `src/Fun88.Web/Modules/Games/Controllers/HomeController.cs`
- Create: `src/Fun88.Web/Views/Home/Index.cshtml`

- [ ] **Step 1: Create `HomeController.cs`**

```csharp
namespace Fun88.Web.Modules.Games.Controllers;

public class HomeController(IGameQueryService gameQuery) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var lang = HttpContext.GetCurrentLanguage();
        var vm = await gameQuery.GetHomeViewModelAsync(lang, ct);
        return View(vm);
    }
}
```

Route: registered as default route `{controller=Home}/{action=Index}/{id?}`.

- [ ] **Step 2: Create `Views/Home/Index.cshtml`**

Sections:
1. Category chip row (link to `/games/category/{slug}`)
2. "Top Picks" featured row — `<partial name="_GameCard" model="game" />` with `featured-card` class
3. "New Games" horizontal scroll row — standard cards
4. "Most Popular" horizontal scroll row
5. Per-category rows (from `vm.CategorySections`)

Each row has section header + "View more →" link and uses `overflow-x: auto` scroll container.

- [ ] **Step 3: Commit**

```bash
git add src/Fun88.Web/Modules/Games/Controllers/HomeController.cs src/Fun88.Web/Views/Home
git commit -m "feat: home page with featured, newest, popular, and per-category game rows"
```

---

### Task 21: GamesController (Index + Detail) + Views

**Files:**
- Create: `src/Fun88.Web/Modules/Games/Controllers/GamesController.cs`
- Create: `src/Fun88.Web/Views/Games/Index.cshtml`
- Create: `src/Fun88.Web/Views/Games/Detail.cshtml`

- [ ] **Step 1: Create `GamesController.cs`**

Actions:
- `GET /games` — all games paginated (`page` query param, default page size 24)
- `GET /games/{slug}` — game detail. Reads GDPR consent cookie (`gdpr_consent`) to build embed URL via `GdEmbedUrlBuilder`. For custom games (`ProviderId == null`), embed URL is `game.GameUrl` directly.
- `GET /games/newest` — newest games list
- `GET /games/most-popular` — most popular list
- `GET /search?q={query}` — search

GDPR consent reading: parse `gdpr_consent` cookie value (format: `tracking=1&targeting=0&third_party=0`). Default all flags to `0` if cookie absent.

- [ ] **Step 2: Create `Views/Games/Index.cshtml`**

Page header: "All Games". Sort links (Newest / Most Popular). 6-column game grid (CSS grid, `repeat(6, 1fr)`). Pagination at bottom.

- [ ] **Step 3: Create `Views/Games/Detail.cshtml`**

Three-column layout:
- Left sidebar: nav (from shared layout — no extra sidebar needed here, or a simplified one)
- Center: game iframe + title, rating placeholder, play count, categories, description, controls, "More Like This" row
- Right: AdSense placeholder slot (`<!-- AdSense Plan 3 -->`) + recommended games list

Game iframe:
```html
<div class="game-frame-wrapper" id="game-container">
  <div class="loading-overlay" id="loading-overlay">Loading...</div>
  <iframe src="@Model.EmbedUrl" id="game-iframe" allowfullscreen frameborder="0"></iframe>
</div>
```

Include `game-player.js` via `@section Scripts`.

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Modules/Games/Controllers/GamesController.cs src/Fun88.Web/Views/Games
git commit -m "feat: games index + detail page with GDPR-aware GD embed URL"
```

---

### Task 22: CategoryController + Category View

**Files:**
- Create: `src/Fun88.Web/Modules/Categories/Controllers/CategoryController.cs`
- Create: `src/Fun88.Web/Modules/Categories/ViewModels/CategoryViewModel.cs`
- Create: `src/Fun88.Web/Views/Category/Index.cshtml`

- [ ] **Step 1: Create `CategoryViewModel.cs`**

```csharp
public record CategoryViewModel(
    string Slug,
    string Name,
    IReadOnlyList<GameCardViewModel> Games,
    int TotalCount,
    int CurrentPage,
    int PageSize
);
```

- [ ] **Step 2: Create `CategoryController.cs`**

`GET /games/category/{slug}?page=1&sort=popular`

- [ ] **Step 3: Create `Views/Category/Index.cshtml`**

Breadcrumb, category title, sort dropdown, 6-column dense grid, pagination. Matches the spec's category page description.

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Modules/Categories src/Fun88.Web/Views/Category
git commit -m "feat: category page with paginated game grid and sort options"
```

---

### Task 23: Language Switch + GDPR Banner

**Files:**
- Create: `src/Fun88.Web/Views/Shared/_GdprBanner.cshtml`
- Add route to handle `GET /language/set?lang={code}`

- [ ] **Step 1: Add language switch endpoint to `HomeController`** (or a dedicated `LanguageController`)

```csharp
[HttpGet("/language/set")]
public IActionResult SetLanguage(string lang, string? returnUrl)
{
    if (!LanguageCode.IsValid(lang)) lang = LanguageCode.English;
    Response.Cookies.Append(_cookieOpts.LanguageCookieName, lang,
        new CookieOptions { MaxAge = TimeSpan.FromDays(365), SameSite = SameSiteMode.Strict, HttpOnly = true });
    return LocalRedirect(returnUrl ?? "/");
}
```

- [ ] **Step 2: Create `_GdprBanner.cshtml`**

Cookie consent banner shown when `gdpr_consent` cookie is absent. Three buttons: Accept All, Reject All, Manage. On click: JS writes `gdpr_consent=tracking=1&targeting=1&third_party=1` (or `=0`) — **NOT HttpOnly** (GD iframe JS must read it).

The `gdpr_consent` cookie is written by client-side JS only. Banner hides after choice. Persist choice: `SameSite=Strict`, 365-day expiry.

- [ ] **Step 3: Add banner to `_Layout.cshtml`**

```cshtml
@await Html.PartialAsync("_GdprBanner")
```

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Views/Shared/_GdprBanner.cshtml
git commit -m "feat: language switcher endpoint and GDPR consent cookie banner"
```

---

### Task 24: game-player.js (GD SDK Events)

**Files:**
- Create: `src/Fun88.Web/wwwroot/js/game-player.js`

- [ ] **Step 1: Create `game-player.js`**

```javascript
(function () {
  const overlay = document.getElementById('loading-overlay');
  const iframe = document.getElementById('game-iframe');

  window.addEventListener('message', function (event) {
    if (!iframe || event.source !== iframe.contentWindow) return;

    switch (event.data) {
      case 'SDK_GAME_START':
        if (overlay) overlay.style.display = 'none';
        break;
      case 'SDK_GAME_PAUSE':
        if (iframe) iframe.style.filter = 'blur(4px)';
        break;
      case 'SDK_GAME_RESUME':
        if (iframe) iframe.style.filter = '';
        break;
      case 'SDK_ERROR':
        if (overlay) {
          overlay.textContent = 'Game failed to load. Please refresh.';
          overlay.style.display = 'flex';
        }
        break;
    }
  });

  // Increment play count after 5 seconds (fire-and-forget)
  const slug = document.getElementById('game-iframe')?.dataset?.gameSlug;
  if (slug) {
    setTimeout(function () {
      fetch('/games/' + slug + '/play', { method: 'POST', headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '' } });
    }, 5000);
  }
}());
```

Add `data-game-slug="@Model.Slug"` attribute to the iframe in `Detail.cshtml`.

- [ ] **Step 2: Commit**

```bash
git add src/Fun88.Web/wwwroot/js/game-player.js
git commit -m "feat: game-player.js handles GD SDK postMessage events and play count tracking"
```

---

### Task 25: Program.cs — Full DI Wiring + Middleware Pipeline

**Files:**
- Modify: `src/Fun88.Web/Program.cs`

- [ ] **Step 1: Write complete `Program.cs`**

```csharp
using Fun88.Web.Infrastructure.Configuration;
using Fun88.Web.Infrastructure.Data;
using Fun88.Web.Infrastructure.Clients;
using Fun88.Web.Middleware;
using Fun88.Web.Modules.Admin.Services;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Modules.Games.Repositories;
using Fun88.Web.Modules.Games.Services;
using Fun88.Web.Modules.Scraper.Providers;
using Fun88.Web.Modules.Scraper.Services;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Options
var authCookieOpts = builder.Configuration.GetSection(AuthCookieOptions.Section).Get<AuthCookieOptions>()!;
builder.Services.Configure<GameDistributionOptions>(builder.Configuration.GetSection(GameDistributionOptions.Section));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.Section));
builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.Section));

// Database
builder.Services.AddScoped<Supabase.Client>();
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
       .UseSnakeCaseNamingConventions());

// Auth — admin cookie only (public Supabase user auth is Plan 2)
builder.Services.AddAuthentication(authCookieOpts.AdminSchemeName)
    .AddCookie(authCookieOpts.AdminSchemeName, opt =>
    {
        opt.LoginPath = "/admin/auth/login";
        opt.LogoutPath = "/admin/auth/logout";
        opt.SlidingExpiration = true;
        opt.ExpireTimeSpan = TimeSpan.FromHours(authCookieOpts.AdminExpiryHours);
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opt.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy(PolicyNames.AdminOnly, policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes(authCookieOpts.AdminSchemeName));
});

// Antiforgery
builder.Services.AddAntiforgery();

// Typed HttpClient — GameDistribution
builder.Services.AddHttpClient<GameDistributionHttpClient>((sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<GameDistributionOptions>>().Value;
    http.BaseAddress = new Uri(opts.ApiBaseUrl);
    http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    http.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
});

// Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();

// Services
builder.Services.AddScoped<IGameQueryService, GameQueryService>();
builder.Services.AddScoped<IGameImportPipeline, GameImportPipeline>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<GdEmbedUrlBuilder>();

// Providers
builder.Services.AddScoped<IGameProvider, GameDistributionProvider>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<LanguageResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Area route for Admin
app.MapControllerRoute("Admin", "admin/{controller=Dashboard}/{action=Index}/{id?}", defaults: new { area = "Admin" }, constraints: new { area = "Admin" })
   .WithMetadata(new Microsoft.AspNetCore.Mvc.AreaAttribute("Admin"));

// Default route
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/Fun88.Web
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Run all tests**

```bash
dotnet test tests/Fun88.Tests
```
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Fun88.Web/Program.cs
git commit -m "feat: complete DI wiring, middleware pipeline, admin cookie auth, antiforgery"
```

---

### Task 26: Docker Setup

**Files:**
- Create: `docker/Dockerfile`
- Create: `docker/docker-compose.yml`
- Create: `.dockerignore`

- [ ] **Step 1: Create `docker/Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Fun88.Web/Fun88.Web.csproj Fun88.Web/
RUN dotnet restore Fun88.Web/Fun88.Web.csproj
COPY src/Fun88.Web/ Fun88.Web/
RUN dotnet publish Fun88.Web/Fun88.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fun88.Web.dll"]
```

- [ ] **Step 2: Create `docker/docker-compose.yml`**

```yaml
services:
  fun88-web:
    build:
      context: ..
      dockerfile: docker/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Default: ${SUPABASE_DB_URL}
      GameDistribution__ApiKey: ${GD_API_KEY}
      GameDistribution__PublisherId: ${GD_PUBLISHER_ID}
      OpenAi__ApiKey: ${OPENAI_API_KEY}
    ports:
      - "8080:8080"
    restart: unless-stopped
```

- [ ] **Step 3: Create `.dockerignore`**

```
**/bin/
**/obj/
**/.git/
**/appsettings.Development.json
```

- [ ] **Step 4: Verify Docker build**

```bash
cd /Users/mikeyoshino/gitRepos/Fun88
docker build -f docker/Dockerfile -t fun88-web .
```
Expected: `Successfully built <image-id>`

- [ ] **Step 5: Commit**

```bash
git add docker .dockerignore
git commit -m "chore: Docker build + compose for production deployment"
```

---

### Task 27: Seed Initial Data

**Files:**
- Create: Raw SQL seed file for Supabase Dashboard
- Create: Admin user creation script (documented)

- [ ] **Step 1: Create seed migration**

```bash
cd src/Fun88.Web
dotnet ef migrations add SeedInitialData --output-dir Infrastructure/Data/Migrations
```

Edit the generated migration's `Up()` method to insert:

```csharp
// GameDistribution provider
migrationBuilder.InsertData("game_providers",
    columns: ["id", "name", "slug", "api_base_url", "is_active"],
    values: [1, "GameDistribution", "game-distribution", "https://api.gamedistribution.com", true]);

// Default categories
migrationBuilder.InsertData("categories",
    columns: ["id", "slug", "icon", "sort_order", "is_active"],
    values: new object[,]
    {
        [1, "action",   "bolt",         1,  true],
        [2, "puzzle",   "puzzle-piece",  2,  true],
        [3, "racing",   "truck",         3,  true],
        [4, "sports",   "trophy",        4,  true],
        [5, "shooting", "crosshairs",    5,  true],
        [6, "strategy", "chess-board",   6,  true],
        [7, "adventure","map",           7,  true],
        [8, "arcade",   "gamepad",       8,  true],
    });

// Category translations (EN)
// Insert CategoryTranslation rows for each category in both English and Thai
// (Thai name = English name for now — admin can edit via admin panel)
```

- [ ] **Step 2: Document admin user creation**

Admin users are NOT seeded in migration (passwords must not be in source control). Create the first admin via a one-time command documented in `docs/admin-setup.md`:

```bash
# Run in production (or development) to create first admin user
dotnet run --project src/Fun88.Web -- seed-admin --email admin@fun88.com --password <SECURE_PASSWORD>
```

Implement as an `IHostedService` that checks for `--seed-admin` CLI arg in `Program.cs`, creates the admin user via `AdminAuthService`, then exits. Add this as a future enhancement note if not implementing now — at minimum document the manual SQL approach:

```sql
INSERT INTO admin_users (id, email, password_hash, display_name, created_at)
VALUES (gen_random_uuid(), 'admin@fun88.com', '<bcrypt hash>', 'Admin', now());
```

Use `dotnet user-secrets` or a local script to generate the hash using `IPasswordHasher<AdminUser>`.

- [ ] **Step 3: Apply migration to dev DB**

```bash
cd src/Fun88.Web
dotnet ef database update
```
Expected: Migration applied, tables exist with seed data.

- [ ] **Step 4: Smoke test the full site**

```bash
dotnet run --project src/Fun88.Web
```

Verify:
- `http://localhost:5000` → Home page loads with correct dark purple UI
- Sidebar collapses to icons, labels fade in on hover
- `http://localhost:5000/admin/auth/login` → Login form renders
- After creating admin user, login redirects to `/admin` dashboard
- Admin can create a custom game via `/admin/games/create`
- Custom game appears on home page

- [ ] **Step 5: Commit**

```bash
git add src/Fun88.Web/Infrastructure/Data/Migrations
git commit -m "feat: seed migration for GameDistribution provider and default categories"
```

---

## Plan 1 Complete

**What's working after Plan 1:**
- ASP.NET MVC site runs with `dotnet run`
- Admin can log in, manage games and categories
- Admin can trigger GameDistribution import (syncs GD catalog)
- Admin can upload custom games
- Visitors see home page with game carousels and CrazyGames-style UI
- Games play in iframe with GDPR-gated GD embed URL
- English and Thai category translations (admin-managed)
- Docker build produces a production-ready image

**Plan 2 covers:** OpenAI auto-translation, Quartz.NET scheduler (automated GD sync), public user auth (Supabase), favorites, ratings, likes, play history

**Plan 3 covers:** Blog, AdSense slot management, advanced SEO, hreflang meta tags
