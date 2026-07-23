# Arlo Chat

A real-time 1:1 and group chat application with a .NET 8 Web API backend and an Angular 17 frontend. Users can register, add friends, and exchange messages instantly over SignalR, with JWT-based authentication and CSRF-protected session cookies.

## Features

- **Authentication** – register/login with hashed passwords, short-lived JWT access tokens delivered as HttpOnly cookies, and rotating refresh tokens with family revocation on logout.
- **CSRF protection** – a readable CSRF cookie paired with a custom header, validated on state-changing requests.
- **Friends** – search for users, send/accept/reject friend requests, and list friends with cursor-based pagination.
- **Real-time messaging** – SignalR hub (`/hubs/chat`) for direct messages, group conversations, conversation creation/update events, presence tracking, and friend-list change notifications.
- **Conversation history** – paginated REST endpoints for fetching a user's conversations and message history.
- **Inactivity check** – a background service that scans for users inactive for 3+ days (email delivery is currently stubbed/logged).

## Tech Stack

**Backend** (`backend/Arlo_chat.Api`)
- ASP.NET Core 8 Web API
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- SignalR for real-time messaging
- JWT Bearer authentication (token read from an HttpOnly cookie)
- AutoMapper, Swashbuckle (Swagger, dev only)
- Azure Key Vault for production secrets (via Managed Identity)

**Frontend** (`frontend/Arlo-chat-client`)
- Angular 17 (standalone components)
- `@microsoft/signalr` client
- RxJS

## Project Structure

```
Arlo-Chat/
├── backend/
│   └── Arlo_chat.Api/
│       ├── Controllers/     # Auth, User, Chat, Ping REST endpoints
│       ├── Hubs/             # ChatHub (SignalR) + presence tracking
│       ├── Data/             # DbContext, entities, repositories, EF migrations
│       ├── Models/           # DTOs / request models
│       ├── Security/         # JWT, cookie, and CSRF configuration
│       └── Services/         # Auth, user, chat, and background services
└── frontend/
    └── Arlo-chat-client/
        └── src/app/
            ├── components/   # login, create-account, home, manage-friends, ...
            ├── services/     # auth, chat API, chat hub, friend API, user API
            ├── guards/       # auth/guest route guards
            └── interceptors/ # CSRF header, auth-error handling
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [Angular CLI](https://angular.dev/tools/cli) (`npm install -g @angular/cli`)
- A PostgreSQL database
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) tool (restored automatically via `.config/dotnet-tools.json`)

## Getting Started

### 1. Backend setup

```bash
cd backend/Arlo_chat.Api
dotnet tool restore
```

Configure the required secrets with `dotnet user-secrets` (development) rather than committing them to `appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=arlochat;Username=postgres;Password=<your-password>"
dotnet user-secrets set "Jwt:SigningKey" "<a-long-random-secret>"
```

Other `Jwt` settings (`Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays`) already have sensible defaults in `appsettings.json`. Optionally set `Cors:AllowedOrigin` if the frontend isn't served from `http://localhost:4200`.

Apply database migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run
```

The API listens on the URLs configured in `Properties/launchSettings.json` (Swagger UI is available at `/swagger` in development).

### 2. Frontend setup

```bash
cd frontend/Arlo-chat-client
npm install
npm start
```

This serves the Angular app at `http://localhost:4200` and proxies `/api` and `/hubs` requests to `https://localhost:7281` (see `proxy.conf.json`) — update the target there if your backend runs on a different port.

## Configuration Reference

| Setting | Location | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | user-secrets / env | PostgreSQL connection string |
| `Jwt:SigningKey` | user-secrets / env | Symmetric key used to sign access tokens |
| `Jwt:Issuer`, `Jwt:Audience` | `appsettings.json` | JWT validation parameters |
| `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | `appsettings.json` | Token lifetimes |
| `Cookie:SameSite` | `appsettings.json` | `SameSite` mode for session cookies |
| `Cors:AllowedOrigin` | `appsettings.json` | Origin allowed to call the API with credentials |
| `KeyVault:Uri` | environment (non-dev only) | Azure Key Vault used to load production secrets via Managed Identity |

## Deployment

A `Dockerfile` is provided for the backend (`backend/Arlo_chat.Api/Dockerfile`), which builds and runs the API on the port from the `PORT` environment variable (defaults to `8080`). The frontend's production build (`ng build`) targets the API URL configured in `src/environments/environment.prod.ts`.

## Database Migrations

EF Core migrations live in `backend/Arlo_chat.Api/Data/Migrations`. To add a new migration after changing entities:

```bash
cd backend/Arlo_chat.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```
