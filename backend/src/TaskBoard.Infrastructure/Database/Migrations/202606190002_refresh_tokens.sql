create table if not exists refresh_tokens
(
    id uuid primary key,
    user_id uuid not null references users(id) on delete cascade,
    token_hash text unique not null,
    expires_at timestamptz not null,
    created_at timestamptz not null,
    revoked_at timestamptz null,
    replaced_by_token_hash text null
);

create index if not exists ix_refresh_tokens_user_id on refresh_tokens(user_id);
create index if not exists ix_refresh_tokens_token_hash on refresh_tokens(token_hash);
