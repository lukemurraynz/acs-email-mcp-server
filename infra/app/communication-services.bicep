@description('Name of the Communication Services resource')
param communicationServicesName string

@description('Location for Communication Services')
param location string = resourceGroup().location

@description('Tags to apply to the resource')
param tags object = {}

@description('The data location where the Communication Services stores its data at rest')
@allowed([
  'Africa'
  'Asia Pacific'
  'Australia'
  'Brazil'
  'Canada'
  'Europe'
  'France'
  'Germany'
  'India'
  'Japan'
  'Korea'
  'Norway'
  'Switzerland'
  'UAE'
  'UK'
  'United States'
])
param dataLocation string = 'United States'

@description('Array of email domain resource IDs to link to this Communication Services resource')
param linkedDomains array = []

// Communication Services resource
resource communicationServices 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServicesName
  location: 'global' // Communication Services is a global service
  tags: tags
  properties: {
    dataLocation: dataLocation
    linkedDomains: linkedDomains
  }
}

// Output the Communication Services resource information
output name string = communicationServices.name
output id string = communicationServices.id
output endpoint string = communicationServices.properties.hostName
output immutableResourceId string = communicationServices.properties.immutableResourceId