const env = {
  SUPABASE_URL: import.meta.env.VITE_SUPABASE_URL,
  SUPABASE_PUBLISHABLE_KEY: import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY,
};

if (!env.SUPABASE_URL || !env.SUPABASE_PUBLISHABLE_KEY) {
  throw new Error("Missing VITE_SUPABASE environment variables in .env file.");
}

export default env;
