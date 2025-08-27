@description('Name of the Email Communication Services resource')
param emailServicesName string

@description('Location for Email Services') 
param location string = resourceGroup().location

@description('Tags to apply to the resource')
param tags object = {}

@description('The domain name to be configured for the Email Service')
param domainName string

@description('User engagement tracking is enabled or disabled')
param userEngagementTracking bool = false

// Email Communication Services resource
resource emailServices 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServicesName
  location: 'global' // Email Services is a global service
  tags: tags
  properties: {
    dataLocation: 'United States'
  }
}

// Email domain resource
resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailServices
  name: domainName
  location: 'global'
  tags: tags
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: userEngagementTracking ? 'Enabled' : 'Disabled'
  }
}

// Output the Email Services resource information
output name string = emailServices.name
output id string = emailServices.id
output domainName string = emailDomain.name
output domainId string = emailDomain.id
output mailFromSenderDomain string = emailDomain.properties.mailFromSenderDomain