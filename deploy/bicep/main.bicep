@description('Location for all resources')
param location string = resourceGroup().location

@description('App name prefix')
param appName string = 'querymind'

@description('Environment (dev, staging, prod)')
param environment string = 'prod'

@description('Anthropic API Key')
@secure()
param anthropicApiKey string

@description('PostgreSQL admin password')
@secure()
param postgresAdminPassword string

var tags = {
  app: appName
  environment: environment
  managedBy: 'bicep'
}

// --- PostgreSQL Flexible Server ---
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: '${appName}-pg-${environment}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: 'querymind'
    administratorLoginPassword: postgresAdminPassword
    version: '16'
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
  }
}

resource querymindDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgres
  name: 'querymind'
}

// --- Storage Account ---
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${replace(appName, '-', '')}${environment}sa'
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

// --- Azure AI Search ---
resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: '${appName}-search-${environment}'
  location: location
  tags: tags
  sku: { name: 'basic' }
  properties: {
    replicaCount: 1
    partitionCount: 1
  }
}

// --- Container App Environment ---
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: '${appName}-env-${environment}'
  location: location
  tags: tags
  properties: {}
}

// --- API Container App ---
resource apiApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: '${appName}-api-${environment}'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: ['*']
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
        }
      }
      secrets: [
        { name: 'anthropic-api-key', value: anthropicApiKey }
        { name: 'postgres-password', value: postgresAdminPassword }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: 'querymind/api:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ConnectionStrings__Postgres', value: 'Host=${postgres.properties.fullyQualifiedDomainName};Database=querymind;Username=querymind;Password=${postgresAdminPassword};SslMode=Require' }
            { name: 'Anthropic__ApiKey', secretRef: 'anthropic-api-key' }
            { name: 'Azure__StorageConnectionString', value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value}' }
            { name: 'Azure__SearchEndpoint', value: 'https://${search.name}.search.windows.net' }
            { name: 'Azure__SearchApiKey', value: search.listAdminKeys().primaryKey }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-rule'
            http: { metadata: { concurrentRequests: '100' } }
          }
        ]
      }
    }
  }
}

// --- Frontend Container App ---
resource frontendApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: '${appName}-frontend-${environment}'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: 'querymind/frontend:latest'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

output apiUrl string = 'https://${apiApp.properties.configuration.ingress!.fqdn}'
output frontendUrl string = 'https://${frontendApp.properties.configuration.ingress!.fqdn}'
output postgresHost string = postgres.properties.fullyQualifiedDomainName
output searchEndpoint string = 'https://${search.name}.search.windows.net'
