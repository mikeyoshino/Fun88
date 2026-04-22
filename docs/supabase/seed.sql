-- Fun88 Supabase Seed Data
-- Run this in the Supabase SQL editor after schema.sql

-- GameDistribution provider
INSERT INTO game_providers (id, name, slug, api_base_url, is_active)
VALUES (1, 'GameDistribution', 'game-distribution', 'https://api.gamedistribution.com', true)
ON CONFLICT (id) DO NOTHING;

-- Default categories
INSERT INTO categories (id, slug, icon, sort_order, is_active) VALUES
(1, 'action',    'bolt',        1,  true),
(2, 'puzzle',    'puzzle-piece', 2,  true),
(3, 'racing',    'truck',       3,  true),
(4, 'sports',    'trophy',      4,  true),
(5, 'shooting',  'crosshairs',  5,  true),
(6, 'strategy',  'chess-board', 6,  true),
(7, 'adventure', 'map',         7,  true),
(8, 'arcade',    'gamepad',     8,  true)
ON CONFLICT (id) DO NOTHING;

-- Category translations — English
INSERT INTO category_translations (category_id, language_code, name) VALUES
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
INSERT INTO category_translations (category_id, language_code, name) VALUES
(1, 'th', 'แอคชั่น'),
(2, 'th', 'ปริศนา'),
(3, 'th', 'แข่งรถ'),
(4, 'th', 'กีฬา'),
(5, 'th', 'ยิงปืน'),
(6, 'th', 'กลยุทธ์'),
(7, 'th', 'ผจญภัย'),
(8, 'th', 'อาร์เคด')
ON CONFLICT (category_id, language_code) DO NOTHING;

-- Scraper schedule for GameDistribution (daily at 2:00 AM UTC)
INSERT INTO scraper_schedules (provider_id, cron_expression, is_enabled)
VALUES (1, '0 0 2 * * ?', true)
ON CONFLICT (provider_id) DO NOTHING;
-- Create the first admin via Supabase Auth dashboard or this SQL (replace hash with bcrypt):
--
--   INSERT INTO admin_users (id, email, password_hash, display_name, created_at)
--   VALUES (gen_random_uuid(), 'admin@fun88.com', '<bcrypt-hash>', 'Admin', now());
--
-- Generate bcrypt hash locally:
--   dotnet script -e "using BCrypt.Net; Console.WriteLine(BCrypt.HashPassword(\"YOUR_PASSWORD\"));"
-- Or use https://bcrypt-generator.com (cost factor 11+)
