# TrackLink API

REST API for location tracking and link sharing built with ASP.NET Core, Entity Framework Core and PostgreSQL.

## About

TrackLink is a backend project designed to create and manage location tracking sessions.

Each tracking session receives a unique token that can be used to retrieve and update its current location.

The project is currently under development and is being built as a practical study of ASP.NET Core and backend architecture.

## Technologies

- C#
- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker
- REST API

## Current Features

- Create a tracking session
- Generate a unique tracking token
- Retrieve tracking information by token
- Update latitude and longitude
- End a tracking session
- Prevent updates to inactive tracking sessions
- PostgreSQL persistence
- Request validation
- Async database operations
- CancellationToken support

## API Endpoints

### Create tracking

```http
POST /api/tracking
```

Request:

```json
{
  "latitude": -21.131,
  "longitude": -48.972
}
```

### Get tracking

```http
GET /api/tracking/{token}
```

### Update location

```http
PUT /api/tracking/{token}
```

Request:

```json
{
  "latitude": -21.1305,
  "longitude": -48.9702
}
```

### End tracking

```http
DELETE /api/tracking/{token}
```

The tracking record is preserved and marked as inactive.

## Architecture

The project currently follows a simple layered structure:

```text
HTTP Request
     ↓
Controller
     ↓
Service
     ↓
Entity Framework Core
     ↓
PostgreSQL
```

Project structure:

```text
TrackLink
├── Controllers
├── Data
├── DTOs
├── Migrations
├── Models
├── Services
└── Program.cs
```

## Running Locally

### Requirements

- .NET 10 SDK
- PostgreSQL
- Docker (optional)

Clone the repository:

```bash
git clone <repository-url>
cd tracklink-api
```

Configure the PostgreSQL connection string and apply the migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run
```

## Roadmap

Planned features include:

- Browser geolocation integration
- Shareable tracking links
- Real-time location updates
- SignalR integration
- Map visualization
- Tracking session expiration
- Frontend application
- Deployment

## Status

🚧 Work in progress

This project is actively being developed while studying ASP.NET Core and modern backend development practices.