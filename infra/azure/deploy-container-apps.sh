#!/usr/bin/env bash
set -euo pipefail

required=(
  ACR_LOGIN_SERVER
  AZURE_RESOURCE_GROUP
  AZURE_CONTAINERAPP_ENV
  AZURE_API_APP_NAME
  AZURE_WEB_APP_NAME
  AZURE_PULL_IDENTITY_ID
  IMAGE_TAG
)
for key in "${required[@]}"; do
  if [[ -z "${!key:-}" ]]; then
    echo "Missing required deployment value: $key" >&2
    exit 2
  fi
done

API_IMAGE="$ACR_LOGIN_SERVER/traffic-insight-api:$IMAGE_TAG"
WEB_IMAGE="$ACR_LOGIN_SERVER/traffic-insight-web:$IMAGE_TAG"

if az containerapp show --name "$AZURE_API_APP_NAME" --resource-group "$AZURE_RESOURCE_GROUP" >/dev/null 2>&1; then
  az containerapp update \
    --name "$AZURE_API_APP_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --image "$API_IMAGE" \
    --set-env-vars ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_HTTP_PORTS=8080
else
  az containerapp create \
    --name "$AZURE_API_APP_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --environment "$AZURE_CONTAINERAPP_ENV" \
    --image "$API_IMAGE" \
    --target-port 8080 \
    --ingress internal \
    --transport http \
    --registry-server "$ACR_LOGIN_SERVER" \
    --registry-identity "$AZURE_PULL_IDENTITY_ID" \
    --user-assigned "$AZURE_PULL_IDENTITY_ID" \
    --env-vars ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_HTTP_PORTS=8080 \
    --cpu 0.5 \
    --memory 1.0Gi \
    --min-replicas 0 \
    --max-replicas 3 \
    --revisions-mode single
fi

if az containerapp show --name "$AZURE_WEB_APP_NAME" --resource-group "$AZURE_RESOURCE_GROUP" >/dev/null 2>&1; then
  az containerapp update \
    --name "$AZURE_WEB_APP_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --image "$WEB_IMAGE" \
    --set-env-vars "API_UPSTREAM=http://$AZURE_API_APP_NAME"
else
  az containerapp create \
    --name "$AZURE_WEB_APP_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --environment "$AZURE_CONTAINERAPP_ENV" \
    --image "$WEB_IMAGE" \
    --target-port 8080 \
    --ingress external \
    --transport http \
    --registry-server "$ACR_LOGIN_SERVER" \
    --registry-identity "$AZURE_PULL_IDENTITY_ID" \
    --user-assigned "$AZURE_PULL_IDENTITY_ID" \
    --env-vars "API_UPSTREAM=http://$AZURE_API_APP_NAME" \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 0 \
    --max-replicas 3 \
    --revisions-mode single
fi

WEB_FQDN="$(az containerapp show \
  --name "$AZURE_WEB_APP_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --query properties.configuration.ingress.fqdn \
  --output tsv)"

az containerapp update \
  --name "$AZURE_API_APP_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --set-env-vars "AllowedOrigins__0=https://$WEB_FQDN"

WEB_URL="https://$WEB_FQDN"
echo "Waiting for the frontend and API health endpoints at $WEB_URL"
for attempt in {1..60}; do
  if curl --fail --silent "$WEB_URL/healthz" >/dev/null &&
     curl --fail --silent "$WEB_URL/health" >/dev/null; then
    echo "Deployment is healthy: $WEB_URL"
    exit 0
  fi
  sleep 5
done

echo "The deployed app did not pass its health checks. Inspect Azure Container Apps logs." >&2
exit 1
