#!/usr/bin/env bash
set -euo pipefail

required=(AZURE_SUBSCRIPTION_ID AZURE_RESOURCE_GROUP AZURE_REGION AZURE_ACR_NAME AZURE_CONTAINERAPP_ENV)
for key in "${required[@]}"; do
  if [[ -z "${!key:-}" ]]; then
    echo "Set $key before running this script." >&2
    exit 2
  fi
done

PULL_IDENTITY_NAME="${AZURE_PULL_IDENTITY_NAME:-traffic-insight-pull}"
LOG_WORKSPACE_NAME="${AZURE_LOG_WORKSPACE_NAME:-traffic-insight-logs}"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
az extension add --name containerapp --upgrade --yes >/dev/null
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.ContainerRegistry --wait

az group create \
  --name "$AZURE_RESOURCE_GROUP" \
  --location "$AZURE_REGION" \
  --tags application=traffic-insight environment=learning

az acr create \
  --name "$AZURE_ACR_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --location "$AZURE_REGION" \
  --sku Basic \
  --admin-enabled false

az monitor log-analytics workspace create \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --workspace-name "$LOG_WORKSPACE_NAME" \
  --location "$AZURE_REGION" \
  --retention-time 30 >/dev/null

WORKSPACE_ID="$(az monitor log-analytics workspace show \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --workspace-name "$LOG_WORKSPACE_NAME" \
  --query customerId --output tsv)"

az containerapp env create \
  --name "$AZURE_CONTAINERAPP_ENV" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --location "$AZURE_REGION" \
  --logs-destination log-analytics \
  --logs-workspace-id "$WORKSPACE_ID" >/dev/null

az identity create \
  --name "$PULL_IDENTITY_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --location "$AZURE_REGION" >/dev/null

IDENTITY_ID="$(az identity show \
  --name "$PULL_IDENTITY_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --query id --output tsv)"
IDENTITY_PRINCIPAL_ID="$(az identity show \
  --name "$PULL_IDENTITY_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --query principalId --output tsv)"
ACR_ID="$(az acr show \
  --name "$AZURE_ACR_NAME" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --query id --output tsv)"

EXISTING_ROLE="$(az role assignment list \
  --assignee "$IDENTITY_PRINCIPAL_ID" \
  --scope "$ACR_ID" \
  --role AcrPull \
  --query '[0].id' --output tsv)"
if [[ -z "$EXISTING_ROLE" || "$EXISTING_ROLE" == "None" ]]; then
  az role assignment create \
    --assignee-object-id "$IDENTITY_PRINCIPAL_ID" \
    --assignee-principal-type ServicePrincipal \
    --role AcrPull \
    --scope "$ACR_ID" >/dev/null
fi

cat <<EOF

Azure foundation is ready. Configure these GitHub repository variables:
  AZURE_RESOURCE_GROUP=$AZURE_RESOURCE_GROUP
  AZURE_REGION=$AZURE_REGION
  AZURE_ACR_NAME=$AZURE_ACR_NAME
  AZURE_CONTAINERAPP_ENV=$AZURE_CONTAINERAPP_ENV
  AZURE_API_APP_NAME=traffic-insight-api
  AZURE_WEB_APP_NAME=traffic-insight-web
  AZURE_PULL_IDENTITY_ID=$IDENTITY_ID

Then configure the three Azure OIDC secrets and grant the GitHub deployment
identity AcrPush on the registry, Container Apps Contributor on the resource
group, and Managed Identity Operator on the pull identity. See setup.md.
EOF
