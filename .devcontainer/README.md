# DevContainer Configuration for Azure MCP Email Server

This folder contains the devcontainer configuration for GitHub Codespaces support.

## Files

- `devcontainer.json` - Main devcontainer configuration
- `setup.sh` - Post-creation setup script that installs Azure tools

## What gets installed

- .NET 8.0 SDK (via base image)
- Azure CLI with Bicep support
- Node.js 20 (for Azure Functions Core Tools)
- Azure Developer CLI (azd)
- Azure Functions Core Tools v4
- VS Code extensions for Azure development

## Port forwarding

- Port 7071: Azure Functions local development
- Port 8080: Custom handler/MCP server

## Usage

1. Open repository in GitHub Codespaces
2. Wait for the setup script to complete
3. Start developing immediately with all tools ready