Supabase configuration for Outpost

Auth
- Enable Email/Password in Authentication → Providers.
- Enable Apple in Authentication → Providers:
  - Services ID, Team ID, Key ID, and Key file from Apple Developer.
  - Callback URL: https://<project>.supabase.co/auth/v1/callback

Database
- Run the SQL in `database/migrations.sql` in the SQL editor to create schemas and RLS policies.
- Optionally seed a family and membership for development.

Storage
- Create bucket `documents` (private) in Storage.
- Optional: Add a policy to allow signed URL access only.

Realtime
- Enable Realtime for the `public` schema.
- Optionally, create broadcasts for updates to tasks/bills/documents.

Environment
- Frontend env:
  - VITE_SUPABASE_URL
  - VITE_SUPABASE_ANON_KEY
  - VITE_API_BASE (e.g., http://localhost:5000)
- Backend env:
  - SUPABASE_DB_CONNECTION (Postgres connection for EF Core if used)
  - SUPABASE_JWT_ISSUER, SUPABASE_JWT_AUDIENCE (optional for JWT validation)
  - BACKEND_JWT_SECRET (for backend-signed tokens)
  - PLAID_CLIENT_ID, PLAID_SECRET (if using Plaid)
  - FINICITY_PARTNER_ID, FINICITY_SECRET (if using Finicity)

Notes
- Prefer Supabase Auth for identity. The backend validates JWTs (Supabase scheme) and authorizes per family membership.
- Supabase Storage signed URLs are recommended for document access.
- Use RLS to enforce per-family data access (done in SQL).
