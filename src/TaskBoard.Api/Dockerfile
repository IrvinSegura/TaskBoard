# Build from the repository root: docker build -f src/TaskBoard.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Project files first, so the restore layer stays cached until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json .editorconfig ./
COPY src/TaskBoard.Domain/TaskBoard.Domain.csproj src/TaskBoard.Domain/
COPY src/TaskBoard.Application/TaskBoard.Application.csproj src/TaskBoard.Application/
COPY src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj src/TaskBoard.Infrastructure/
COPY src/TaskBoard.Api/TaskBoard.Api.csproj src/TaskBoard.Api/
RUN dotnet restore src/TaskBoard.Api/TaskBoard.Api.csproj && dotnet tool restore

COPY src/ src/
RUN dotnet publish src/TaskBoard.Api/TaskBoard.Api.csproj --configuration Release --no-restore --output /app/publish

# Migrations bundle: one executable that applies pending migrations, run once per deployment before the API.
FROM build AS migrations-build
RUN dotnet ef migrations bundle \
    --project src/TaskBoard.Infrastructure \
    --startup-project src/TaskBoard.Api \
    --configuration Release \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrations
WORKDIR /app
COPY --from=migrations-build /app/efbundle .
USER $APP_UID
# Reads ConnectionStrings__Database like the API; --connection overrides it.
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskBoard.Api.dll"]
