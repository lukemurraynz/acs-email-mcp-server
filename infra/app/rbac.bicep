param storageAccountName string
param appInsightsName string
param communicationServicesName string
param managedIdentityPrincipalId string // Principal ID for the Managed Identity
param userIdentityPrincipalId string = '' // Principal ID for the User Identity
param allowUserIdentityPrincipal bool = false // Flag to enable user identity role assignments
param enableBlob bool = true
param enableQueue bool = false
param enableTable bool = false

// Define Role Definition IDs internally
var storageRoleDefinitionId  = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' //Storage Blob Data Owner role
var queueRoleDefinitionId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88' // Storage Queue Data Contributor role
var tableRoleDefinitionId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Storage Table Data Contributor role
var monitoringRoleDefinitionId = '3913510d-42f4-4e42-8a64-420c390055eb' // Monitoring Metrics Publisher role ID
var communicationContributorRoleDefinitionId = 'b24988ac-6180-42a0-ab88-20f7382dd24c' // Contributor role

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageAccountName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource communicationServices 'Microsoft.Communication/communicationServices@2023-04-01' existing = {
  name: communicationServicesName
}

// Role assignment for Storage Account (Blob) - Managed Identity
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableBlob) {
  name: guid(storageAccount.id, managedIdentityPrincipalId, storageRoleDefinitionId) // Use managed identity ID
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinitionId)
    principalId: managedIdentityPrincipalId // Use managed identity ID
    principalType: 'ServicePrincipal' // Managed Identity is a Service Principal
  }
}

// Role assignment for Storage Account (Blob) - User Identity
resource storageRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableBlob && allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(storageAccount.id, userIdentityPrincipalId, storageRoleDefinitionId)
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinitionId)
    principalId: userIdentityPrincipalId // Use user identity ID
    principalType: 'User' // User Identity is a User Principal
  }
}

// Role assignment for Storage Account (Queue) - Managed Identity
resource queueRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableQueue) {
  name: guid(storageAccount.id, managedIdentityPrincipalId, queueRoleDefinitionId) // Use managed identity ID
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', queueRoleDefinitionId)
    principalId: managedIdentityPrincipalId // Use managed identity ID
    principalType: 'ServicePrincipal' // Managed Identity is a Service Principal
  }
}

// Role assignment for Storage Account (Queue) - User Identity
resource queueRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableQueue && allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(storageAccount.id, userIdentityPrincipalId, queueRoleDefinitionId)
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', queueRoleDefinitionId)
    principalId: userIdentityPrincipalId // Use user identity ID
    principalType: 'User' // User Identity is a User Principal
  }
}

// Role assignment for Storage Account (Table) - Managed Identity
resource tableRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableTable) {
  name: guid(storageAccount.id, managedIdentityPrincipalId, tableRoleDefinitionId) // Use managed identity ID
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', tableRoleDefinitionId)
    principalId: managedIdentityPrincipalId // Use managed identity ID
    principalType: 'ServicePrincipal' // Managed Identity is a Service Principal
  }
}

// Role assignment for Storage Account (Table) - User Identity
resource tableRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableTable && allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(storageAccount.id, userIdentityPrincipalId, tableRoleDefinitionId)
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', tableRoleDefinitionId)
    principalId: userIdentityPrincipalId // Use user identity ID
    principalType: 'User' // User Identity is a User Principal
  }
}

// Role assignment for Application Insights - Managed Identity
resource appInsightsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(applicationInsights.id, managedIdentityPrincipalId, monitoringRoleDefinitionId) // Use managed identity ID
  scope: applicationInsights
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', monitoringRoleDefinitionId)
    principalId: managedIdentityPrincipalId // Use managed identity ID
    principalType: 'ServicePrincipal' // Managed Identity is a Service Principal
  }
}

// Role assignment for Application Insights - User Identity
resource appInsightsRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(applicationInsights.id, userIdentityPrincipalId, monitoringRoleDefinitionId)
  scope: applicationInsights
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', monitoringRoleDefinitionId)
    principalId: userIdentityPrincipalId // Use user identity ID
    principalType: 'User' // User Identity is a User Principal
  }
}

// Role assignment for Communication Services - Managed Identity
resource communicationServicesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(communicationServices.id, managedIdentityPrincipalId, communicationContributorRoleDefinitionId)
  scope: communicationServices
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', communicationContributorRoleDefinitionId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignment for Communication Services - User Identity
resource communicationServicesRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(communicationServices.id, userIdentityPrincipalId, communicationContributorRoleDefinitionId)
  scope: communicationServices
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', communicationContributorRoleDefinitionId)
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}
