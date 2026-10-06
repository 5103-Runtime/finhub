import { createClient } from "@supabase/supabase-js";
import env from "./env";

export const supabase = createClient(
  env.SUPABASE_URL,
  env.SUPABASE_PUBLISHABLE_KEY,
  {
    auth: {
      persistSession: true, // Turn persistence back on
      storage: window.sessionStorage, // Tell Supabase to use sessionStorage instead of localStorage
      autoRefreshToken: true, // Usually good to keep true so tokens refresh during the tab session
      detectSessionInUrl: true, // Helps if you have auth redirects/magic links
    },
  },
);
