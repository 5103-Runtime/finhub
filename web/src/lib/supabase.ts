import { createClient } from "@supabase/supabase-js";
import env from "./env";

export const supabase = createClient(
  env.SUPABASE_URL,
  env.SUPABASE_PUBLISHABLE_KEY,
  {
    auth: {
      persistSession: true,
      storage: window.sessionStorage, // Tell Supabase to use sessionStorage instead of localStorage
      autoRefreshToken: true, // Tokens refresh during the tab session
      detectSessionInUrl: true, // Helps if the application has auth redirects/magic links
    },
  },
);
