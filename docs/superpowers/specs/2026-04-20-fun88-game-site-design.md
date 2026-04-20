# Fun88 — HTML5 Game Site Design Spec
**Date:** 2026-04-20  
**Stack:** ASP.NET MVC · Supabase (Auth + Storage + PostgreSQL) · Docker (production) · OpenAI (translation)

---

## 1. Overview

Fun88 is an HTML5 game portal site. Games are sourced from GameDistribution (as a registered publisher) and from custom/own game uploads. All user-facing content is available in English and Thai; Thai translations are generated automatically via OpenAI on import. The UI is CrazyGames-inspired: dark purple gradient theme, collapsible icon sidebar, horizontal scroll carousels on the home page, dense grid on category pages.

---

## 2. Architecture — Modular Monolith

Single ASP.NET MVC project. Feature modules under `Modules/`. Scraper runs as `IHostedService` + Quartz.NET inside the same process. One Docker image in production.

### Project Structure

```
Fun88/
├── src/
│   └── Fun88.Web/
│       ├── Modules/
│       │   ├── Games/                    # Browse, play, search, ratings, likes
│       │   │   ├── Controllers/
│       │   │   ├── Services/             # IGameService, IGameQueryService
│       │   │   ├── Repositories/         # IGameRepository
│       │   │   └── ViewModels/
│       │   ├── Scraper/                  # GameDistribution catalog sync + import pipeline
│       │   │   ├── Providers/
│       │   │   │   ├── IGameProvider.cs  # Provider contract (open for extension)
│       │   │   │   └── GameDistributionProvider.cs
│       │   │   ├── Services/             # IGameImportPipeline, IScraperOrchestrationService
│       │   │   └── Jobs/                 # Quartz.NET IJob implementations
│       │   ├── Translation/              # OpenAI translation pipeline
│       │   │   ├── Services/             # ITranslationService
│       │   │   └── Models/               # TranslationRequest, TranslationResult, TranslationContext
│       │   ├── Users/                    # Auth, profiles, favorites, play history, ratings
│       │   │   ├── Controllers/
│       │   │   ├── Services/             # IUserService, IFavoriteService
│       │   │   └── ViewModels/
│       │   ├── Admin/                    # MVC Area — all routes under /admin
│       │   │   ├── Controllers/
│       │   │   └── ViewModels/
│       │   ├── Blog/                     # Posts, categories, SEO, scheduled publishing
│       │   │   ├── Controllers/
│       │   │   ├── Services/             # IBlogService
│       │   │   └── ViewModels/
│       │   └── Ads/                      # Google AdSense slot management
│       │       ├── Services/             # IAdSlotService
│       │       └── ViewModels/
│       ├── Infrastructure/
│       │   ├── Data/                     # EF Core DbContext, Migrations
│       │   ├── Clients/                  # Typed HttpClients: GameDistributionHttpClient, OpenAiHttpClient
│       │   ├── BackgroundServices/       # IHostedService wrappers for Quartz
│       │   └── Configuration/            # Typed options — zero magic strings
│       │       ├── GameDistributionOptions.cs
│       │       ├── OpenAiOptions.cs
│       │       ├── SupabaseOptions.cs
│       │       ├── AdSenseOptions.cs
│       │       ├── AuthCookieOptions.cs
│       │       └── GdprConsentOptions.cs
│       └── Shared/
│           ├── Constants/                # RouteNames, PolicyNames, LanguageCode, CacheKeys
│           ├── ValueObjects/             # Slug, LanguageCode enum
│           └── Extensions/              # IQueryable, IServiceCollection helpers
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
└── docs/
    └── superpowers/specs/
```

### Key Architectural Rules
- **No magic strings.** All route names, policy names, language codes, config keys, and cache keys are `static readonly` constants or strongly-typed enums.
- **Repository pattern** per module. No raw SQL except for performance-critical reporting.
- **Typed options** bound from `appsettings.json`. Secrets (API keys) from environment variables only — never in appsettings.
- **No server-side session or memory state.** All auth state in encrypted cookies.

---

## 3. Database Design

PostgreSQL via EF Core 8 on Supabase.

### i18n Principle
- Every user-facing text field lives in a `*_translations` table with `(entity_id, language_code)` composite PK.
- Technical/shared fields (slugs, URLs, counts, flags) stay in the base table.
- Language fallback: missing Thai → silently serve English.
- Language codes: `"en"` | `"th"` — defined as `static class LanguageCode` constants, never inline strings.

### Schema

```sql
-- GAMES
games
  id                uuid          PK
  slug              varchar(200)  UNIQUE NOT NULL
  provider_id       int           FK → game_providers.id  [NULLABLE — null = own/custom game]
  provider_game_id  varchar(200)  [NULLABLE — null = own/custom game]
  game_url          text          NOT NULL  (GD: "https://html5.gamedistribution.com/{MD5_ID}/")
  thumbnail_url     text          NOT NULL
  play_count        bigint        DEFAULT 0
  like_count        bigint        DEFAULT 0
  is_active         bool          DEFAULT true
  created_at        timestamptz   NOT NULL
  updated_at        timestamptz   NOT NULL

game_translations
  game_id           uuid          FK → games.id
  language_code     varchar(5)    NOT NULL  ("en" | "th")
  title             varchar(300)  NOT NULL
  description       text
  control_description text
  meta_title        varchar(160)
  meta_description  varchar(320)
  PRIMARY KEY (game_id, language_code)

-- CATEGORIES
categories
  id                int           PK
  slug              varchar(100)  UNIQUE NOT NULL
  icon              varchar(100)  (Heroicon name constant — never inline string)
  sort_order        int           DEFAULT 0
  is_active         bool          DEFAULT true

category_translations
  category_id       int           FK → categories.id
  language_code     varchar(5)    NOT NULL
  name              varchar(100)  NOT NULL
  PRIMARY KEY (category_id, language_code)

game_categories
  game_id           uuid          FK → games.id
  category_id       int           FK → categories.id
  PRIMARY KEY (game_id, category_id)

-- PROVIDERS
game_providers
  id                int           PK
  name              varchar(100)  NOT NULL
  slug              varchar(100)  UNIQUE NOT NULL
  api_base_url      text          NOT NULL
  is_active         bool          DEFAULT true

-- USERS (public)
users
  id                uuid          PK  (mirrors Supabase Auth UID)
  username          varchar(50)   UNIQUE NOT NULL
  display_name      varchar(100)
  avatar_url        text
  preferred_language varchar(5)   DEFAULT 'en'
  created_at        timestamptz   NOT NULL

user_favorites
  user_id           uuid          FK → users.id
  game_id           uuid          FK → games.id
  created_at        timestamptz   NOT NULL
  PRIMARY KEY (user_id, game_id)

user_play_history
  id                uuid          PK
  user_id           uuid          FK → users.id  [NULLABLE — anonymous guests]
  game_id           uuid          FK → games.id
  played_at         timestamptz   NOT NULL
  session_id        varchar(64)   (anonymous guest tracking via HttpOnly cookie)

game_ratings
  user_id           uuid          FK → users.id
  game_id           uuid          FK → games.id
  rating            smallint      NOT NULL  CHECK (rating BETWEEN 1 AND 5)
  created_at        timestamptz
  PRIMARY KEY (user_id, game_id)

-- ADMIN
admin_users
  id                uuid          PK
  email             varchar(200)  UNIQUE NOT NULL
  password_hash     text          NOT NULL  (ASP.NET Identity PasswordHasher)
  display_name      varchar(100)
  created_at        timestamptz
  last_login_at     timestamptz

-- SCRAPER
scraper_jobs
  id                uuid          PK
  provider_id       int           FK → game_providers.id
  triggered_by      varchar(20)   ("schedule" | "manual")
  status            varchar(20)   ("pending" | "running" | "completed" | "failed")
  games_found       int
  games_imported    int
  games_skipped     int
  error_message     text
  started_at        timestamptz
  completed_at      timestamptz

scraper_schedules
  provider_id       int           PK  FK → game_providers.id
  cron_expression   varchar(100)  NOT NULL
  is_enabled        bool          DEFAULT true
  last_run_at       timestamptz
  next_run_at       timestamptz

-- TRANSLATION JOBS
translation_jobs
  game_id           uuid          FK → games.id
  language_code     varchar(5)    NOT NULL
  status            varchar(20)   ("pending" | "completed" | "failed")
  attempt_count     smallint      DEFAULT 0
  last_error        text
  created_at        timestamptz
  completed_at      timestamptz
  PRIMARY KEY (game_id, language_code)

-- BLOG
blog_posts
  id                uuid          PK
  slug              varchar(200)  UNIQUE NOT NULL
  featured_image_url text
  author_id         uuid          FK → admin_users.id
  status            varchar(20)   ("draft" | "scheduled" | "published")
  published_at      timestamptz
  created_at        timestamptz

blog_post_translations
  post_id           uuid          FK → blog_posts.id
  language_code     varchar(5)    NOT NULL
  title             varchar(300)  NOT NULL
  body              text          NOT NULL
  excerpt           varchar(500)
  meta_title        varchar(160)
  meta_description  varchar(320)
  PRIMARY KEY (post_id, language_code)

blog_categories
  id                int           PK
  slug              varchar(100)  UNIQUE NOT NULL

blog_category_translations
  category_id       int           FK → blog_categories.id
  language_code     varchar(5)    NOT NULL
  name              varchar(100)  NOT NULL
  PRIMARY KEY (category_id, language_code)

blog_post_categories
  post_id           uuid          FK → blog_posts.id
  category_id       int           FK → blog_categories.id
  PRIMARY KEY (post_id, category_id)

-- ADS
ad_slots
  id                int           PK
  position          varchar(50)   NOT NULL  (AdSlotPosition constant: "header"|"sidebar"|"in_game"|"footer")
  adsense_slot_id   varchar(100)
  adsense_client_id varchar(100)
  is_active         bool          DEFAULT true
```

---

## 4. GameDistribution Publisher Integration

Fun88 is a registered GameDistribution publisher. Games are not scraped — they are fetched via authenticated GD Publisher API.

### Game Embed URL
Built server-side by `GdEmbedUrlBuilder` (typed service). Never string-concatenated in views.

```
https://html5.gamedistribution.com/{game.ProviderGameId}/
  ?gd_sdk_referrer_url=https://fun88.com/games/{game.Slug}
  &gdpr-tracking={consentTracking}
  &gdpr-targeting={consentTargeting}
  &gdpr-third-party={consentThirdParty}
```

### IGameProvider Contract
```csharp
interface IGameProvider
{
    string ProviderSlug { get; }
    Task<IReadOnlyList<RawGameData>> FetchGamesAsync(GameFetchOptions options, CancellationToken ct);
    Task<RawGameData?> FetchGameByIdAsync(string providerGameId, CancellationToken ct);
}

record RawGameData(
    string ProviderGameId,
    string Title,
    string Description,
    string ControlDescription,
    string ThumbnailUrl,
    string GameUrl,
    IReadOnlyList<string> CategorySlugs
);
```

Only `GameDistributionProvider` exists now. Adding a new provider = one new class, zero refactoring.

### Import Pipeline (`IGameImportPipeline`)
Both GameDistribution sync and custom game upload flow through the same pipeline:
1. Validate & normalise raw data
2. Check duplicate (`provider_game_id` + `provider_id` for GD; `slug` uniqueness for custom)
3. Upload thumbnail → Supabase Storage
4. Persist `games` row + English `game_translations` row (EF Core)
5. Enqueue `TranslationJob` → Quartz.NET one-shot
6. Assign categories
7. Update `scraper_jobs` record

**Custom games:** `provider_id = NULL`, `provider_game_id = NULL`. Admin form includes title, description, controls, game URL/embed, thumbnail upload, categories, and "Auto-translate to Thai" checkbox.

**Duplicate strategy:** GD games upsert on `(provider_id, provider_game_id)` — metadata updated if changed upstream. Custom games error on slug collision with a clear message.

### GD SDK postMessage Events
Game player page JS (`/wwwroot/js/game-player.js`) listens for:
- `SDK_GAME_START` → remove loading overlay
- `SDK_GAME_PAUSE` → blur/mute game controls (ad about to show)
- `SDK_GAME_RESUME` → restore game controls
- `SDK_ERROR` → show error state

### Revenue Streams
| In-game ads (GD) | Page ads (AdSense) |
|---|---|
| Managed by GD SDK inside iframe | Managed by our `ad_slots` table |
| 33% revenue share, paid by GD | 100% revenue, paid by Google |
| No code needed from Fun88 | Slot IDs configured in admin panel |

### Quartz.NET Jobs
- `ScraperJob` — `DisallowConcurrentExecution`, cron from `scraper_schedules` table or on-demand via `IScheduler.TriggerJob()`
- `TranslationJob` — one-shot per game, 3 retries with exponential back-off (5 min → 30 min → give up)
- All job state persisted in PostgreSQL Quartz tables — no memory state

---

## 5. Translation Pipeline

### Flow
1. Game saved with English translation
2. Quartz fires `TranslationJob(game_id)`
3. `ITranslationService.TranslateGameAsync(gameId, LanguageCode.Thai)`
4. Load English `game_translations` row
5. Batch all fields into **one** OpenAI Chat Completions request
6. Parse structured JSON response
7. Upsert `game_translations` row for `"th"`

### OpenAI Config
```csharp
class OpenAiOptions
{
    public string ApiKey { get; init; }           // env var only — never appsettings
    public string Model { get; init; }            // "gpt-4o-mini"
    public int MaxTokensPerRequest { get; init; } // 1000
    public int TranslationTimeoutSeconds { get; init; } // 30
    public bool TranslationEnabled { get; init; } // kill-switch
}
```

### System Prompt Rules (stored as constant)
- Game titles and proper nouns: keep in English
- Preserve all HTML tags exactly
- Use common Thai gaming terminology
- Output only valid JSON matching input structure

### Cost Estimate
~$0.000135 per game (gpt-4o-mini). 10,000 games ≈ $1.35 total.

### Generic Service
`ITranslationService` takes `Dictionary<string, string>` fields + `TranslationContext` enum. Used by games, blog posts, and categories — no coupling to any single entity type.

### Failure Handling
Failed jobs tracked in `translation_jobs` table. Admin panel shows failed translations with individual and bulk-retry buttons. Admin can also manually edit Thai translations for any game.

---

## 6. Pages & Routing

### Public Routes
```
GET  /                              Home — featured, newest, most popular by category
GET  /games                         All games, paginated
GET  /games/{slug}                  Game player page
GET  /games/category/{slug}         Category page
GET  /games/newest                  Newest games
GET  /games/most-popular            Most popular games
GET  /search?q={query}              Full-text search (EN + TH)
GET  /blog                          Blog index
GET  /blog/{slug}                   Blog post
GET  /blog/category/{slug}          Blog category
GET  /account/login                 Login
GET  /account/register              Register
GET  /account/profile               [Authorize]
GET  /account/favorites             [Authorize]
GET  /account/history               [Authorize]
POST /games/{slug}/like             [Authorize]
POST /games/{slug}/rate             [Authorize]
POST /games/{slug}/play             Anonymous OK (increments play_count)
GET  /language/set?lang={code}      Set language cookie + redirect
```

### Admin Routes (Area: "Admin", prefix: /admin)
All require `[Authorize(Policy = PolicyNames.AdminOnly)]`.

```
GET|POST /admin/games/*             CRUD + retranslate
GET|POST /admin/scraper/*           Schedule config, on-demand trigger, job history
GET|POST /admin/categories/*        CRUD
GET|POST /admin/blog/*              CRUD, publish, schedule
GET|POST /admin/ads/*               AdSense slot config
GET|POST /admin/users/*             View, disable/enable
GET|POST /admin/translations/*      Retry failed/pending jobs
GET      /admin                     Dashboard (stats)
```

### Route & Policy Constants
```csharp
static class RouteNames { public const string GamePlay = nameof(GamePlay); /* ... */ }
static class PolicyNames { public const string AdminOnly = nameof(AdminOnly); /* ... */ }
static class LanguageCode { public const string English = "en"; public const string Thai = "th"; }
```

### Language Resolution Order
1. Authenticated user's `preferred_language` from DB
2. `lang` cookie
3. `Accept-Language` header
4. Default: `"en"`

Language is **not** in the URL path. `hreflang` meta tags handle SEO.

---

## 7. UI Design

### Design Tokens
| Token | Value |
|---|---|
| bg-base | `#0d0b14` |
| bg-surface (sidebar/header) | `linear-gradient(180deg, #1e1a2e 0%, #16132a 100%)` |
| bg-card | `#1a1729` |
| border | `#2a2540` |
| accent-primary | `#6c5ce7` |
| accent-new badge | `#00b894` |
| accent-hot badge | `#e17055` |
| accent-top badge | `#fdcb6e` |
| text-primary | `#ffffff` |
| text-secondary | `#c0bdd8` |
| text-muted | `#6c6880` |
| icon-inactive | `#7c7a96` |
| icon-active | `#a29bfe` |

### Layout — All Pages
- **Top nav:** 100% viewport width, `52px` tall, gradient background. Contains: hamburger (☰) · logo · search bar · bell · heart · avatar · Login button.
- **Below nav:** collapsible sidebar (left) + main content (right)
- **Sidebar:** `60px` collapsed (icon only, labels `opacity: 0`), `230px` on CSS hover (labels fade in). `stroke-width: 2.5` Heroicon outlines. Active item: `#6c5ce7` left border + `#a29bfe` text/icon.
- **Sidebar sections:** Quick links (Home, Recently played, New, Popular, Updated, Multiplayer, Leaderboards) → divider → Categories A–Z (scrollable)

### Home Page
- Category chip row below nav
- "Top picks" — large featured cards (280px wide), horizontal scroll
- Per-section rows (New games, Most Popular, per-category) — standard cards 180×135px, horizontal scroll, "View more →" link
- Badges on cards: NEW (green), HOT (orange), TOP (gold) — driven by DB flags

### Game Detail / Player Page
- Three-column: sidebar / game+info / right sidebar
- Game iframe: 16:9 responsive, max-height fills between nav and footer
- Below iframe: title, rating, play count, category tags, metadata grid, description, controls, "More Games Like This" horizontal scroll
- Right sidebar: AdSense 300×250 slot + vertical recommended games list
- GD SDK postMessage events handled in `/wwwroot/js/game-player.js`

### Category Page
- Breadcrumb: Games › Category Name
- Category title + collapsible description + "Show More" toggle
- Sort dropdown (Top / Newest / Most Played)
- 6-column dense game grid, 4:3 aspect ratio thumbnails
- Pagination at bottom
- SEO content (category description + FAQ) below grid in current language

---

## 8. Auth & Security

### Public Users (Supabase)
- Supabase JWT + refresh token stored in separate `HttpOnly; Secure; SameSite=Strict` encrypted cookies
- ASP.NET Data Protection API for cookie encryption
- JWT validated per-request via middleware — no server-side session, no memory state
- Password reset and email verification delegated entirely to Supabase

### Admin Users
- Own `admin_users` table, ASP.NET Identity `PasswordHasher` — never touch Supabase for admin
- Separate admin cookie, 8-hour sliding expiry, `HttpOnly; Secure; SameSite=Strict`
- No "remember me" option
- `[Authorize(Policy = PolicyNames.AdminOnly)]` on every admin action — no exceptions

### CSRF
- ASP.NET antiforgery middleware enabled globally
- `[ValidateAntiForgeryToken]` on all POST/PUT/DELETE
- Token injected via `@Html.AntiForgeryToken()` in all forms

### GDPR Consent Cookie
- Name: `gdpr_consent` (from `GdprConsentOptions.CookieName`)
- **Not** HttpOnly — JS reads it to pass flags to GD iframe
- `SameSite=Strict`, 365-day expiry
- Default: all flags `0` (privacy-first) until user explicitly accepts
- Cookie banner shown on first visit

### Security Headers (middleware, not per-controller)
- `Content-Security-Policy`: frame-src restricted to `html5.gamedistribution.com`
- `X-Frame-Options: SAMEORIGIN`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`

---

## 9. Deployment

### Local Development
```
dotnet run
```
EF Core connects to Supabase PostgreSQL via `ConnectionStrings:Default` in `appsettings.Development.json` (gitignored). No Docker needed locally.

### Production (Docker)
```yaml
# docker-compose.yml
services:
  fun88-web:
    build: .
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: ${SUPABASE_DB_URL}
      OpenAi__ApiKey: ${OPENAI_API_KEY}
      GameDistribution__ApiKey: ${GD_API_KEY}
      GameDistribution__PublisherId: ${GD_PUBLISHER_ID}
      Supabase__ServiceKey: ${SUPABASE_SERVICE_KEY}
    ports:
      - "8080:8080"
```

- Single image — no sidecar containers
- Quartz.NET uses PostgreSQL job store (same Supabase DB)
- EF Core migrations run at startup via `dbContext.Database.MigrateAsync()`
- All secrets from environment variables — never baked into image

---

## 10. UI Mockup Reference

The approved interactive UI mockup is saved at:

```
docs/superpowers/specs/ui/2026-04-20-fun88-ui-mockup.html
```

Open in a browser to see the live demo. Covers:
- **Home page** — full-width top nav, collapsible sidebar (hover to expand), category chips, featured large cards, horizontal scroll game rows with NEW/HOT/TOP badges
- **Sidebar behaviour** — 60px collapsed (icons only, labels `opacity:0`), 230px expanded on hover, Heroicon outlines at `stroke-width:2.5`, active item purple left border
- **Game cards** — 180×135px thumbnails (4:3), badge overlay, hover scale effect

Additional mockup iterations (brainstorm history) are in `.superpowers/brainstorm/` — not committed.

---

## 11. Open Questions / Decisions Deferred to Implementation

- Exact GameDistribution Publisher API endpoint URL — confirm from GD dashboard after publisher account is set up
- Supabase project URL and keys — provided at deploy time
- Domain name / SSL termination — handled at infrastructure level, outside this spec
- Whether to use Supabase Realtime for live play count updates — can add later without schema changes
