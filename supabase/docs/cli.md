# Install and run the CLI

The CLI can run through `npx`; it does not need to be installed globally or added to `web/package.json`.

Run these commands from the repository root:

```powershell
npx supabase@latest --version
npx supabase@latest login
npx supabase@latest init
npx supabase@latest link --project-ref bwvnqbuxainfmeumnjci
```

`login` authenticates the CLI and opens a browser. `init` creates the local Supabase configuration. Run it once per repository. `link` associates this repository with the Supabase project and is normally run once per development machine.

If `supabase/config.toml` already exists, do not run `init` again.
