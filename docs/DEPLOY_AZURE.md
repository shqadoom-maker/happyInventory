# Deploy HappyInventory to GitHub and Azure

This guide publishes the existing Angular + ASP.NET Core application to GitHub and
runs it as **one Linux Azure App Service container**. The container serves both the
Angular client and the `/api` endpoints from the same origin, so no frontend API URL
or CORS change is required.

The SQLite database is stored at `/home/happyInventory.db`, which is persistent App
Service storage. This is suitable for a demo or a single-instance deployment. It is
**not** safe for scale-out; see [SQLite and scaling](#sqlite-and-scaling).

> The project currently targets .NET 6. The Docker image pins .NET 6 so the app can
> run unchanged, but .NET 6 is end-of-life. Upgrade to .NET 8 LTS before using this
> for a long-lived production system.

## 1. Prerequisites

- A GitHub account and push access to
  `https://github.com/shqadoom-maker/happyInventory.git`
- An Azure subscription where you can create a resource group, Azure Container
  Registry (ACR), and App Service
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed and
  signed in (`az login`)
- Docker is **not** required locally. GitHub Actions builds the image.

All commands below are PowerShell commands. Run them from the repository root.

## 2. Push the project to GitHub

The repository already has the `origin` remote and uses the `master` branch.

```powershell
git status
git add .
git commit -m "Add Azure container deployment"
git push origin master
```

If Git asks for credentials, use a GitHub personal access token or configure SSH.
Do not commit real JWT keys, admin passwords, database files, or logs; `.gitignore`
and `.dockerignore` already exclude the common local files.

## 3. Create the Azure resources

Set names for your deployment. The ACR name and web app name must be globally unique.

```powershell
$RG = "rg-happy-inventory"
$LOCATION = "westeurope"
$ACR = "happyinventoryacr"          # lowercase letters and numbers only
$PLAN = "plan-happy-inventory"
$APP = "happy-inventory-app"        # must be globally unique
$IMAGE = "happy-inventory"

az group create --name $RG --location $LOCATION
az acr create --resource-group $RG --name $ACR --sku Basic
az appservice plan create --resource-group $RG --name $PLAN --is-linux --sku B1
az webapp create --resource-group $RG --plan $PLAN --name $APP `
  --deployment-container-image-name "$ACR.azurecr.io/$IMAGE:latest"
```

Keep the App Service plan at **one worker** while using SQLite:

```powershell
az appservice plan update --resource-group $RG --name $PLAN --number-of-workers 1
```

## 4. Configure the application settings

Generate a strong JWT key and an initial admin password. The JWT key must be at
least 32 bytes. Keep both values out of source control.

```powershell
$JwtKey = "<paste-a-random-secret-of-at-least-32-characters>"
$AdminPassword = "<paste-a-strong-admin-password>"

az webapp config appsettings set --resource-group $RG --name $APP --settings `
  WEBSITES_PORT=8080 `
  WEBSITES_ENABLE_APP_SERVICE_STORAGE=true `
  ASPNETCORE_ENVIRONMENT=Production `
  "ConnectionStrings__Default=Data Source=/home/happyInventory.db" `
  "Jwt__Key=$JwtKey" `
  "Jwt__Issuer=happyInventory" `
  "Jwt__Audience=happyInventory.Client" `
  "Seed__AdminUsername=admin@happywarehouse.com" `
  "Seed__AdminPassword=$AdminPassword" `
  "Logging__Database__Enabled=true"
```

The double underscores are ASP.NET Core's environment-variable mapping for nested
configuration keys. The Dockerfile listens on port `8080`, so `WEBSITES_PORT` must
match.

Enable HTTPS-only traffic at the App Service front end:

```powershell
az webapp update --resource-group $RG --name $APP --https-only true
```

The API only enables `UseHttpsRedirection` in Development because App Service
terminates TLS and forwards HTTP to the container. HTTPS-only is therefore enforced
by the platform.

## 5. Let App Service pull the image with managed identity

The workflow pushes images to ACR. App Service should pull them with its
system-assigned managed identity instead of an ACR admin password.

```powershell
$principalId = az webapp identity assign `
  --resource-group $RG --name $APP --query principalId -o tsv

$acrId = az acr show `
  --resource-group $RG --name $ACR --query id -o tsv

az role assignment create `
  --assignee-object-id $principalId `
  --assignee-principal-type ServicePrincipal `
  --scope $acrId `
  --role AcrPull

# ACR must accept Microsoft Entra/ARM tokens for managed-identity pulls.
az acr config authentication-as-arm update --registry $ACR --status enabled

# Tell App Service to use its managed identity when pulling from ACR.
az webapp config set --resource-group $RG --name $APP `
  --generic-configurations '{"acrUseManagedIdentityCreds": true}'

# Point the app at the registry. The image tag is replaced by each deployment.
az webapp config container set --resource-group $RG --name $APP `
  --docker-custom-image-name "$ACR.azurecr.io/$IMAGE:latest" `
  --docker-registry-server-url "https://$ACR.azurecr.io"
```

If the last command resets the managed-identity flag, run the
`acrUseManagedIdentityCreds` command again and restart the app.

## 6. Create the GitHub Actions credentials

The workflow uses a service principal stored as the `AZURE_CREDENTIALS` GitHub
secret. It needs permission to deploy the App Service and push to ACR.

```powershell
$subscriptionId = az account show --query id -o tsv

$spJson = az ad sp create-for-rbac `
  --name "happy-inventory-github" `
  --role Contributor `
  --scopes "/subscriptions/$subscriptionId/resourceGroups/$RG" `
  --sdk-auth

$spJson
```

Copy the complete JSON object printed by `$spJson` into GitHub:

1. Open the repository on GitHub.
2. Go to **Settings** > **Secrets and variables** > **Actions**.
3. Create a repository secret named `AZURE_CREDENTIALS` and paste the JSON.
4. Create repository variables:
   - `AZURE_WEBAPP_NAME` = the value of `$APP`
   - `ACR_NAME` = the value of `$ACR`

The service principal also needs `AcrPush` on the registry so the workflow can run
`docker push`:

```powershell
$spAppId = ($spJson | ConvertFrom-Json).appId

az role assignment create `
  --assignee-object-id $spAppId `
  --assignee-principal-type ServicePrincipal `
  --scope $acrId `
  --role AcrPush
```

Never paste the `AZURE_CREDENTIALS` JSON into source code, an issue, or a chat
message.

## 7. Run the deployment

The workflow is `.github/workflows/azure-container-deploy.yml`. It runs when you
push to `master`, and it can also be started manually from the GitHub **Actions**
tab.

```powershell
git add .
git commit -m "Configure Azure deployment"
git push origin master
```

The workflow:

1. Runs the .NET test project.
2. Signs in to Azure with `AZURE_CREDENTIALS`.
3. Builds the multi-stage Docker image, including the Angular production build.
4. Pushes the image to ACR with the Git commit SHA as the tag.
5. Deploys that image to the App Service.

If your default branch is `main` instead of `master`, update the `branches` entry in
the workflow before pushing.

## 8. Verify the running site

```powershell
az webapp browse --resource-group $RG --name $APP
```

Open `https://<your-app>.azurewebsites.net` and sign in with the admin username and
password from step 4.

Check the API health from a terminal:

```powershell
curl.exe -i https://<your-app>.azurewebsites.net/api/dashboard
```

Without a token this should return `401`, which confirms the API is running and
authorization is active. Sign in through the UI to get a token and load the
dashboard.

If the container does not start, enable and stream logs:

```powershell
az webapp log config --resource-group $RG --name $APP `
  --docker-container-logging filesystem

az webapp log tail --resource-group $RG --name $APP
```

Common causes are a missing/short `Jwt__Key`, a port mismatch, or an ACR pull
authorization error.

## SQLite and scaling

- The database file is `/home/happyInventory.db`. `WEBSITES_ENABLE_APP_SERVICE_STORAGE=true`
  is required for Linux custom containers, otherwise `/home` is not persistent.
- Keep the App Service plan at **one instance**. SQLite does not support safe
  concurrent writers across multiple instances.
- The database is created and migrated automatically on first startup. The initial
  admin user is seeded from `Seed__AdminUsername` and `Seed__AdminPassword`.
- Changing `Seed__AdminPassword` after the database exists does **not** reset the
  existing admin password. Change it through the application, or remove the
  database (destructive) to reseed.
- Back up the SQLite file by downloading it from the Kudu console or by copying it
  from `/home`. App Service does not provide SQLite backups automatically.
- To move to Azure SQL later, change the EF Core provider and migrations from
  SQLite to SQL Server/Azure SQL. Changing only the connection string is not enough.

## Troubleshooting checklist

| Symptom | Check |
| --- | --- |
| Workflow fails at `az acr login` | Service principal has `AcrPush` on the registry |
| App shows "Application Error" | `az webapp log tail`; check `Jwt__Key`, port `8080`, and image pull |
| `401` on every API call | Sign in first; verify `Jwt__Key`, issuer, and audience settings |
| Login succeeds but data disappears after restart | `WEBSITES_ENABLE_APP_SERVICE_STORAGE=true` and `/home/happyInventory.db` |
| New deployment still serves old code | Wait for the image pull/restart; check the workflow run and container logs |
| `.NET 6` warning in the image build | Expected; plan an upgrade to .NET 8 LTS |

## Useful Azure commands

```powershell
# Show the deployed container image
az webapp config container show --resource-group $RG --name $APP

# Restart after changing settings
az webapp restart --resource-group $RG --name $APP

# Stream container logs
az webapp log tail --resource-group $RG --name $APP

# Show the public URL
az webapp show --resource-group $RG --name $APP --query defaultHostName -o tsv
```
