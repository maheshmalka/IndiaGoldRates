// Deploys the IndiaGoldRates Azure footprint into the resource group you deploy this into.
// See infra/README.md for exact CLI commands and the two-step CORS/frontend-URL wiring.
//
// Cost-conscious tier choices (see the project plan for rationale):
//   - App Service Plan: Linux B1 (~$13/mo) — Free/Shared tiers don't support "Always On",
//     which the continuous rate-polling BackgroundService requires.
//   - Azure SQL: Basic 5 DTU (~$5/mo) — not Serverless, since RateSnapshot writes every ~60s
//     mean the DB never idles long enough to auto-pause.
//   - Static Web Apps: Free tier — sufficient for the React SPA, includes PR previews.
//   - No Azure SignalR Service in v1 — in-process SignalR on one instance is enough at this
//     scale; only needed once scaling out to multiple instances (sticky sessions).

@description('Short name used to derive resource names, e.g. \'indiagoldrates\'. Lowercase letters/numbers only.')
@minLength(3)
@maxLength(16)
param appNamePrefix string = 'indiagoldrates'

@description('Deployment environment suffix, e.g. \'prod\' or \'dev\'.')
param environmentName string = 'prod'

param location string = resourceGroup().location

@description('Admin login for the Azure SQL logical server.')
param sqlAdminLogin string

@secure()
@description('Admin password for the Azure SQL logical server.')
param sqlAdminPassword string

@secure()
@description('Google OAuth Client ID. Leave empty to deploy without Google sign-in configured yet.')
param googleClientId string = ''

@secure()
param googleClientSecret string = ''

@secure()
@description('Microsoft Entra OAuth Client ID. Leave empty to deploy without Microsoft sign-in configured yet.')
param microsoftClientId string = ''

@secure()
param microsoftClientSecret string = ''

@description('The deployed frontend URL (Static Web App). Known only after the first deploy — see infra/README.md step 2. Used for CORS and OAuth-callback redirects.')
param frontendBaseUrl string = ''

var resourceToken = '${appNamePrefix}-${environmentName}'
var sqlServerName = '${resourceToken}-sql'
var sqlDatabaseName = '${appNamePrefix}db'
var appServicePlanName = '${resourceToken}-plan'
var apiAppName = '${resourceToken}-api'
var staticWebAppName = '${resourceToken}-web'
var logAnalyticsName = '${resourceToken}-logs'
var appInsightsName = '${resourceToken}-insights'
var communicationServiceName = '${resourceToken}-acs'
var emailServiceName = '${resourceToken}-email'

// ---- Monitoring ----

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

// ---- Database ----

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    maxSizeBytes: 2147483648 // 2 GB, the Basic tier ceiling
  }
}

// Allows Azure services (including this App Service) to reach the SQL server.
resource sqlFirewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---- Email (Azure Communication Services) ----

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: 'global'
  properties: {
    dataLocation: 'India'
  }
}

// Azure-managed sender domain (e.g. donotreply@<guid>.azurecomm.net) — zero extra DNS setup.
// Swap for a custom verified domain later if you want a branded "from" address.
resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: 'global'
  properties: {
    dataLocation: 'India'
    linkedDomains: [
      emailDomain.id
    ]
  }
}

// ---- Backend: App Service Plan + Web App ----

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ConnectionStrings__SqlServer', value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;' }
        { name: 'Frontend__BaseUrl', value: frontendBaseUrl }
        { name: 'Cors__AllowedOrigin', value: frontendBaseUrl }
        { name: 'Authentication__Google__ClientId', value: googleClientId }
        { name: 'Authentication__Google__ClientSecret', value: googleClientSecret }
        { name: 'Authentication__Microsoft__ClientId', value: microsoftClientId }
        { name: 'Authentication__Microsoft__ClientSecret', value: microsoftClientSecret }
        { name: 'Acs__ConnectionString', value: communicationService.listKeys().primaryConnectionString }
        { name: 'Acs__SenderAddress', value: 'DoNotReply@${emailDomain.properties.mailFromSenderDomain}' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
      ]
    }
  }
}

// ---- Frontend: Static Web App ----

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

output apiUrl string = 'https://${apiApp.properties.defaultHostName}'
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output staticWebAppName string = staticWebApp.name
// Deployment token is a credential — retrieve it separately, don't put it in deployment outputs
// (which get logged): az staticwebapp secrets list --name <staticWebAppName> --query "properties.apiKey" -o tsv
