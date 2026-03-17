# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY CargoHub.Backend.sln ./
COPY CargoHub.Api/CargoHub.Api.csproj CargoHub.Api/
COPY CargoHub.Application/CargoHub.Application.csproj CargoHub.Application/
COPY CargoHub.Domain/CargoHub.Domain.csproj CargoHub.Domain/
COPY CargoHub.Infrastructure/CargoHub.Infrastructure.csproj CargoHub.Infrastructure/

# Restore dependencies
RUN dotnet restore CargoHub.Api/CargoHub.Api.csproj

# Copy all source code
COPY . .

# Build and publish
RUN dotnet publish CargoHub.Api/CargoHub.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# PORT is read by Program.cs in Production; expose for local Docker
EXPOSE 8080

ENTRYPOINT ["dotnet", "CargoHub.Api.dll"]
