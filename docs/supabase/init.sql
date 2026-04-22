-- ============================================================
-- Fun88 — Complete Database Init Script
-- Run this once in the Supabase SQL Editor.
-- Safe to re-run: uses CREATE TABLE IF NOT EXISTS / ON CONFLICT DO NOTHING.
-- ============================================================

-- ── 1. CORE LOOKUP TABLES ────────────────────────────────────

CREATE TABLE IF NOT EXISTS public.categories (
    id          SERIAL PRIMARY KEY,
    slug        VARCHAR(255) NOT NULL UNIQUE,
    icon        VARCHAR(255),
    sort_order  INT          NOT NULL DEFAULT 0,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS public.category_translations (
    category_id   INT          NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    language_code VARCHAR(10)  NOT NULL,
    name          TEXT         NOT NULL,
    PRIMARY KEY (category_id, language_code)
);

CREATE TABLE IF NOT EXISTS public.game_providers (
    id           SERIAL PRIMARY KEY,
    name         VARCHAR(255) NOT NULL,
    slug         VARCHAR(255) NOT NULL UNIQUE,
    api_base_url VARCHAR(255),
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE
);

-- ── 2. USERS ─────────────────────────────────────────────────
-- Public users synced from Supabase Auth on first login.

CREATE TABLE IF NOT EXISTS public.users (
    id                 UUID        PRIMARY KEY,   -- matches Supabase Auth uid
    username           TEXT        NOT NULL UNIQUE,
    display_name       TEXT,
    avatar_url         TEXT,
    preferred_language VARCHAR(10) NOT NULL DEFAULT 'en',
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_login_at      TIMESTAMPTZ
);

-- ── 3. GAMES ─────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS public.games (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    slug             VARCHAR(255) NOT NULL UNIQUE,
    provider_id      INT          REFERENCES public.game_providers(id) ON DELETE SET NULL,
    provider_game_id VARCHAR(255),
    game_url         TEXT         NOT NULL,
    thumbnail_url    TEXT         NOT NULL,
    play_count       BIGINT       NOT NULL DEFAULT 0,
    like_count       BIGINT       NOT NULL DEFAULT 0,
    is_active        BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_games_provider_game_id
    ON public.games(provider_id, provider_game_id)
    WHERE provider_game_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS public.game_translations (
    game_id          UUID        NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    language_code    VARCHAR(10) NOT NULL,
    title            TEXT        NOT NULL,
    description      TEXT,
    control_description TEXT,
    meta_title       TEXT,
    meta_description TEXT,
    PRIMARY KEY (game_id, language_code)
);

CREATE TABLE IF NOT EXISTS public.game_categories (
    game_id     UUID NOT NULL REFERENCES public.games(id)       ON DELETE CASCADE,
    category_id INT  NOT NULL REFERENCES public.categories(id)  ON DELETE CASCADE,
    PRIMARY KEY (game_id, category_id)
);

-- ── 4. SCRAPER ───────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS public.scraper_schedules (
    provider_id     INT          PRIMARY KEY REFERENCES public.game_providers(id) ON DELETE CASCADE,
    cron_expression VARCHAR(120) NOT NULL,
    is_enabled      BOOLEAN      NOT NULL DEFAULT TRUE,
    last_run_at     TIMESTAMPTZ,
    next_run_at     TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS public.scraper_jobs (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    provider_id    INT         NOT NULL REFERENCES public.game_providers(id) ON DELETE CASCADE,
    triggered_by   VARCHAR(50) NOT NULL,  -- 'schedule' | 'manual'
    status         VARCHAR(50) NOT NULL DEFAULT 'pending',  -- 'pending' | 'running' | 'completed' | 'failed' | 'cancelled'
    games_found    INT         NOT NULL DEFAULT 0,
    games_imported INT         NOT NULL DEFAULT 0,
    games_skipped  INT         NOT NULL DEFAULT 0,
    error_message  TEXT,
    started_at     TIMESTAMPTZ,
    completed_at   TIMESTAMPTZ
);

-- ── 5. TRANSLATION JOBS ──────────────────────────────────────

CREATE TABLE IF NOT EXISTS public.translation_jobs (
    game_id       UUID        NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    language_code VARCHAR(10) NOT NULL,
    status        VARCHAR(50) NOT NULL DEFAULT 'pending',  -- 'pending' | 'completed' | 'failed'
    attempt_count SMALLINT    NOT NULL DEFAULT 0,
    last_error    TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at  TIMESTAMPTZ,
    PRIMARY KEY (game_id, language_code)
);

-- ── 6. USER ENGAGEMENT ───────────────────────────────────────

CREATE TABLE IF NOT EXISTS public.user_favorites (
    user_id    UUID        NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    game_id    UUID        NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, game_id)
);

CREATE TABLE IF NOT EXISTS public.user_likes (
    user_id    UUID        NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    game_id    UUID        NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, game_id)
);

CREATE TABLE IF NOT EXISTS public.game_ratings (
    user_id    UUID        NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    game_id    UUID        NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    rating     SMALLINT    NOT NULL CHECK (rating BETWEEN 1 AND 5),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, game_id)
);

CREATE TABLE IF NOT EXISTS public.user_play_history (
    id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID        REFERENCES public.users(id) ON DELETE SET NULL,
    game_id    UUID        NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    played_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    session_id TEXT        NOT NULL DEFAULT ''
);

-- ── 7. QUARTZ.NET SCHEDULER ──────────────────────────────────

CREATE TABLE IF NOT EXISTS qrtz_job_details (
    sched_name        VARCHAR(120) NOT NULL,
    job_name          VARCHAR(200) NOT NULL,
    job_group         VARCHAR(200) NOT NULL,
    description       VARCHAR(250),
    job_class_name    VARCHAR(250) NOT NULL,
    is_durable        BOOLEAN      NOT NULL,
    is_nonconcurrent  BOOLEAN      NOT NULL,
    is_update_data    BOOLEAN      NOT NULL,
    requests_recovery BOOLEAN      NOT NULL,
    job_data          BYTEA,
    PRIMARY KEY (sched_name, job_name, job_group)
);

CREATE TABLE IF NOT EXISTS qrtz_triggers (
    sched_name     VARCHAR(120) NOT NULL,
    trigger_name   VARCHAR(200) NOT NULL,
    trigger_group  VARCHAR(200) NOT NULL,
    job_name       VARCHAR(200) NOT NULL,
    job_group      VARCHAR(200) NOT NULL,
    description    VARCHAR(250),
    next_fire_time BIGINT,
    prev_fire_time BIGINT,
    priority       INTEGER,
    trigger_state  VARCHAR(16)  NOT NULL,
    trigger_type   VARCHAR(8)   NOT NULL,
    start_time     BIGINT       NOT NULL,
    end_time       BIGINT,
    calendar_name  VARCHAR(200),
    misfire_instr  SMALLINT,
    job_data       BYTEA,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, job_name, job_group)
        REFERENCES qrtz_job_details (sched_name, job_name, job_group)
);

CREATE TABLE IF NOT EXISTS qrtz_simple_triggers (
    sched_name        VARCHAR(120) NOT NULL,
    trigger_name      VARCHAR(200) NOT NULL,
    trigger_group     VARCHAR(200) NOT NULL,
    repeat_count      BIGINT       NOT NULL,
    repeat_interval   BIGINT       NOT NULL,
    times_triggered   BIGINT       NOT NULL,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
);

CREATE TABLE IF NOT EXISTS qrtz_cron_triggers (
    sched_name      VARCHAR(120) NOT NULL,
    trigger_name    VARCHAR(200) NOT NULL,
    trigger_group   VARCHAR(200) NOT NULL,
    cron_expression VARCHAR(120) NOT NULL,
    time_zone_id    VARCHAR(80),
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
);

CREATE TABLE IF NOT EXISTS qrtz_simprop_triggers (
    sched_name    VARCHAR(120) NOT NULL,
    trigger_name  VARCHAR(200) NOT NULL,
    trigger_group VARCHAR(200) NOT NULL,
    str_prop_1    VARCHAR(512),
    str_prop_2    VARCHAR(512),
    str_prop_3    VARCHAR(512),
    int_prop_1    INTEGER,
    int_prop_2    INTEGER,
    long_prop_1   BIGINT,
    long_prop_2   BIGINT,
    dec_prop_1    NUMERIC(13,4),
    dec_prop_2    NUMERIC(13,4),
    bool_prop_1   BOOLEAN,
    bool_prop_2   BOOLEAN,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
);

CREATE TABLE IF NOT EXISTS qrtz_blob_triggers (
    sched_name    VARCHAR(120) NOT NULL,
    trigger_name  VARCHAR(200) NOT NULL,
    trigger_group VARCHAR(200) NOT NULL,
    blob_data     BYTEA,
    PRIMARY KEY (sched_name, trigger_name, trigger_group),
    FOREIGN KEY (sched_name, trigger_name, trigger_group)
        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
);

CREATE TABLE IF NOT EXISTS qrtz_calendars (
    sched_name    VARCHAR(120) NOT NULL,
    calendar_name VARCHAR(200) NOT NULL,
    calendar      BYTEA        NOT NULL,
    PRIMARY KEY (sched_name, calendar_name)
);

CREATE TABLE IF NOT EXISTS qrtz_paused_trigger_grps (
    sched_name    VARCHAR(120) NOT NULL,
    trigger_group VARCHAR(200) NOT NULL,
    PRIMARY KEY (sched_name, trigger_group)
);

CREATE TABLE IF NOT EXISTS qrtz_fired_triggers (
    sched_name         VARCHAR(120) NOT NULL,
    entry_id           VARCHAR(95)  NOT NULL,
    trigger_name       VARCHAR(200) NOT NULL,
    trigger_group      VARCHAR(200) NOT NULL,
    instance_name      VARCHAR(200) NOT NULL,
    fired_time         BIGINT       NOT NULL,
    sched_time         BIGINT       NOT NULL,
    priority           INTEGER      NOT NULL,
    state              VARCHAR(16)  NOT NULL,
    job_name           VARCHAR(200),
    job_group          VARCHAR(200),
    is_nonconcurrent   BOOLEAN,
    requests_recovery  BOOLEAN,
    PRIMARY KEY (sched_name, entry_id)
);

CREATE TABLE IF NOT EXISTS qrtz_scheduler_state (
    sched_name       VARCHAR(120) NOT NULL,
    instance_name    VARCHAR(200) NOT NULL,
    last_checkin_time BIGINT      NOT NULL,
    checkin_interval BIGINT       NOT NULL,
    PRIMARY KEY (sched_name, instance_name)
);

CREATE TABLE IF NOT EXISTS qrtz_locks (
    sched_name VARCHAR(120) NOT NULL,
    lock_name  VARCHAR(40)  NOT NULL,
    PRIMARY KEY (sched_name, lock_name)
);

-- ── 8. ROW LEVEL SECURITY ────────────────────────────────────

ALTER TABLE public.categories        ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.category_translations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_providers    ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.games             ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_translations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_categories   ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.users             ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_favorites    ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_likes        ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_ratings      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_play_history ENABLE ROW LEVEL SECURITY;

-- Public read policies
DROP POLICY IF EXISTS "Public read active games"          ON public.games;
DROP POLICY IF EXISTS "Public read active categories"     ON public.categories;
DROP POLICY IF EXISTS "Public read category_translations" ON public.category_translations;
DROP POLICY IF EXISTS "Public read game_translations"     ON public.game_translations;
DROP POLICY IF EXISTS "Public read game_categories"       ON public.game_categories;
DROP POLICY IF EXISTS "Public read game_providers"        ON public.game_providers;
DROP POLICY IF EXISTS "Users read own favorites"          ON public.user_favorites;
DROP POLICY IF EXISTS "Users read own likes"              ON public.user_likes;
DROP POLICY IF EXISTS "Users read own ratings"            ON public.game_ratings;

CREATE POLICY "Public read active games"
    ON public.games FOR SELECT USING (is_active = true);

CREATE POLICY "Public read active categories"
    ON public.categories FOR SELECT USING (is_active = true);

CREATE POLICY "Public read category_translations"
    ON public.category_translations FOR SELECT USING (true);

CREATE POLICY "Public read game_translations"
    ON public.game_translations FOR SELECT USING (true);

CREATE POLICY "Public read game_categories"
    ON public.game_categories FOR SELECT USING (true);

CREATE POLICY "Public read game_providers"
    ON public.game_providers FOR SELECT USING (is_active = true);

-- Authenticated user policies (service key bypasses these)
CREATE POLICY "Users read own favorites"
    ON public.user_favorites FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users read own likes"
    ON public.user_likes FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users read own ratings"
    ON public.game_ratings FOR SELECT USING (auth.uid() = user_id);

-- ── 9. SEED DATA ─────────────────────────────────────────────

-- GameDistribution provider
INSERT INTO public.game_providers (id, name, slug, api_base_url, is_active)
VALUES (1, 'GameDistribution', 'game-distribution', 'https://api.gamedistribution.com', true)
ON CONFLICT (id) DO NOTHING;

-- Default categories
INSERT INTO public.categories (id, slug, icon, sort_order, is_active) VALUES
(1, 'action',    'bolt',        1, true),
(2, 'puzzle',    'puzzle-piece', 2, true),
(3, 'racing',    'truck',       3, true),
(4, 'sports',    'trophy',      4, true),
(5, 'shooting',  'crosshairs',  5, true),
(6, 'strategy',  'chess-board', 6, true),
(7, 'adventure', 'map',         7, true),
(8, 'arcade',    'gamepad',     8, true)
ON CONFLICT (id) DO NOTHING;

-- Category translations — English
INSERT INTO public.category_translations (category_id, language_code, name) VALUES
(1, 'en', 'Action'),
(2, 'en', 'Puzzle'),
(3, 'en', 'Racing'),
(4, 'en', 'Sports'),
(5, 'en', 'Shooting'),
(6, 'en', 'Strategy'),
(7, 'en', 'Adventure'),
(8, 'en', 'Arcade')
ON CONFLICT (category_id, language_code) DO NOTHING;

-- Category translations — Thai
INSERT INTO public.category_translations (category_id, language_code, name) VALUES
(1, 'th', 'แอคชั่น'),
(2, 'th', 'ปริศนา'),
(3, 'th', 'แข่งรถ'),
(4, 'th', 'กีฬา'),
(5, 'th', 'ยิงปืน'),
(6, 'th', 'กลยุทธ์'),
(7, 'th', 'ผจญภัย'),
(8, 'th', 'อาร์เคด')
ON CONFLICT (category_id, language_code) DO NOTHING;

-- Scraper schedule for GameDistribution (daily 2:00 AM UTC)
INSERT INTO public.scraper_schedules (provider_id, cron_expression, is_enabled)
VALUES (1, '0 0 2 * * ?', true)
ON CONFLICT (provider_id) DO NOTHING;

-- ── ADMIN USER ───────────────────────────────────────────────
-- Admin login uses Supabase Auth directly (no custom table).
-- Create admin via Supabase dashboard: Authentication → Users → Invite user
-- Then set their role or use the service key in your app config.
