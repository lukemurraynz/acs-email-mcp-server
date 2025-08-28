
# ACS Email MCP Server

This project is a C# .NET 8 MCP (Model Context Protocol) server, designed to run as an Azure Functions custom handler. It provides advanced email automation tools using Azure Communication Services (ACS), with support for templates, attachments, and robust error handling.

## Features

- **MCP Server**: Implements the Model Context Protocol using the official SDK and ASP.NET Core.
- **Azure Functions Custom Handler**: Deployable as a serverless app for scalable, event-driven workloads.
- **Email Tools**: Send emails (HTML or plain text), use templates, add attachments, and track operations via ACS.
- **Health & Readiness Endpoints**: `/api/healthz` and `/api/ready` for monitoring and automation.
- **Infrastructure as Code**: Bicep templates for easy Azure deployment.
- **Comprehensive Tests**: Unit and integration tests for all major features.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0?WT.mc_id=AZ-MVP-5004796)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?WT.mc_id=AZ-MVP-5004796)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd?WT.mc_id=AZ-MVP-5004796)
- [Visual Studio Code](https://code.visualstudio.com/) (recommended)
- An Azure subscription with [Communication Services Email](https://learn.microsoft.com/azure/communication-services/quickstarts/email/send-email?WT.mc_id=AZ-MVP-5004796)

## Getting Started

1. **Clone and setup**
  ```pwsh
  git clone https://github.com/lukemurraynz/acs-email-mcp.git
  cd acs-email-mcp
  dotnet restore
  ```
2. **Configure environment**
  - Set `ACS_ENDPOINT` and `DEFAULT_SENDER_ADDRESS` in your environment or `local.settings.json`.
  - For production, configure managed identity and verified sender domains in Azure.
3. **Run locally**
  ```pwsh
  func start
  ```
4. **Run tests**
  ```pwsh
  dotnet test
  ```

## Email Tools Overview

The server exposes the following MCP tools (see `Tools/EmailTools.cs`):

- **SendEmail**: Send an email with full options (HTML/plain text, attachments, sender/recipient names).
- **SendSimpleEmail**: Send a plain text email with minimal parameters.
- **SendEmailWithMultipleAttachments**: Send an email with multiple attachments.
- **SendIncidentEmail**: Use a predefined incident template for outage notifications.
- **ListEmailTemplates**: List available templates and required variables.

### Example Usage

```
Send an email to john@example.com with subject 'Meeting Reminder' and body 'Don't forget about our meeting tomorrow at 2pm'
Send a simple email to team@example.com about the project update
Send an incident email to ops@example.com with incidentVariables '{"SystemName":"CRM","Severity":"High"}'
```

## Deployment

Deploy to Azure using the Developer CLI:

```pwsh
azd up
```

This will provision all required resources and deploy the app. See `infra/` for Bicep templates.

## Troubleshooting

- **DomainNotLinked**: Ensure your sender domain is verified and linked in ACS. See [Domain Configuration Troubleshooting](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-domain-configuration-troubleshooting?WT.mc_id=AZ-MVP-5004796).
- **Unauthorized**: Check managed identity permissions for your Communication Services resource.
- **Invalid Sender Address**: Use a sender from a verified domain (e.g., `donotreply@{domain-guid}.azurecomm.net`).

## Documentation

- [Diagrams](./docs/diagrams/)

## License

MIT License. See [LICENSE](LICENSE).
