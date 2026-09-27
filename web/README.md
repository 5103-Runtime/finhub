# Web
React + TypeScript app built with Vite.

## Conventions
- Call the API with relative paths (`/api/...`), never a hardcoded host. In development, Vite proxies them to the development API.
- Install with `npm ci`, not `npm install`. [Why?](https://docs.npmjs.com/cli/commands/npm-ci)
- CI fails on lint errors; run `npm run lint` before pushing.

See the [root README](../README.md) to run it.
