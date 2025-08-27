# Azure Communication Services Email MCP Tool - Architecture Diagrams

This directory contains professional draw.io diagrams documenting the architecture of the Azure Communication Services Email MCP Tool. The diagrams are designed using Microsoft Azure colors and official Azure stencils to provide clear, visually appealing documentation suitable for different audiences.

## Diagram Overview

### 1. Solution Overview (`01-solution-overview.drawio`)
**Target Audience:** Beginners, Executives, Business Stakeholders

**Purpose:** High-level overview of the complete solution showing how AI agents interact with the MCP server hosted on Azure Functions to send emails via Azure Communication Services.

**Key Components:**
- MCP (Model Context Protocol) workflow
- Azure Functions hosting architecture
- Azure Communication Services integration
- Email delivery process
- Key features and benefits

### 2. Infrastructure Architecture (`02-infrastructure-architecture.drawio`)
**Target Audience:** Cloud Engineers, Infrastructure Architects, Technical Teams

**Purpose:** Detailed view of all Azure resources, their relationships, and network topology.

**Key Components:**
- Complete Azure resource inventory
- Resource naming conventions
- Network architecture (VNet integration)
- Storage and compute components
- Security boundaries and access controls
- Configuration details and constraints

### 3. Security & Authentication Flow (`03-security-authentication-flow.drawio`)
**Target Audience:** Security Architects, Compliance Teams, DevSecOps Engineers

**Purpose:** Comprehensive security model showing authentication flows for both client access and service-to-service communication.

**Key Components:**
- OAuth2/OpenID Connect client authentication
- PKCE (Proof Key for Code Exchange) flow
- Managed Identity service authentication
- RBAC role assignments and permissions
- API Management security policies
- Zero Trust security principles

### 4. Email Service Flow (`04-email-service-flow.drawio`)
**Target Audience:** Developers, Application Architects, Technical Implementers

**Purpose:** Detailed workflow of email processing from MCP tool invocation through delivery and monitoring.

**Key Components:**
- MCP tool invocation process
- Email validation and processing
- Domain configuration (Azure-managed vs custom)
- SMTP delivery and tracking
- Error handling scenarios
- Performance monitoring
- Email types and content handling

### 5. CI/CD & Deployment Architecture (`05-cicd-deployment-architecture.drawio`)
**Target Audience:** DevOps Engineers, Release Managers, Platform Teams

**Purpose:** Complete development lifecycle from code changes through deployment and operations.

**Key Components:**
- Development environment setup
- GitHub Actions CI/CD pipeline
- Quality gates and security scanning
- Azure deployment process (azd)
- Infrastructure as Code (Bicep)
- Monitoring and operations
- Compliance and governance

### 6. MCP Connection Scenarios (`06-mcp-connection-scenarios.drawio`)
**Target Audience:** Developers, Solution Architects, Security Teams

**Purpose:** Visual comparison of the two primary MCP server connection methods showing authentication flows and security implications.

**Key Components:**
- Scenario 1: Direct Function App connection with function key authentication
- Scenario 2: API Management gateway with OAuth2/Entra ID authentication
- Security comparison and authentication flow differences
- Configuration requirements for each scenario
- Visual distinction using modern Azure governance color scheme

## Design Standards

All diagrams follow these standards:

### Color Scheme
Based on Azure Governance Services modern design:
- **Primary Background:** `#1B3B4D` - Dark blue/teal background for modern appearance
- **Primary Accent:** `#00B7C3` - Bright cyan for main Azure services and primary flows
- **Secondary Accent:** `#40E0D0` - Light cyan for communication services and data flows
- **Authentication Purple:** `#8B5FBF` - Purple for security components and authentication (Entra ID, APIM policies)
- **Management Purple:** `#9C4F96` - Darker purple for management and governance components
- **Success Green:** `#32CD32` - Success states, quality gates, email delivery
- **Warning Orange:** `#FF9800` - Monitoring, alerts, and observability
- **Text Light:** `#FFFFFF` - Primary text on dark backgrounds
- **Text Dark:** `#2D2D2D` - Secondary text on light components

### Azure Stencils
All diagrams use official Azure stencils from the [Microsoft Azure Architecture Icons](https://github.com/microsoft/Azure-Architecture-Icons) repository, ensuring consistency with Microsoft documentation and best practices.

### Layout Principles
- **Left-to-right flow:** Process flows move from left to right
- **Top-to-bottom hierarchy:** Higher-level concepts at the top
- **Grouped components:** Related services grouped in colored sections
- **Clear labeling:** All components clearly labeled with purpose
- **Legend inclusion:** Color coding explained in legends
- **Professional styling:** Enterprise-grade visual design

## Usage Guidelines

### For Beginners
Start with the **Solution Overview** diagram to understand the high-level concept, then move to the **Email Service Flow** for understanding the core functionality.

### For Cloud Engineers
Focus on the **Infrastructure Architecture** and **CI/CD Deployment** diagrams for implementation details and operational considerations.

### For Security Architects
Review the **Security & Authentication Flow** diagram for comprehensive security model and compliance requirements.

### For Developers
The **Email Service Flow** and **Solution Overview** provide the most relevant technical implementation details.

## File Formats

- **Source Format:** `.drawio` (Draw.io XML format)
- **Compatibility:** Can be opened in:
  - [Draw.io web application](https://app.diagrams.net/)
  - Draw.io desktop application
  - VS Code with Draw.io extension
  - Any text editor (XML format)

## Maintenance

These diagrams should be updated when:
- New Azure services are added to the architecture
- Security models change
- CI/CD processes are modified
- New features are added to the MCP tools
- Infrastructure topology changes

## Technical Validation

All diagrams have been validated for:
- ✅ Valid Draw.io XML syntax
- ✅ Accurate Azure service representations
- ✅ Consistent color scheme usage
- ✅ Professional visual design
- ✅ Clear information hierarchy
- ✅ Comprehensive documentation coverage

## Related Documentation

- [Architecture Decision Records](../architecture/architectural-decisions.md)
- [Developer Onboarding Guide](../developer/onboarding.md)
- [Project README](../../README.md)
- [Infrastructure as Code](../../infra/)

---

*These diagrams are designed to be professional, visually appealing, and suitable for technical presentations, documentation, and architectural reviews.*