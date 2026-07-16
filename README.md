# ChatApp

Browser chatroom with a decoupled stock-quote bot, built on ASP.NET Core, SignalR,
PostgreSQL, and RabbitMQ.

## Solution layout

```
src/
  ChatApp.Contracts/   shared DTOs + the /stock= command parser (no dependencies)
  ChatApp.Web/          ASP.NET Core: SignalR hub, auth endpoints, EF Core, static frontend
  ChatApp.Bot/          Worker service: consumes stock.requests, calls Yahoo Finance, publishes stock.responses
tests/
  ChatApp.Tests/        xUnit tests for the command parser, the quote parser, and the validators
docker-compose.yml       Postgres + RabbitMQ + both apps
```

## Prerequisites

This project runs the same way on macOS and Windows - .NET, Docker, and Postgres/RabbitMQ
are all cross-platform. The commands below differ mainly in shell syntax; both are given
throughout.

**macOS**
- .NET 8 SDK: `brew install --cask dotnet-sdk` (or the installer from dotnet.microsoft.com)
- Docker Desktop or Rancher Desktop, with the container engine set to dockerd/moby
- The EF Core CLI tool:
  ```
  dotnet tool install --global dotnet-ef
  ```
  Global tools install to `~/.dotnet/tools`, which isn't always on `PATH` by default.
  If `dotnet ef --version` isn't found after installing, add it:
  ```
  echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc
  source ~/.zshrc
  ```

**Windows**
- .NET 8 SDK: `winget install Microsoft.DotNet.SDK.8` (or the installer from dotnet.microsoft.com)
- Docker Desktop or Rancher Desktop, using the WSL2 backend
- The EF Core CLI tool, from PowerShell:
  ```
  dotnet tool install --global dotnet-ef
  ```
  Global tools install to `%USERPROFILE%\.dotnet\tools`. The .NET SDK installer normally
  adds this to `PATH` automatically; if `dotnet ef --version` isn't found, open a **new**
  PowerShell window first (PATH changes don't apply to already-open terminals). If it's
  still missing, add it for your user account:
  ```
  [Environment]::SetEnvironmentVariable("Path", "$env:Path;$env:USERPROFILE\.dotnet\tools", "User")
  ```
  then open a new terminal.

**Both platforms**
- Verify the tool is installed and on `PATH`:
  ```
  dotnet ef --version
  ```
- If you already have an older `dotnet-ef` installed, update it instead:
  ```
  dotnet tool update --global dotnet-ef
  ```

## First-time setup

### 1. Restore and build

```
dotnet restore
dotnet build
```

### 2. Start the infra (Postgres + RabbitMQ) first

Set a Postgres password and keep it consistent across this session (add it to your
shell profile, or drop it in a `.env` file in the repo root, which docker-compose
auto-loads on any OS - the `.env` approach is the simplest cross-platform option):

macOS/Linux (bash/zsh):
```
export POSTGRES_PASSWORD=devpassword
```
Windows (PowerShell):
```
$env:POSTGRES_PASSWORD = "devpassword"
```

Start just the infra containers, in their own terminal:
```
docker compose up postgres rabbitmq
```

Verify Postgres is accepting connections (empty table list, no error, is expected
before the migration below has run):
```
docker exec -it chatapp-postgres-1 psql -U chatapp -d chatapp -c "\dt"
```
(Container name may differ - check `docker ps` if that doesn't resolve.)

Verify RabbitMQ is up by opening the management UI at `http://localhost:15672`
(login `guest` / `guest`). No queues will exist yet - `stock.requests` and
`stock.responses` are created the first time Web or Bot actually runs.

### 3. Point the app at this infra via user secrets (never committed)

```
cd src/ChatApp.Web
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=chatapp;Username=chatapp;Password=devpassword"
dotnet user-secrets set "RabbitMq:UserName" "guest"
dotnet user-secrets set "RabbitMq:Password" "guest"
cd ../..
```

### 4. Create the initial EF Core migration and apply it

From the repo root, with the infra from step 2 still running:
```
dotnet ef migrations add InitialCreate --project src/ChatApp.Web --startup-project src/ChatApp.Web
dotnet ef database update --project src/ChatApp.Web --startup-project src/ChatApp.Web
```
(`Program.cs` also calls `db.Database.Migrate()` on startup, so this step is really
just to generate the migration files the first time - after that, `dotnet run` keeps
the schema current.) Re-run the `\dt` check from step 2 afterward - you should now
see the `Users` and `Messages` tables.

### Infra troubleshooting

| Symptom | Likely cause |
|---|---|
| `psql` connection refused | Postgres container still starting - wait a few seconds, or check `docker compose logs postgres` |
| Web app can't connect to Postgres, "password authentication failed" | `POSTGRES_PASSWORD` env var and the user-secret connection string don't match |
| RabbitMQ management UI won't load | Port 15672 already in use locally, or container still starting - check `docker compose logs rabbitmq` |
| `docker compose up` immediately exits | Check you're in the repo root (where `docker-compose.yml` lives) |
| `role "chatapp" does not exist` from `dotnet ef`, but `psql` *inside* the container connects fine as `chatapp` | Something else on your machine is already bound to port 5432, so `dotnet ef` on the host is talking to that instead of the Docker container. **macOS**: check with `brew services list`, confirm with `lsof -nP -iTCP:5432 -sTCP:LISTEN`, stop it with `brew services stop postgresql` (or `brew uninstall postgresql`). **Windows**: check with `Get-NetTCPConnection -LocalPort 5432 -State Listen` (or `netstat -ano \| findstr :5432` then look up the PID in Task Manager), and if it's a Postgres Windows Service, stop it via `services.msc` or `Stop-Service postgresql-x64-16` (name varies by version). Either OS: once resolved, run `docker compose down -v && docker compose up postgres rabbitmq` to reinitialize cleanly |
| `role "chatapp" does not exist` even after fixing the port conflict, or right after first bringing up `postgres` | The named volume (`chatapp_pgdata`) already existed from an earlier run before `POSTGRES_PASSWORD` was set correctly - Postgres only runs its init (creating the role/db) against an empty data directory. Fix: `docker compose down -v` (the `-v` deletes the volume), export `POSTGRES_PASSWORD` again, then `docker compose up postgres rabbitmq` |
| `Did not find any relations` from `\dt` | Not an error - it just means the DB connection works but no migration has been applied yet. Continue to step 4 |
| `password authentication failed for user "chatapp"` when running `dotnet run` (not `dotnet ef`) | `ASPNETCORE_ENVIRONMENT` isn't set to `Development`, so ASP.NET Core never loads your user secrets and falls back to the placeholder password in `appsettings.json`. `src/ChatApp.Web/Properties/launchSettings.json` sets this automatically for `dotnet run` - if you're instead running the built DLL directly (`dotnet bin/Debug/net8.0/ChatApp.Web.dll`) or from Docker, set it explicitly: macOS/Linux `export ASPNETCORE_ENVIRONMENT=Development`, Windows `$env:ASPNETCORE_ENVIRONMENT = "Development"` |

## Running for a demo

Terminal 1 - infra:

macOS/Linux:
```
POSTGRES_PASSWORD=devpassword docker compose up postgres rabbitmq
```
Windows (PowerShell):
```
$env:POSTGRES_PASSWORD = "devpassword"
docker compose up postgres rabbitmq
```

Terminal 2 - web app:
```
cd src/ChatApp.Web
dotnet run
```

Terminal 3 - bot:
```
cd src/ChatApp.Bot
dotnet run
```

Then open `http://localhost:5000` (or whatever port `dotnet run` prints) in two
different browser windows/profiles, register two different users, and:
- send plain messages back and forth (join the same room - e.g. leave "Room" as
  `general` in both windows)
- send `/stock=aapl.us` from one window and watch StockBot's reply land in both
- open a third window in a different room (e.g. `random`) and confirm none of the
  `general` room's messages or stock replies show up there
- optionally Ctrl+C the bot terminal mid-demo to show the chat keeps working without it

## Running everything in Docker instead

macOS/Linux:
```
POSTGRES_PASSWORD=devpassword docker compose up --build
```
Windows (PowerShell):
```
$env:POSTGRES_PASSWORD = "devpassword"
docker compose up --build
```

Scale the bot to demonstrate the competing-consumers pattern:
```
docker compose up --scale chatapp-bot=3
```

## Running the tests

```
dotnet test
```

## Account rules

Enforced server-side in `Validation/CredentialValidator.cs` (the frontend mirrors the
same regexes for instant feedback, but the server is what actually decides):

- **Username**: 3-20 characters, must start with a letter, then letters/numbers/`.`/`_`/`-`.
- **Password**: at least 8 characters, including at least one letter and one number.

Both are shown as hint text under the fields on the login screen, and violations
return a specific 400 error describing which rule failed.

## Security notes

- **Rate limiting on auth endpoints**: `/api/auth/register` and `/api/auth/login`
  are limited to 5 requests per minute, partitioned per client IP (`Program.cs`,
  `AddRateLimiter`/`RequireRateLimiting("auth")`). This is the app's one unauthenticated
  surface, so it's the realistic target for brute-force or registration-spam attempts.
  Exceeding it returns `429 Too Many Requests`; the frontend shows this as "Too many
  attempts - please wait a minute and try again."
- **Rate limiting on the chat hub**: `ChatHub.SendMessage` allows at most 10 calls per
  10 seconds per connection (in-memory, per-`ConnectionId`). This protects against a
  single client flooding the chatroom or - more importantly - flooding RabbitMQ and the
  Yahoo Finance API with rapid-fire `/stock=` commands. Caveat: this state is per-process, so if
  the app is ever scaled to multiple Web instances (see the SignalR backplane discussion
  above), it would need to move to a shared store like Redis to stay effective across
  instances - noted in a comment in `ChatHub.cs`.
- Passwords are hashed with BCrypt, never stored or logged in plaintext.
- Secrets (DB connection string, RabbitMQ credentials) are never committed - see the
  `dotnet user-secrets` steps above and `.gitignore`.
- Chat message content is inserted via `textContent`, not `innerHTML`, on the frontend
  (`app.js`), which prevents stored XSS from a malicious message body.

## Notes

- **Multiple chatrooms**: users pick a room name at login (defaults to `general`,
  validated by `RoomNameValidator.cs` - 1-30 characters, letters/numbers/`.`/`_`/`-`).
  `ChatHub` reads the room from the `?room=` query string on the SignalR connection,
  joins a group named after it, and scopes both message history and live broadcasts
  (`Clients.Group(roomName)`) to that group. `ChatMessage.RoomName` is persisted and
  indexed alongside `TimestampUtc` so "last 50" stays scoped per room. `/stock=` replies
  round-trip the room through `StockRequested`/`StockQuoteReady` (Bot doesn't otherwise
  care about rooms - it just carries the value through) so StockBot's answer lands back
  in the room that asked. Rooms are created implicitly - there's no separate `Rooms`
  table, no room list/directory, and no access control (anyone who knows/guesses a name
  can join it), which is the main simplification versus a "real" multi-room design.
- **Stock quote provider**: `ChatApp.Bot` originally called the Stooq CSV endpoint
  (`stooq.com/q/l/...`), but Stooq has since put its endpoints behind a JavaScript
  proof-of-work challenge (a script that must run in a real browser and POST a solved
  hash to `/__verify` before any data is returned) - a plain server-side `HttpClient`
  can never pass that gate, no matter what headers it sends. The bot now calls Yahoo
  Finance's public chart endpoint instead
  (`query1.finance.yahoo.com/v8/finance/chart/{SYMBOL}`), which returns plain JSON with
  no such challenge. See `YahooFinanceClient.cs` (request/URL building) and
  `YahooQuoteParser.cs` (response parsing). The `/stock=ticker.exchange` chat command
  format (e.g. `/stock=aapl.us`) is unchanged for users - the bot just takes the part
  before the dot and uses that as the Yahoo ticker.
- **Retry policy**: the `YahooFinanceClient` HTTP client is wrapped with a Polly retry
  policy (`Program.cs`, `GetYahooRetryPolicy`) that retries transient failures (network
  errors, timeouts, 5xx) and `429 Too Many Requests` up to 3 times with exponential
  backoff + jitter. `404` (unknown ticker) is deliberately excluded - it's a valid
  response the parser already turns into "unknown stock code", not a transient error.
- `/stock=code` commands are intentionally never persisted as chat posts - only the
  bot's resulting quote message is saved and broadcast.
- The chat history query is capped to the last 50 messages by timestamp
  (`ChatHub.OnConnectedAsync`), and the DB has an index on `TimestampUtc` to keep
  that query cheap regardless of table size.
- RabbitMQ prefetch is set to 5 on both the bot's request consumer and the web app's
  response consumer, so load spreads reasonably evenly if you scale to multiple bot instances.
