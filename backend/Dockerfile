# Multi-stage build for ASP.NET Core API (production)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and restore
COPY backend/FamilyManagement.sln ./
COPY backend/FamilyManagement.API/FamilyManagement.API.csproj backend/FamilyManagement.API/
RUN dotnet restore backend/FamilyManagement.API/FamilyManagement.API.csproj

# Copy source and publish
COPY backend/ backend/
RUN dotnet publish backend/FamilyManagement.API/FamilyManagement.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
RUN apk add --no-cache curl
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl -fsS http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "FamilyManagement.API.dll"]
