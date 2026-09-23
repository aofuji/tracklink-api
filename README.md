# TrackLink API

TrackLink is an ASP.NET Core backend API for creating and sharing real-time location tracking sessions through shareable tokens.

An authenticated user can create a tracking session, receive a public token, send location updates, and share that token with viewers. Public viewers can read the current location and location history without an account, while mutations remain protected by JWT authentication and ownership checks.

The project is under active development and is intended as a practical backend portfolio project using ASP.NET Core, Entity Framework Core, PostgreSQL, JWT authentication, refresh token rotation, SignalR, and integration testing.

## Main Features

- User registration and login
- JWT Bearer authentication
- Short-lived access tokens
- Refresh tokens delivered through an HttpOnly cookie, with rotation and revocation
- Logout through refresh token revocation and cookie removal
- Password hashing
- Refresh token SHA-256 hashing before database storage
- Authenticated tracking session creation
- Tracking ownership validation for updates and ending sessions
- Public tracking access through shareable tracking tokens
- Current location storage on each tracking session
- Persistent chronological location history
- Tracking expiration
- End/inactivate tracking sessions
- SignalR groups by tracking token
- Real-time `LocationUpdated` event
- Real-time `TrackingEnded` event
- Global exception handling with ProblemDetails
- Integration tests using xUnit, WebApplicationFactory, and SQLite in-memory databases

## Technologies

- C#
- .NET 10
- ASP.NET Core Web API
- ASP.NET Core Authentication/JWT Bearer
- SignalR
- Entity Framework Core
- Npgsql Entity Framework Core provider
- PostgreSQL
- Docker / Docker Compose
- xUnit
- Microsoft.AspNetCore.Mvc.Testing / WebApplicationFactory
- SQLite in-memory for integration tests

## Architecture

The API follows a small Controller + Service + DTO + EF Core structure:

```text
HTTP request
    ↓
Controller
    ↓
Service
    ↓
Entity Framework Core
    ↓
PostgreSQL
```

Main responsibilities:

- `Controllers`: HTTP endpoints, response mapping, authorization attributes
- `Services`: business flow for authentication, tokens, tracking, and history
- `DTOs`: request and response contracts exposed by the API
- `Models`: EF Core entities
- `Data`: `AppDbContext` and EF Core configuration
- `Hubs`: SignalR real-time tracking hub
- `Migrations`: EF Core database migrations

Main data relationships:

```text
User
├── Trackings
│   └── TrackingLocations
└── RefreshTokens
```

## Authentication Flow

1. A user registers with name, email, and password.
2. The password is hashed before being stored.
3. The user logs in with email and password.
4. The API returns only an access token in the response body.
5. The refresh token is sent in the `tracklink_refresh_token` cookie. It is not returned in JSON.
6. Access tokens are short-lived JWT Bearer tokens.
7. Refresh tokens are stored in the database only as SHA-256 hashes, never as raw token values.
8. Refreshing uses the cookie, rotates the refresh token, revokes the previous token, stores only the new token hash, replaces the cookie, and returns only a new access token in the response body.
9. Reusing a revoked refresh token fails.
10. Logout uses the cookie, revokes the matching refresh token, and removes/expires the cookie.

Refresh cookie attributes:

- Name: `tracklink_refresh_token`
- `HttpOnly=true`
- `SameSite=Lax`
- `Path=/api/auth`
- `Domain` is not explicitly set
- Cookie expiration follows the refresh token expiration
- `Secure=true` in production
- In local HTTP development, `Secure` may be `false`

Frontend clients do not send refresh tokens in JSON. Browser clients should send credentials when calling login, refresh, and logout so the cookie can be stored and sent.

## Tracking Flow

```text
Authenticated user
    ↓
Creates tracking session
    ↓
Receives shareable tracking token
    ↓
Sends location updates
    ↓
Latest location is updated and history is stored
    ↓
SignalR broadcasts LocationUpdated to viewers in the token group
    ↓
Public viewer follows current location and history using the token
    ↓
Owner can end the tracking session
```

Tracking sessions expire 24 hours after creation. Expired or inactive sessions cannot be updated. Public tracking access returns `404 Not Found` for unknown tokens, `409 Conflict` for inactive sessions, and `410 Gone` for expired sessions. Authenticated updates return `404 Not Found` when the token does not belong to the authenticated user.

## API Endpoints

### Authentication

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Public | Register a new user. |
| `POST` | `/api/auth/login` | Public | Login, receive an access token in the body, and receive the refresh token as an HttpOnly cookie. |
| `POST` | `/api/auth/refresh` | Public | Rotate the refresh token from the cookie and receive a new access token in the body. |
| `POST` | `/api/auth/logout` | Public | Revoke the refresh token from the cookie and remove/expire the cookie. |
| `GET` | `/api/auth/me` | Bearer JWT | Return claims for the authenticated user. |

### Tracking

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/api/tracking` | Bearer JWT | Create a tracking session for the authenticated user. |
| `GET` | `/api/tracking/my` | Bearer JWT | List tracking sessions owned by the authenticated user. |
| `GET` | `/api/tracking/{token}` | Public | Get the current tracking location by shareable token. |
| `GET` | `/api/tracking/{token}/history` | Public | Get chronological location history by shareable token. |
| `PUT` | `/api/tracking/{token}` | Bearer JWT | Update a tracking session owned by the authenticated user. |
| `DELETE` | `/api/tracking/{token}` | Bearer JWT | End a tracking session owned by the authenticated user. |

### Example Requests

Register:

```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "Test User",
  "email": "user@example.com",
  "password": "P@ssw0rd!"
}
```

Login:

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "P@ssw0rd!"
}
```

Successful login response:

```http
HTTP/1.1 200 OK
Set-Cookie: tracklink_refresh_token=<refresh-token>; expires=<date>; path=/api/auth; samesite=lax; httponly
Content-Type: application/json

{
  "accessToken": "<jwt-access-token>"
}
```

Refresh access token:

```http
POST /api/auth/refresh
Cookie: tracklink_refresh_token=<refresh-token>
```

Successful refresh response:

```http
HTTP/1.1 200 OK
Set-Cookie: tracklink_refresh_token=<new-refresh-token>; expires=<date>; path=/api/auth; samesite=lax; httponly
Content-Type: application/json

{
  "accessToken": "<new-jwt-access-token>"
}
```

Logout:

```http
POST /api/auth/logout
Cookie: tracklink_refresh_token=<refresh-token>
```

Successful logout response:

```http
HTTP/1.1 204 No Content
Set-Cookie: tracklink_refresh_token=; expires=<past-date>; path=/api/auth
```

Create tracking:

```http
POST /api/tracking
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "latitude": -21.131,
  "longitude": -48.972
}
```

Update location:

```http
PUT /api/tracking/{token}
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "latitude": -21.1305,
  "longitude": -48.9702
}
```

Read public tracking:

```http
GET /api/tracking/{token}
```

Read public history:

```http
GET /api/tracking/{token}/history
```

## SignalR

SignalR hub endpoint:

```text
/hubs/tracking
```

Clients call `JoinTracking(token)` to join a SignalR group identified by the tracking token.

`JoinTracking` validates that the tracking session exists, is active, and has not expired. If the session is unavailable, the hub throws a `HubException`.

Implemented events:

- `LocationUpdated`: sent to the token group after a successful owner update. The payload matches the tracking response: `token`, `latitude`, `longitude`, `updatedAt`, `isActive`, and `expiresAt`.
- `TrackingEnded`: sent to the token group after the owner ends a tracking session through HTTP or after the MCP `stop_tracking` tool succeeds. This event currently has no payload.

## MCP Integration

The MCP server is available at:

```text
POST /mcp
```

The MCP endpoint requires JWT authentication:

```http
Authorization: Bearer <access_token>
```

MCP uses stateless HTTP transport. Clients should accept both JSON and Server-Sent Events responses, because MCP responses can use `text/event-stream`.

Available tools:

| Tool | Description |
| --- | --- |
| `ping` | Checks whether the MCP server is working. |
| `who_am_i` | Returns information about the authenticated user. |
| `get_tracking_status` | Receives `token` and returns the current tracking status/location. |
| `get_tracking_history` | Receives `token` and returns the location history. |
| `get_my_trackings` | Returns the tracking sessions owned by the authenticated user. |
| `stop_tracking` | Receives `token`, ends a tracking session owned by the authenticated user, and emits the SignalR `TrackingEnded` event. |

`get_my_trackings` and `stop_tracking` read the `userId` from the authenticated JWT claims. They do not accept a client-supplied `userId`.

List MCP tools:

```bash
curl -X POST http://localhost:8080/mcp \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json, text/event-stream" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  }'
```

Call `get_tracking_status`:

```bash
curl -X POST http://localhost:8080/mcp \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json, text/event-stream" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "get_tracking_status",
      "arguments": {
        "token": "tracking-token"
      }
    }
  }'
```

## Database

The application uses PostgreSQL with Entity Framework Core migrations.

Main entities:

- `User`
- `Tracking`
- `TrackingLocation`
- `RefreshToken`

Relationships:

- `User` 1:N `Tracking`
- `Tracking` 1:N `TrackingLocation`
- `User` 1:N `RefreshToken`

Important indexes:

- `Tracking.Token` is unique.
- `User.Email` is unique.
- `RefreshToken.TokenHash` is unique.

## Running Locally

### Requirements

- .NET 10 SDK
- PostgreSQL
- EF Core CLI tools

Restore packages:

```bash
dotnet restore
```

Review the local PostgreSQL connection string in `appsettings.json`:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=tracklink;Username=tracklink;Password=tracklink"
```

Apply EF Core migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run
```

The default development HTTP URL is:

```text
http://localhost:5258
```

The backend includes a named CORS policy for the Angular development server at `http://localhost:4200`. The policy allows credentials so the browser can store and send the HttpOnly refresh cookie.

## Running with Docker Compose

The repository includes a Docker setup for running the API and PostgreSQL together.

Start the complete backend environment:

```bash
docker compose up --build
```

This starts:

- `postgres`: PostgreSQL 16 database
- `tracklink-migrate`: one-shot EF Core migration bundle
- `tracklink-api`: ASP.NET Core API container

The API is exposed on port `8080` by default:

```text
http://localhost:8080
```

The SignalR hub is available at:

```text
http://localhost:8080/hubs/tracking
```

Stop the containers:

```bash
docker compose down
```

Remove containers and the PostgreSQL data volume:

```bash
docker compose down -v
```

### Docker Configuration

Docker-specific settings are supplied through environment variables using ASP.NET Core configuration conventions.

Important variables:

| Variable | Default | Description |
| --- | --- | --- |
| `API_PORT` | `8080` | Host port mapped to the API container. |
| `POSTGRES_DB` | `tracklink` | PostgreSQL database name. |
| `POSTGRES_USER` | `tracklink` | PostgreSQL user. |
| `POSTGRES_PASSWORD` | `tracklink` | PostgreSQL password. |
| `POSTGRES_PORT` | `5432` | Host port mapped to PostgreSQL. |
| `JWT_KEY` | development value | JWT signing key for the Docker environment. |
| `JWT_ISSUER` | `TrackLink` | JWT issuer. |
| `JWT_AUDIENCE` | `TrackLink` | JWT audience. |
| `JWT_EXPIRATION_MINUTES` | `15` | Access token lifetime in minutes. |

Inside Docker, the API connects to PostgreSQL through the Compose service name:

```text
Host=postgres
```

Do not use `localhost` for the database host from inside the API container.

The Docker API runs with `ASPNETCORE_ENVIRONMENT=Production` by default, so the refresh cookie is emitted with `Secure=true`. Use HTTPS-aware infrastructure when sending cookies to browsers in production.

PostgreSQL data is stored in the named Docker volume:

```text
postgres_data
```

### Docker Migration Strategy

The application does not run migrations automatically inside `Program.cs`.

For Docker Compose, migrations are applied by the separate `tracklink-migrate` service using an EF Core migration bundle. The API waits for PostgreSQL to become healthy and for the migration service to complete successfully before starting.

For non-Docker local development, continue using:

```bash
dotnet ef database update
```

## Testing

Integration tests are located in `TrackLink.Tests`.

The test project uses:

- xUnit
- `WebApplicationFactory`
- SQLite in-memory databases

The tests replace the production PostgreSQL configuration with an isolated in-memory SQLite database, so the development PostgreSQL database is not modified.

Run the integration tests:

```bash
dotnet test TrackLink.Tests/TrackLink.Tests.csproj
```

## Project Structure

```text
TrackLink
├── Controllers
│   ├── AuthController.cs
│   └── TrackingController.cs
├── Data
│   └── AppDbContext.cs
├── DTOs
├── Hubs
│   └── TrackingHub.cs
├── Mcp
│   └── Responses
├── McpTools
│   └── TrackingTools.cs
├── Migrations
├── Models
├── Services
├── TrackLink.Tests
│   ├── TrackLinkApiFactory.cs
│   └── TrackLinkIntegrationTests.cs
├── Dockerfile
├── docker-compose.yml
├── Program.cs
└── TrackLink.csproj
```

## Security Notes

- Passwords are hashed before storage.
- Refresh tokens are delivered only through an HttpOnly cookie and are not exposed in response bodies.
- Refresh tokens are stored as SHA-256 hashes, not raw tokens.
- JWT Bearer authentication protects user-specific and mutation endpoints.
- Tracking mutations require ownership validation.
- Public tracking access depends on possession of the shareable tracking token.
- Production secrets, JWT keys, and database credentials should be supplied through environment variables or a secret manager and should not be committed to source control.

## Project Status

TrackLink is still under development. Core authentication, tracking, history, real-time updates, and integration testing are implemented, but the project is not yet a complete production platform.

## Roadmap

Possible future work:

- PostgreSQL-backed integration tests with Testcontainers
- Frontend application
- Map visualization
- AI/agent integration
