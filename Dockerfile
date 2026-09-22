FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TrackLink.csproj ./
RUN dotnet restore TrackLink.csproj

COPY . .
RUN dotnet publish TrackLink.csproj -c Release -o /app/publish --no-restore

FROM build AS migration-bundle
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle --configuration Release --output /app/migrations/efbundle --force

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TrackLink.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
WORKDIR /app

COPY --from=migration-bundle /app/migrations/efbundle .

ENTRYPOINT ["./efbundle"]
