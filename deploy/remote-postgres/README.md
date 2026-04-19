# Remote PostgreSQL for Birds

This folder contains a minimal PostgreSQL Compose setup for the remote sync backend.

## Recommended usage

Run Docker Compose from the repository root so it can use the root `.env` file:

```powershell
docker compose -f deploy/remote-postgres/docker-compose.yml --env-file .env up -d
```

Check status:

```powershell
docker compose -f deploy/remote-postgres/docker-compose.yml --env-file .env ps
```

View logs:

```powershell
docker compose -f deploy/remote-postgres/docker-compose.yml --env-file .env logs -f postgres
```

Stop:

```powershell
docker compose -f deploy/remote-postgres/docker-compose.yml --env-file .env down
```

## Required variables

These values come from the root `.env` file:

- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `POSTGRES_PORT` (optional, defaults to `5432`)
- `POSTGRES_TIMEZONE` (optional, defaults to `UTC`)

## Migration flow for your current plan

1. Start a fresh PostgreSQL instance with Docker Compose.
2. Point `Birds.App/appsettings.json` to the new server host if needed.
3. Launch the app.
4. Import the snapshot through the app into local SQLite.
5. Let remote sync push the imported data to PostgreSQL.

## Time zone note

`POSTGRES_TIMEZONE` mainly affects PostgreSQL session/log/display behavior inside the container.

For the Birds app itself:

- `Arrival` / `Departure` are stored as `DateOnly`, so server timezone does not affect them.
- Remote sync tombstones use UTC-oriented timestamps.
- Many created/updated timestamps shown in the UI come from the app/runtime side, not from Docker container timezone alone.

So for stability, `UTC` is the safest default for the database container.
