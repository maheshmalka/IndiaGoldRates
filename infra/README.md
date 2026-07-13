# Deploying IndiaGoldRates to Azure

Everything below uses the Azure CLI (`az`). You'll need an Azure subscription and to be logged
in (`az login`). This deploys real, billed resources (~$18-20/month — see `main.bicep` for the
tier breakdown and rationale).

## 1. Create the resource group and deploy infrastructure

```bash
az group create --name rg-indiagoldrates-prod --location centralindia

az deployment group create \
  --resource-group rg-indiagoldrates-prod \
  --template-file infra/main.bicep \
  --parameters \
    sqlAdminLogin='<choose-a-login>' \
    sqlAdminPassword='<choose-a-strong-password>' \
    googleClientId='<from Google Cloud Console, or leave blank for now>' \
    googleClientSecret='<...>' \
    microsoftClientId='<from Microsoft Entra, or leave blank for now>' \
    microsoftClientSecret='<...>'
```

This creates: App Service Plan (Linux B1) + Web App for the API, Azure SQL Server + Database
(Basic), Azure Communication Services + Email (Azure-managed sender domain), Static Web App
(Free), Log Analytics + Application Insights.

Note the outputs — `apiUrl` and `staticWebAppUrl` — you'll need both next.

## 2. Re-deploy with the frontend URL (fixes CORS + OAuth redirect target)

The Static Web App's URL only exists after step 1, but the API needs it for CORS and for
redirecting back to the frontend after login. Run the same command again, adding:

```bash
    frontendBaseUrl='<staticWebAppUrl from step 1 output>'
```

(Re-running is safe — Bicep deployments are idempotent; this just updates the API app's settings.)

## 3. Update the OAuth apps' redirect URIs for production

In Google Cloud Console and the Microsoft Entra app registration (see the main setup
instructions), add a second redirect URI alongside the localhost one:

```
<apiUrl>/signin-google
<apiUrl>/signin-microsoft
```

## 4. Get the Static Web App deployment token (for GitHub Actions)

```bash
az staticwebapp secrets list \
  --name <staticWebAppName from step 1 output> \
  --resource-group rg-indiagoldrates-prod \
  --query "properties.apiKey" -o tsv
```

Add this as a GitHub repo secret named `AZURE_STATIC_WEB_APPS_API_TOKEN`.

## 5. Set up GitHub Actions deployment credentials for the API

The backend workflow deploys via OIDC federated credentials (no long-lived secret). Create an
Entra app registration for GitHub Actions and federate it to your repo:

```bash
az ad app create --display-name "indiagoldrates-github-actions"
# note the appId from the output, then:
az ad sp create --id <appId>
az role assignment create --assignee <appId> --role contributor \
  --scope /subscriptions/<subscription-id>/resourceGroups/rg-indiagoldrates-prod

az ad app federated-credential create --id <appId> --parameters '{
  "name": "github-main-branch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<your-github-org>/<your-repo>:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

Add these as GitHub repo secrets: `AZURE_CLIENT_ID` (the appId), `AZURE_TENANT_ID`,
`AZURE_SUBSCRIPTION_ID`.

## 6. Push to `main`

Both GitHub Actions workflows (`.github/workflows/backend-deploy.yml` and
`frontend-deploy.yml`) trigger on push to `main` and deploy automatically from there.
