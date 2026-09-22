# TrackLink API

TrackLink is an ASP.NET Core backend API for creating and sharing real-time location tracking sessions through shareable tokens.

An authenticated user can create a tracking session, receive a public token, send location updates, and share that token with viewers. Public viewers can read the current location and location history without an account, while mutations remain protected by JWT authentication and ownership checks.

The project is under active development and is intended as a practical backend portfolio project using ASP.NET Core, Entity Framework Core, PostgreSQL, JWT authentication, refresh token rotation, SignalR, and integration testing.

## Main Features

- User registration and login
- JWT Bearer authentication
- Short-lived access tokens
- Refresh tokens with rotation and revocation
- Logout through refresh token revocation
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
4. The API returns:
   - an access token used as a Bearer JWT for protected endpoints
   - a refresh token used to obtain a new token pair
5. Access tokens are short-lived.
6. Refresh tokens are stored in the database as SHA-256 hashes, not as raw tokens.
7. Refreshing a token rotates it:
   - the current refresh token is revoked
   - a new refresh token is generated
   - only the new refresh token hash is stored
8. Reusing a revoked refresh token fails.
9. Logout revokes the submitted refresh token.

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

Tracking sessions can also expire. Expired or inactive sessions cannot be updated, and public access returns the existing unavailable-session responses.

## API Endpoints

### Authentication

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Public | Register a new user. |
| `POST` | `/api/auth/login` | Public | Login and receive an access token and refresh token. |
| `POST` | `/api/auth/refresh` | Public | Rotate a valid refresh token and receive a new token pair. |
| `POST` | `/api/auth/logout` | Public | Revoke a refresh token. |
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

- `LocationUpdated`: sent to the token group after a successful owner update.
- `TrackingEnded`: sent to the token group after the owner ends a tracking session.

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
├── Migrations
├── Models
├── Services
├── TrackLink.Tests
│   ├── TrackLinkApiFactory.cs
│   └── TrackLinkIntegrationTests.cs
├── Program.cs
└── TrackLink.csproj
```

## Security Notes

- Passwords are hashed before storage.
- Refresh tokens are stored as SHA-256 hashes, not raw tokens.
- JWT Bearer authentication protects user-specific and mutation endpoints.
- Tracking mutations require ownership validation.
- Public tracking access depends on possession of the shareable tracking token.
- Production secrets, JWT keys, and database credentials should not be committed to source control.

## Project Status

TrackLink is still under development. Core authentication, tracking, history, real-time updates, and integration testing are implemented, but the project is not yet a complete production platform.

## Roadmap

Possible future work:

- Docker / Docker Compose workflow
- PostgreSQL-backed integration tests with Testcontainers
- Frontend application
- Map visualization
- MCP server
- AI/agent integration
- Deployment
