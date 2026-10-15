# Authentication and profiles

Supabase Auth owns credentials and password hashing. The `public.profiles` table is not the place for financial data; it stores application profile data such as:

- `first_name`
- `last_name`
- `preferred_name`
- `date_of_birth`
  - Date of birth is private personal information, so the table is protected by RLS and should not be publicly readable.
- `avatar_url`
- timestamps

These fields are nullable so registration can begin with only an email and password.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.profiles` exists.
2. Open the table policies and confirm Row Level Security is enabled.
3. Confirm the policies restrict users to their own profile.
4. In **Authentication**, confirm users remain managed by Supabase Auth.
5. Register a test user later and confirm the profile trigger creates a matching profile row.
