FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY backend/TrafficInsight.Api/TrafficInsight.Api.csproj backend/TrafficInsight.Api/
RUN dotnet restore backend/TrafficInsight.Api/TrafficInsight.Api.csproj

COPY backend/TrafficInsight.Api/ backend/TrafficInsight.Api/
WORKDIR /src/backend/TrafficInsight.Api
RUN dotnet publish TrafficInsight.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN apk add --no-cache curl
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "TrafficInsight.Api.dll"]
