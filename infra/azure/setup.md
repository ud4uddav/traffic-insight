# Azure deployment setup

The repository contains a repeatable Azure foundation script and a GitHub Actions deployment job. The app runs as two Azure Container Apps: the web app has public HTTPS ingress, while the API is internal-only. No database is deployed because this app does not retain traffic data. Azure resources are not created by GitHub until the setup below is completed.

## 1. Create the Azure foundation

Install Azure CLI, sign in, select a subscription, and choose a region supported by Azure Container Apps. The script creates an Azure resource group, Basic Azure Container Registry (ACR), Log Analytics workspace, Consumption Container Apps environment, and a managed identity with read-only access to pull images.

```sh
az login
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_RESOURCE_GROUP="traffic-insight-rg"
export AZURE_REGION="northeurope"
export AZURE_ACR_NAME="your-globally-unique-registry-name"
export AZURE_CONTAINERAPP_ENV="traffic-insight-env"
bash infra/azure/bootstrap.sh
```

ACR names are globally unique and use only lowercase letters and numbers. Choose the region near your expected users and Digitraffic source. Review Azure pricing before running the script: ACR storage, log ingestion/retention, and Container Apps usage can incur charges. Consumption apps may scale to zero, but this does not make the registry or log workspace free.

## 2. Configure GitHub to sign in to Azure without a password

Create an app registration/service principal in Microsoft Entra ID. Add a **federated credential** for GitHub Actions with:

- Organization: `ud4uddav`
- Repository: `traffic-insight`
- Entity type: branch
- Branch: `main`
- Audience: `api://AzureADTokenExchange`

Add these GitHub repository **secrets** under **Settings → Secrets and variables → Actions → Secrets**:

| Secret | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | Entra app registration's client ID |
| `AZURE_TENANT_ID` | Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

Grant the service principal these roles:

- `AcrPush` on the ACR resource
- `Container Apps Contributor` on the app resource group
- `Managed Identity Operator` on the pull identity created by `bootstrap.sh`

The pull identity only needs `AcrPull` on the ACR; the bootstrap script assigns that role. The GitHub workflow uses OIDC tokens and does not need an ACR password or client secret.

Add these repository **variables** under **Settings → Secrets and variables → Actions → Variables**:

| Variable | Value |
| --- | --- |
| `AZURE_RESOURCE_GROUP` | Resource group name from the bootstrap step |
| `AZURE_REGION` | Chosen Azure region |
| `AZURE_ACR_NAME` | ACR name from the bootstrap step |
| `AZURE_CONTAINERAPP_ENV` | Container Apps environment name |
| `AZURE_API_APP_NAME` | `traffic-insight-api` |
| `AZURE_WEB_APP_NAME` | `traffic-insight-web` |
| `AZURE_PULL_IDENTITY_ID` | Full user-assigned identity resource ID printed by the script |

Do not configure the `AZURE_RESOURCE_GROUP` variable until you are ready for CI to create or update Azure apps. Until it is set, GitHub Actions only tests and builds the application and Docker images locally on the runner.

Protect `main` in GitHub and require the **Test and build** check before merging pull requests. Enable Dependabot alerts and security updates in the repository settings.

## 3. Deploy

Every push and pull request runs frontend tests, lint, vulnerability checks, backend tests, and Docker builds. A push to `main` publishes SHA-tagged images to ACR and deploys them only after the Azure variables and OIDC secrets have been configured. For the first deployment, open **Actions → CI and Azure deployment → Run workflow** on `main`. The workflow checks `/healthz` on the frontend and `/health` through the frontend proxy after deployment.

The returned frontend URL is the public dashboard. API ingress is internal to the Container Apps environment. Each deploy uses the source commit SHA as its image version; the registry also receives a `latest` convenience tag.

## 4. Logs, health, alerts, and cleanup

View application and platform logs:

```sh
az containerapp logs show \
  --name traffic-insight-api \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --type console \
  --follow

az containerapp logs show \
  --name traffic-insight-api \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --type system \
  --tail 100
```

The API and frontend expose health endpoints, and the containers include Docker health checks for local Compose. Azure Container Apps also applies ingress health probes. Set a small Azure Cost Management budget and email alert in the Azure portal before leaving the project running. A budget alert reports spend; it does not shut resources down. Delete the resource group in Azure when finished to remove the project resources.

The API's limit is 120 traffic-data requests per minute per API instance. It has a 15-second timeout for Digitraffic calls, emits JSON logs in Production, rejects non-HTTPS Digitraffic base URLs, and returns sanitized upstream errors. Azure handles public HTTPS at ingress. No credentials are needed by the application itself.
