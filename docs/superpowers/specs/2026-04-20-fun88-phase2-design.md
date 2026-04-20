# Fun88 — Phase 2 Design Spec
**Date:** 2026-04-20  
**Depends on:** Phase 1 Foundation (Plan 1)  
**Scope:** OpenAI auto-translation · Quartz.NET scraper scheduling · Public user auth (Supabase) · Favorites, ratings, likes, play history

---

## 1. Overview

Phase 2 adds four subsystems built in dependency order:

1. **Translation** — OpenAI-powered Thai translation triggered automatically on game import and manually from the admin panel
2. **Scheduler** — Quartz.NET cron-based GameDistribution sync + one-shot translation jobs, all persisted in PostgreSQL
3. **User Auth** — Public register/login via Supabase Auth SDK, mirrored into our `users` table
4. **Engagement** — Favorites, star ratings, likes, play history for authenticated users

All modules live in the existing modular monolith (`Fun88.Web`). No new projects or services.

---

## 2. Architecture

### Module Layout (additions to Phase 1)

```
Modules/
├── Translation/
│   ├── Services/
│   │   ├── ITranslationService.cs
│   │   └── OpenAiTranslationService.cs
│   ├── Models/
│   │   ├── TranslationContext.cs          (enum: Game | BlogPost | Category)
│   │   ├── TranslationRequest.cs
│   │   └── TranslationResult.cs
│   └── Jobs/
│       └── TranslationJob.cs              (Quartz IJob)
├── Scraper/
│   └── Jobs/
│       └── ScraperJob.cs                  (Quartz IJob — new, replaces manual-only trigger)
├── Users/
│   ├── Controllers/
│   │   └── AccountController.cs
│   ├── Services/
│   │   ├── IUserSyncService.cs
│   │   ├── UserSyncService.cs
│   │   ├── IFavoriteService.cs
│   │   ├── FavoriteService.cs
│   │   ├── IGameRatingService.cs
│   │   ├── GameRatingService.cs
│   │   ├── ILikeService.cs
│   │   ├── LikeService.cs
│   │   ├── IPlayHistoryService.cs
│   │   └── PlayHistoryService.cs
│   └── ViewModels/
│       ├── LoginViewModel.cs
│       ├── RegisterViewModel.cs
│       └── ProfileViewModel.cs
Infrastructure/
├── Clients/
│   └── OpenAiHttpClient.cs                (typed HttpClient)
├── BackgroundServices/
│   ├── QuartzHostedService.cs             (IHostedService wrapping IScheduler)
│   └── QuartzStartupService.cs            (IHostedService — registers cron triggers on startup)
├── Data/
│   └── Entities/
│       ├── User.cs
│       ├── UserFavorite.cs
│       ├── UserPlayHistory.cs
│       ├── GameRating.cs
│       ├── UserLike.cs
│       ├── ScraperJob.cs                  (entity, not Quartz job)
│       ├── ScraperSchedule.cs
│       └── TranslationJob.cs              (entity, not Quartz job)
```

### New Configuration

```csharp
// Already defined in Phase 1, now fully wired:
class OpenAiOptions
{
    public const string Section = "OpenAi";
    public string ApiKey { get; init; }                    // env var only
    public string Model { get; init; }                     // "gpt-4o-mini"
    public int MaxTokensPerRequest { get; init; }          // 1000
    public int TranslationTimeoutSeconds { get; init; }    // 30
    public bool TranslationEnabled { get; init; }          // kill-switch
}

// New:
class QuartzOptions
{
    public const string Section = "Quartz";
    public string ConnectionString { get; init; }          // direct Postgres (not PostgREST)
}
```

---

## 3. Module 1 — Translation

### `ITranslationService`

```csharp
interface ITranslationService
{
    Task<Dictionary<string, string>> TranslateAsync(
        Dictionary<string, string> fields,
        TranslationContext context,
        string targetLanguage,
        CancellationToken ct = default);
}
```

- Takes field name → English value pairs
- Batches all fields into **one** OpenAI Chat Completions call
- System prompt (stored as `const string` in `OpenAiTranslationService`):
  - Game titles and proper nouns: keep in English
  - Preserve all HTML tags exactly
  - Use common Thai gaming terminology
  - Output only valid JSON matching input structure
- Parses JSON response → returns field name → Thai value pairs
- If `OpenAiOptions.TranslationEnabled = false`, logs a warning and returns an empty dictionary — callers must not upsert empty results

### `OpenAiHttpClient`

Typed `HttpClient`:
- Base address from `OpenAiOptions` (hardcoded to `https://api.openai.com/v1/`)
- `Authorization: Bearer {ApiKey}` default header
- Timeout: `OpenAiOptions.TranslationTimeoutSeconds`
- Registered as `services.AddHttpClient<OpenAiHttpClient>()`

### `TranslationJob` (Quartz `IJob`)

- Accepts `game_id` via `JobDataMap`
- Flow:
  1. Load English `game_translations` row from Supabase
  2. Build field dictionary (`title`, `description`, `control_description`)
  3. Call `ITranslationService.TranslateAsync(..., TranslationContext.Game, "th")`
  4. Upsert Thai `game_translations` row
  5. Update `translation_jobs` row: status=completed, completed_at=now
- On exception: update `translation_jobs` row (status=failed, last_error, attempt_count++)
- Retry strategy: Quartz `WithSimpleSchedule` — 3 attempts, 5 min → 30 min intervals

### Auto-trigger on import

`GameImportPipeline`, after persisting a game row, schedules a one-shot `TranslationJob`:
```csharp
if (_openAiOptions.TranslationEnabled)
{
    var trigger = TriggerBuilder.Create().StartNow().Build();
    await _scheduler.ScheduleJob(jobDetail, trigger, ct);
}
```

Also creates a `translation_jobs` row (status=pending) before scheduling.

### Admin panel integration

`/admin/translations` page (new controller `AdminTranslationsController`):
- Lists `translation_jobs` rows where `status = "failed"` or `status = "pending"`
- "Retry" button per row → triggers one-shot `TranslationJob` via `IScheduler.TriggerJob`
- "Retry All Failed" button → bulk trigger
- Game detail admin edit page: "Re-translate to Thai" button → triggers one-shot job

---

## 4. Module 2 — Quartz.NET Scheduler

### Setup

```csharp
services.AddQuartz(q =>
{
    q.UsePersistentStore(s =>
    {
        s.UsePostgres(cfg => cfg.ConnectionString = quartzConnStr);
        s.UseJsonSerializer();
    });
    q.AddJob<ScraperJob>(opts => opts.WithIdentity("scraper-gd"));
    q.AddJob<TranslationJob>(opts => opts.StoreDurably());
});
services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
```

### `ScraperJob` (`[DisallowConcurrentExecution]`)

- On execute:
  1. Load `scraper_schedules` row for this provider
  2. Create `scraper_jobs` row (status=running, triggered_by="schedule"|"manual")
  3. Call `IGameProvider.FetchGamesAsync()`
  4. For each game: call `IGameImportPipeline.ImportAsync()`
  5. Update `scraper_jobs` row (status=completed, games_found/imported/skipped counts)
- On exception: update row (status=failed, error_message)

### Cron Schedule Registration

On app startup, `QuartzStartupService : IHostedService` (runs before Quartz):
1. Load all `scraper_schedules` rows where `is_enabled = true`
2. For each: register Quartz cron trigger (skip if trigger already exists — idempotent)

### Admin Scraper Panel (`/admin/scraper`)

- Table of `scraper_jobs` rows (last 20 runs): started_at, completed_at, status, counts, error
- "Run Now" button → `IScheduler.TriggerJob(new JobKey("scraper-gd"))`
- Schedule edit form: cron expression input → updates `scraper_schedules` row + re-registers trigger
- Next scheduled run time displayed (read from Quartz `ITrigger.GetNextFireTimeUtc()`)

### Quartz Schema

Provided as `docs/supabase/quartz-schema.sql` — standard Quartz.NET PostgreSQL DDL. Run once in Supabase SQL editor before deployment.

---

## 5. Module 3 — Public User Auth

### Supabase Auth Flow

**Register** (`POST /account/register`):
1. Call `supabaseClient.Auth.SignUpAsync(email, password)`
2. On success: call `IUserSyncService.SyncAsync(authUser)` — upserts `users` row with `id = authUser.Id`, `preferred_language = "en"`
3. Store Supabase session JWT in encrypted ASP.NET cookie (scheme `"UserAuth"`)
4. Redirect to home

**Login** (`POST /account/login`):
1. Call `supabaseClient.Auth.SignInWithPasswordAsync(email, password)`
2. On success: `IUserSyncService.SyncAsync(authUser)` — upserts `users` row (updates `last_login_at`)
3. Store session in `"UserAuth"` cookie
4. Redirect to return URL or home

**Logout** (`POST /account/logout`):
1. Call `supabaseClient.Auth.SignOutAsync()`
2. Delete `"UserAuth"` cookie
3. Redirect to home

### Auth Schemes

```csharp
// "AdminAuth" scheme — unchanged from Phase 1
// "UserAuth" scheme — new:
services.AddAuthentication()
    .AddCookie("UserAuth", o =>
    {
        o.LoginPath = "/account/login";
        o.LogoutPath = "/account/logout";
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
        o.SlidingExpiration = true;
    });

services.AddAuthorization(o =>
{
    o.AddPolicy(PolicyNames.AdminOnly, p => p.AddAuthenticationSchemes("AdminAuth").RequireRole("Admin").RequireAuthenticatedUser());
    o.AddPolicy(PolicyNames.UserOnly,  p => p.AddAuthenticationSchemes("UserAuth").RequireAuthenticatedUser());
});
```

### `IUserSyncService`

```csharp
interface IUserSyncService
{
    Task<User> SyncAsync(Supabase.Gotrue.User authUser, CancellationToken ct = default);
}
```

Upserts `users` row on `id` — creates if missing, updates `last_login_at` if exists.

### `LanguageResolutionMiddleware` Update

Step 1 (previously stub): if `"UserAuth"` cookie present and valid, load `users.preferred_language` from Supabase and set `HttpContext.Items["lang"]`.

### Routes

```
GET  /account/login      → LoginViewModel
POST /account/login      → authenticate + redirect
GET  /account/register   → RegisterViewModel
POST /account/register   → create + redirect
POST /account/logout     → sign out + redirect
GET  /account/profile    [Authorize(Policy="UserOnly")] → ProfileViewModel
POST /account/profile    [Authorize(Policy="UserOnly")] → update display_name, preferred_language
```

### Views

- `Views/Account/Login.cshtml` — email + password form, "Remember me" checkbox
- `Views/Account/Register.cshtml` — email + password + confirm password form
- `Views/Account/Profile.cshtml` — display name field, language selector dropdown
- `Views/Shared/_LoginPartial.cshtml` — shows username + logout when authenticated, or login/register links

---

## 6. Module 4 — User Engagement

### New Schema

```sql
-- Add to existing schema:
user_likes
  user_id     uuid        FK → users.id
  game_id     uuid        FK → games.id
  created_at  timestamptz NOT NULL
  PRIMARY KEY (user_id, game_id)
```

All other tables (`user_favorites`, `user_play_history`, `game_ratings`) already defined in the Phase 1 spec schema.

### Favorites (`IFavoriteService`)

```csharp
interface IFavoriteService
{
    Task AddAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<GameCardViewModel>> GetPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
}
```

- `POST /games/{slug}/favorite` — toggle (add if missing, remove if exists). Returns `{ favorited: true/false }` JSON. Requires `[Authorize(Policy="UserOnly")]`.
- `/account/favorites` — paginated grid using `_GameCard` partial.
- Heart icon on game detail page — filled/empty based on `IsFavoriteAsync` result passed in `DetailViewModel`.

### Star Ratings (`IGameRatingService`)

```csharp
interface IGameRatingService
{
    Task UpsertAsync(Guid userId, Guid gameId, int rating, CancellationToken ct = default);
    Task<double> GetAverageAsync(Guid gameId, CancellationToken ct = default);
    Task<int?> GetUserRatingAsync(Guid userId, Guid gameId, CancellationToken ct = default);
}
```

- `POST /games/{slug}/rate` — accepts `rating` (1–5), upserts `game_ratings` row. Requires auth.
- Game detail page: 5-star widget (server-rendered, submitted via fetch). Shows average rating + count.
- Average computed at query time from `game_ratings` table — no denormalized column.

### Likes (`ILikeService`)

```csharp
interface ILikeService
{
    Task<(long NewCount, bool Liked)> ToggleAsync(Guid userId, Guid gameId, CancellationToken ct = default);
}
```

- `POST /games/{slug}/like` — toggle. Updates `games.like_count` (increment/decrement). Inserts/deletes `user_likes` row. Returns `{ likeCount: 42, liked: true }` JSON. Requires auth.
- Game detail page: thumbs-up button with count, updated client-side on response.

### Play History (`IPlayHistoryService`)

```csharp
interface IPlayHistoryService
{
    Task RecordAsync(Guid? userId, Guid gameId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<GameCardViewModel>> GetRecentAsync(Guid userId, int limit, CancellationToken ct = default);
}
```

- `POST /games/{slug}/play` — already stubbed in `GamesController`. Now calls `IPlayHistoryService.RecordAsync()`. Increments `games.play_count`. Guests identified by `session_id` cookie (HttpOnly, set on first visit).
- `/account/history` — shows last 20 played games (authenticated only).

---

## 7. Data Flow — Game Import with Translation

```
Admin triggers sync (manual or cron)
  → ScraperJob executes
    → GameDistributionProvider.FetchGamesAsync()
    → GameImportPipeline.ImportAsync(rawGame)
      → Validate & normalise
      → Dedup check (provider_id + provider_game_id)
      → Persist games row + EN game_translations row
      → Create translation_jobs row (status=pending)
      → Scheduler.ScheduleJob(TranslationJob, startNow)   ← if TranslationEnabled
      → Assign game_categories
    → Update scraper_jobs row (counts)
  
  [async, moments later]
  TranslationJob executes
    → Load EN game_translations
    → OpenAiTranslationService.TranslateAsync(fields, Game, "th")
      → POST https://api.openai.com/v1/chat/completions
      → Parse JSON response
    → Upsert TH game_translations
    → Update translation_jobs row (status=completed)
```

---

## 8. Testing

### Translation
- `OpenAiTranslationServiceTests` — mock `OpenAiHttpClient`, verify request JSON structure, verify response parsing, verify kill-switch skips API call
- `TranslationJobTests` — mock `ITranslationService`, verify upsert called on success, verify error state on failure

### Scheduler
- `ScraperJobTests` — mock `IGameImportPipeline` + `IGameProvider`, verify job row updated with correct counts
- Integration: no Quartz scheduler tests (infrastructure concern); test `ScraperJob.Execute()` directly by injecting a mock `IJobExecutionContext`

### User Auth
- `AccountControllerTests` — mock `Supabase.Client.Auth`, verify cookie set on success, verify redirect on failure
- `UserSyncServiceTests` — verify upsert logic (new user vs returning user)

### Engagement
- `FavoriteServiceTests` — toggle add/remove, `IsFavoriteAsync` returns correct state
- `LikeServiceTests` — verify `like_count` incremented/decremented, double-toggle is idempotent
- `PlayHistoryServiceTests` — record with userId null (guest), record with userId set

---

## 9. New SQL (Phase 2 additions)

### `docs/supabase/phase2-schema.sql`
```sql
-- user_likes table (not in Phase 1 schema)
CREATE TABLE user_likes (
  user_id    uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  game_id    uuid        NOT NULL REFERENCES games(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, game_id)
);

-- Add last_login_at to users (updated by UserSyncService on each login)
ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_at timestamptz;
```

### `docs/supabase/quartz-schema.sql`
Standard Quartz.NET PostgreSQL DDL (provided in full — ~11 tables).

---

## 10. Out of Scope (Phase 3)

- Blog module
- AdSense slot management
- Full SEO (sitemap, hreflang meta, structured data)
- Social login (Google/Facebook OAuth)
- Admin user management panel
