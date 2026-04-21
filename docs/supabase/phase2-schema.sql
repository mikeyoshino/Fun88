-- Phase 2 schema additions for Fun88

-- user_likes table (not in Phase 1 schema)
CREATE TABLE user_likes (
  user_id    uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  game_id    uuid        NOT NULL REFERENCES games(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, game_id)
);

-- Add last_login_at to users (updated by UserSyncService on each login)
ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_at timestamptz;
