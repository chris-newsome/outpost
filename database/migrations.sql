-- Supabase Postgres schema for Outpost
-- Extensions
create extension if not exists pgcrypto;

-- Families
create table if not exists public.families (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  created_at timestamptz not null default now()
);

-- Memberships (link to Supabase auth.users)
create table if not exists public.memberships (
  family_id uuid not null references public.families(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  role text not null default 'member',
  primary key (family_id, user_id)
);

-- Tasks
create table if not exists public.tasks (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  title text not null,
  description text,
  due_date timestamptz,
  completed boolean not null default false,
  assigned_to uuid references auth.users(id),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create index if not exists idx_tasks_family_due on public.tasks (family_id, due_date);

-- Bills
create table if not exists public.bills (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  vendor text not null,
  amount numeric(12,2) not null check (amount >= 0),
  due_date timestamptz not null,
  status text not null default 'pending',
  category text,
  recurring boolean not null default false,
  subscription_id uuid,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create index if not exists idx_bills_family_due_status on public.bills (family_id, due_date, status);

-- Documents
create table if not exists public.documents (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  name text not null,
  content_type text,
  storage_path text not null,
  uploaded_by uuid references auth.users(id),
  created_at timestamptz not null default now()
);
create index if not exists idx_documents_family_created on public.documents (family_id, created_at desc);

-- Subscriptions
create table if not exists public.subscriptions (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  name text not null,
  amount numeric(12,2) not null,
  interval text not null default 'monthly',
  next_due_date timestamptz,
  linked_bill_id uuid references public.bills(id)
);
create index if not exists idx_subscriptions_family_next_due on public.subscriptions (family_id, next_due_date);

-- Finance integrations
create table if not exists public.finance_items (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  provider text not null check (provider in ('plaid','finicity')),
  item_id text not null,
  access_token_encrypted text,
  linked_at timestamptz not null default now()
);
create index if not exists idx_finance_items on public.finance_items (family_id, provider, item_id);

create table if not exists public.finance_accounts (
  id uuid primary key default gen_random_uuid(),
  family_id uuid not null references public.families(id) on delete cascade,
  account_id text not null,
  name text not null,
  type text,
  subtype text,
  balance numeric(14,2) not null default 0
);
create index if not exists idx_finance_accounts on public.finance_accounts (family_id, account_id);

-- RLS policies
alter table public.families enable row level security;
alter table public.memberships enable row level security;
alter table public.tasks enable row level security;
alter table public.bills enable row level security;
alter table public.documents enable row level security;
alter table public.subscriptions enable row level security;
alter table public.finance_items enable row level security;
alter table public.finance_accounts enable row level security;

create or replace function public.is_family_member(fid uuid)
returns boolean
language sql
security definer
stable
as $$
  select exists(
    select 1 from public.memberships m
    where m.family_id = fid and m.user_id = auth.uid()
  );
$$;

-- Families
drop policy if exists "families_select" on public.families;
create policy "families_select" on public.families for select using (
  exists(select 1 from public.memberships m where m.family_id = id and m.user_id = auth.uid())
);

-- Memberships
drop policy if exists "memberships_select" on public.memberships;
create policy "memberships_select" on public.memberships for select using (user_id = auth.uid());

-- Tasks
drop policy if exists "tasks_all" on public.tasks;
create policy "tasks_all" on public.tasks
  for all
  using (public.is_family_member(family_id))
  with check (public.is_family_member(family_id));

-- Bills
drop policy if exists "bills_all" on public.bills;
create policy "bills_all" on public.bills
  for all
  using (public.is_family_member(family_id))
  with check (public.is_family_member(family_id));

-- Documents
drop policy if exists "documents_all" on public.documents;
create policy "documents_all" on public.documents
  for all
  using (public.is_family_member(family_id))
  with check (public.is_family_member(family_id));

-- Subscriptions
drop policy if exists "subscriptions_all" on public.subscriptions;
create policy "subscriptions_all" on public.subscriptions
  for all
  using (public.is_family_member(family_id))
  with check (public.is_family_member(family_id));

-- Finance
drop policy if exists "finance_items_all" on public.finance_items;
create policy "finance_items_all" on public.finance_items
  for all
  using (public.is_family_member(family_id))
  with check (public.is_family_member(family_id));

drop policy if exists "finance_accounts_all" on public.finance_accounts;
create policy "finance_accounts_all" on public.finance_accounts
  for all
  using (public.is_family_member(family_id))
  with check (public.is_family_member(family_id));

-- Storage bucket (run on Supabase; may require elevated role)
-- insert into storage.buckets (id, name, public) values ('documents','documents', false)
-- on conflict (id) do nothing;
