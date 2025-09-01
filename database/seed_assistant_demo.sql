-- Seed demo family and data for assistant tests
insert into public.families (id, name) values ('11111111-1111-1111-1111-111111111111','Demo Family') on conflict do nothing;

-- Use your own user uuid from auth.users in memberships
-- insert into public.memberships (family_id, user_id, role) values ('11111111-1111-1111-1111-111111111111','YOUR-USER-UUID','owner');

insert into public.tasks (family_id, title, description, due_date)
values
('11111111-1111-1111-1111-111111111111','Grocery run','Buy milk and eggs', now() + interval '2 days'),
('11111111-1111-1111-1111-111111111111','Car service','Oil change at Jiffy', now() + interval '5 days')
on conflict do nothing;

insert into public.bills (family_id, vendor, amount, due_date, status)
values
('11111111-1111-1111-1111-111111111111','Comcast', 89.99, now() + interval '6 days', 'pending'),
('11111111-1111-1111-1111-111111111111','Spotify', 14.99, now() + interval '3 days', 'pending')
on conflict do nothing;

insert into public.documents (family_id, name, content_type, storage_path)
values
('11111111-1111-1111-1111-111111111111', 'Car_Insurance_Policy.pdf', 'application/pdf', 'documents/demo/car_policy.pdf')
on conflict do nothing;

