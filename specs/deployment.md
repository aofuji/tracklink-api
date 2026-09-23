# Deployment Spec

Esta spec documenta somente a configuracao de deployment existente.

## .NET

- O projeto principal MUST usar `Microsoft.NET.Sdk.Web`.
- O target framework configurado e `net10.0`.
- Nullable reference types e implicit usings estao habilitados.
- O Dockerfile usa imagens oficiais:
  - `mcr.microsoft.com/dotnet/sdk:10.0` para build e bundle de migrations.
  - `mcr.microsoft.com/dotnet/aspnet:10.0` para API e migration runner.

## PostgreSQL E EF Core

- A aplicacao usa EF Core com provider Npgsql para PostgreSQL fora do ambiente `Testing`.
- A connection string padrao em `appsettings.json` e:

```text
Host=localhost;Port=5432;Database=tracklink;Username=tracklink;Password=tracklink
```

- O projeto contem migrations EF Core em `Migrations/`.
- A aplicacao MUST NOT executar migrations automaticamente em `Program.cs`.
- Em Docker Compose, migrations MUST ser aplicadas por um servico separado de migration bundle.

## Dockerfile

- O stage `build` MUST restaurar `TrackLink.csproj` e publicar em Release para `/app/publish`.
- O stage `migration-bundle` MUST instalar `dotnet-ef` versao `10.0.12` e gerar `/app/migrations/efbundle`.
- O stage `api` MUST copiar o publish output e iniciar `dotnet TrackLink.dll`.
- O stage `api` MUST expor a porta `8080` e configurar `ASPNETCORE_URLS=http://+:8080`.
- O stage `migrate` MUST copiar o `efbundle` e executa-lo como entrypoint.

## Docker Compose

### Servico `postgres`

- MUST usar a imagem `postgres:16`.
- MUST usar container name `tracklink-postgres`.
- MUST reiniciar com `unless-stopped`.
- MUST configurar:
  - `POSTGRES_DB` com default `tracklink`.
  - `POSTGRES_USER` com default `tracklink`.
  - `POSTGRES_PASSWORD` com default `tracklink`.
- MUST mapear `${POSTGRES_PORT:-5432}:5432`.
- MUST persistir dados no volume nomeado `postgres_data`.
- MUST ter healthcheck usando `pg_isready`.

### Servico `tracklink-migrate`

- MUST buildar o Dockerfile com target `migrate`.
- MUST usar container name `tracklink-migrate`.
- MUST depender do `postgres` saudavel.
- MUST executar o migration bundle com `--connection` apontando para `Host=postgres;Port=5432`.
- MUST usar `restart: "no"`.

### Servico `tracklink-api`

- MUST buildar o Dockerfile com target `api`.
- MUST usar container name `tracklink-api`.
- MUST reiniciar com `unless-stopped`.
- MUST depender do `postgres` saudavel e do `tracklink-migrate` concluido com sucesso.
- MUST configurar `ASPNETCORE_URLS=http://+:8080`.
- MUST mapear `${API_PORT:-8080}:8080`.
- MUST conectar ao PostgreSQL pelo host de Compose `postgres`.

## Variaveis De Ambiente Existentes

As variaveis documentadas em `.env.example` e usadas pelo Compose sao:

- `POSTGRES_DB`, default `tracklink`.
- `POSTGRES_USER`, default `tracklink`.
- `POSTGRES_PASSWORD`, default `tracklink`.
- `POSTGRES_PORT`, default `5432`.
- `API_PORT`, default `8080`.
- `JWT_KEY`, default `TrackLink-Docker-Development-Key-Change-Me`.
- `JWT_ISSUER`, default `TrackLink`.
- `JWT_AUDIENCE`, default `TrackLink`.
- `JWT_EXPIRATION_MINUTES`, default `15`.

No container da API, essas variaveis sao aplicadas como:

- `ASPNETCORE_ENVIRONMENT`.
- `ASPNETCORE_URLS`.
- `ConnectionStrings__DefaultConnection`.
- `Jwt__Key`.
- `Jwt__Issuer`.
- `Jwt__Audience`.
- `Jwt__ExpirationMinutes`.

## Portas Configuradas

- Docker API: container `8080`, host `${API_PORT:-8080}`.
- Docker PostgreSQL: container `5432`, host `${POSTGRES_PORT:-5432}`.
- Launch profile `http`: `http://localhost:5258`.
- Launch profile `https`: `https://localhost:7268;http://localhost:5258`.
