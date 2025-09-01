-- AI chat sessions + messages
create table if not exists ai_chat_sessions (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null,
  created_at timestamptz default now(),
  title text
);

create table if not exists ai_chat_messages (
  id uuid primary key default gen_random_uuid(),
  session_id uuid references ai_chat_sessions(id) on delete cascade,
  family_id uuid not null,
  role text check (role in ('system','user','assistant','tool')) not null,
  content jsonb not null,
  created_at timestamptz default now()
);

-- embeddings for retrieval
create table if not exists ai_embeddings (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null,
  src text not null,
  src_id uuid,
  chunk text not null,
  embedding vector(1536),
  created_at timestamptz default now()
);
create index if not exists ai_embeddings_ivfflat on ai_embeddings using ivfflat (embedding vector_cosine_ops);
create index if not exists ai_embeddings_family_src on ai_embeddings (family_id, src);

-- behavior summaries
create table if not exists ai_behavior_summaries (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null,
  summary text not null,
  embedding vector(1536),
  period tstzrange not null,
  created_at timestamptz default now()
);

