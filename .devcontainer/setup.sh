#!/bin/bash

# Script to set up Azure Developer CLI and Azure Functions Core Tools
# in the devcontainer environment

set -e

echo "🚀 Setting up Azure MCP Email Server development environment..."

# Install Azure Developer CLI (azd)
echo "📦 Installing Azure Developer CLI..."
curl -fsSL https://aka.ms/install-azd.sh | bash


# Install Azure Functions Core Tools (Debian Bookworm compatible)
echo "📦 Installing Azure Functions Core Tools..."
# Install the Microsoft package repository GPG key
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo install -o root -g root -m 644 microsoft.gpg /etc/apt/trusted.gpg.d/
rm microsoft.gpg

# Detect OS and set up the correct APT source list
if [ -f /etc/debian_version ]; then
	# Debian-based
	if grep -qi bookworm /etc/os-release; then
		echo "Detected Debian Bookworm. Using Microsoft Debian repo."
		echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-functions-debian-bookworm bookworm main" | sudo tee /etc/apt/sources.list.d/azure-functions.list
	else
		echo "Non-Bookworm Debian detected. Please update setup.sh for your version."
		exit 1
	fi
else
	# Fallback for Ubuntu
	UBUNTU_CODENAME=$(lsb_release -cs 2>/dev/null || echo "jammy")
	echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-${UBUNTU_CODENAME}-prod ${UBUNTU_CODENAME} main" | sudo tee /etc/apt/sources.list.d/dotnetdev.list
fi

# Update package list and install Azure Functions Core Tools
sudo apt-get update
sudo apt-get install -y azure-functions-core-tools-4

# Restore .NET packages
echo "📦 Restoring .NET packages..."
dotnet restore

# Verify installations
echo "🔍 Verifying installations..."
echo "- .NET version: $(dotnet --version)"
echo "- Azure CLI version: $(az version --output table 2>/dev/null | head -n 1 || echo 'Not available yet (will be available after container restart)')"
echo "- Azure Developer CLI: $(azd version 2>/dev/null || echo 'Installed, restart shell to use')"
echo "- Azure Functions Core Tools: $(func --version 2>/dev/null || echo 'Installed, restart shell to use')"

echo ""
echo "✅ Development environment setup complete!"
echo ""
echo "🎯 Quick start commands:"
echo "  - Build project: dotnet build"
echo "  - Run locally: func start (after building)"
echo "  - Deploy to Azure: azd up"
echo ""
echo "📖 See README.md for full documentation"