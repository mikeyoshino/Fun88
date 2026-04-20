-- Supabase PostgreSQL Schema for Fun88
-- Applies the schema originally meant for EF Core, mapping exactly to our Postgrest.Models

-- Categories
CREATE TABLE public.categories (
    id SERIAL PRIMARY KEY,
    slug VARCHAR(255) NOT NULL UNIQUE,
    icon VARCHAR(255),
    sort_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Category Translations
CREATE TABLE public.category_translations (
    category_id INT NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    language_code VARCHAR(10) NOT NULL,
    name TEXT NOT NULL,
    PRIMARY KEY (category_id, language_code)
);

-- Game Providers
CREATE TABLE public.game_providers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    api_base_url VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Games
CREATE TABLE public.games (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug VARCHAR(255) NOT NULL UNIQUE,
    provider_id INT REFERENCES public.game_providers(id) ON DELETE SET NULL,
    provider_game_id VARCHAR(255),
    game_url TEXT NOT NULL,
    thumbnail_url TEXT NOT NULL,
    play_count BIGINT NOT NULL DEFAULT 0,
    like_count BIGINT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Ensure a provider's game ID is unique
CREATE UNIQUE INDEX idx_games_provider_game_id ON public.games(provider_id, provider_game_id) WHERE provider_game_id IS NOT NULL;

-- Game Translations
CREATE TABLE public.game_translations (
    game_id UUID NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    language_code VARCHAR(10) NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    control_description TEXT,
    meta_title TEXT,
    meta_description TEXT,
    PRIMARY KEY (game_id, language_code)
);

-- Game Categories M2M
CREATE TABLE public.game_categories (
    game_id UUID NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    category_id INT NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    PRIMARY KEY (game_id, category_id)
);

-- Scraper Jobs
CREATE TABLE public.scraper_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider_id INT NOT NULL REFERENCES public.game_providers(id) ON DELETE CASCADE,
    triggered_by VARCHAR(50) NOT NULL, -- 'schedule', 'manual'
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'running', 'completed', 'failed'
    games_found INT NOT NULL DEFAULT 0,
    games_imported INT NOT NULL DEFAULT 0,
    games_skipped INT NOT NULL DEFAULT 0,
    error_message TEXT,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ
);

-- Translation Jobs
CREATE TABLE public.translation_jobs (
    game_id UUID NOT NULL REFERENCES public.games(id) ON DELETE CASCADE,
    language_code VARCHAR(10) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'completed', 'failed'
    attempt_count SMALLINT NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    PRIMARY KEY (game_id, language_code)
);

-- RLS (Row Level Security) - By default allow public read, admin write
-- You can configure these policies in Supabase dashboard
ALTER TABLE public.categories ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.category_translations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.games ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_translations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_providers ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.game_categories ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Public read games" ON public.games FOR SELECT USING (is_active = true);
CREATE POLICY "Public read categories" ON public.categories FOR SELECT USING (is_active = true);
-- Add more policies as needed...
