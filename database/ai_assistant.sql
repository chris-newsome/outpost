-- Enable pgvector and create AI assistant tables
create extension if not exists vector;

-- sessions
create table if not exists public.ai_chat_sessions (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  created_at timestamptz default now(),
  title text
);

create table if not exists public.ai_chat_messages (
  id uuid primary key default gen_random_uuid(),
  session_id uuid references public.ai_chat_sessions(id) on delete cascade,
  family_id uuid not null references public.families(id) on delete cascade,
  role text check (role in ('system','user','assistant','tool')) not null,
  content jsonb not null,
  created_at timestamptz default now()
);

create table if not exists public.ai_embeddings (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  src text not null,
  src_id uuid,
  chunk text not null,
  embedding vector(1536),
  created_at timestamptz default now()
);
create index if not exists ai_embeddings_ivfflat on public.ai_embeddings using ivfflat (embedding vector_cosine_ops);
create index if not exists ai_embeddings_family_src on public.ai_embeddings (family_id, src);

create table if not exists public.ai_behavior_summaries (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  summary text not null,
  embedding vector(1536),
  period tstzrange not null,
  created_at timestamptz default now()
);

