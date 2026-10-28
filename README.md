# JajkoBackend

Backend for the Jajko shop. API reference: [docs.md](docs.md). Known problems: [issues.md](issues.md).

## Running

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run
```

The API runs at `http://localhost:5097`. Use `dotnet watch` instead to restart it on code changes.

- Nothing to configure: the development settings (including the JWT key) are in `appsettings.Development.json`.
- The database is SQLite in `data/jajko.db`. Migrations are applied on startup.
- The OpenAPI spec is at `http://localhost:5097/openapi/v1.json`.
- To reset the database, restore it with `git checkout data/jajko.db`, or delete the file to start with an empty one.

## Calling it from the frontend

CORS isn't configured, so proxy `/api` through your dev server. For Vite:

```js
// vite.config.js
export default {
  server: {
    proxy: { '/api': 'http://localhost:5097' }
  }
}
```

Then call `/api/...` without a host, e.g. `fetch('/api/items')`.
