using FluentAssertions;
using Xunit;
using System.Text.Json;
using AcsEmailMcp.Tools;
using Azure.Communication.Email;
using Azure;
using Moq;

namespace AcsEmailMcp.Tests;

public class EmailErrorHandlingTests
{
    [Fact]
    public async Task SendEmail_ShouldReturnValidationError_WhenSenderAddressIsEmpty()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();
        
        // Ensure no default sender address is set
        Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", null);
        
        // Act
        var result = await EmailTools.SendEmail(
            mockEmailClient.Object,
            "test@example.com", // recipient
            "Test Subject",     // subject
            "Test Body");       // body (senderAddress will use default empty string)

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Be("No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set");
        response.GetProperty("Message").GetString().Should().Be("Sender address is required");
    }

    [Fact]
    public async Task SendEmail_ShouldReturnValidationError_WhenSenderAddressIsInvalidFormat()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();
        
        // Act
        var result = await EmailTools.SendEmail(
            mockEmailClient.Object,
            "test@example.com", // recipient
            "Test Subject",     // subject
            "Test Body",        // body
            "invalid-email");   // senderAddress (invalid format)

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Be("Sender address must be a valid email format");
        response.GetProperty("Message").GetString().Should().Be("Invalid sender address format");
    }

    [Fact]
    public async Task SendSimpleEmail_ShouldReturnValidationError_WhenSenderAddressIsEmpty()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();
        
        // Ensure no default sender address is set
        Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", null);
        
        // Act
        var result = await EmailTools.SendSimpleEmail(
            mockEmailClient.Object,
            "test@example.com", // recipient
            "Test Subject",     // subject
            "Test Body");       // plainTextBody (senderAddress will use default empty string)

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Be("No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set");
        response.GetProperty("Message").GetString().Should().Be("Sender address is required");
    }

    [Fact]
    public async Task SendIncidentEmail_ShouldReturnValidationError_WhenSenderAddressIsInvalid()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();
        var incidentVariables = JsonSerializer.Serialize(new
        {
            SystemName = "Test System",
            IncidentId = "INC-001",
            StartTime = "2024-01-01 10:00",
            ImpactDescription = "Test impact"
        });
        
        // Act
        var result = await EmailTools.SendIncidentEmail(
            mockEmailClient.Object,
            "test@example.com", // recipient
            incidentVariables,  // incidentVariables
            "invalid-email");   // senderAddress (invalid format)

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Be("Sender address must be a valid email format");
        response.GetProperty("Message").GetString().Should().Be("Invalid sender address format");
    }

    [Theory]
    [InlineData("test@mydomain.com", null)] // Valid custom domain
    [InlineData("donotreply@abc123-def4-5678-9012-abcdef123456.azurecomm.net", null)] // Valid Azure managed domain
    [InlineData("", "Sender address cannot be empty")] // Empty
    [InlineData("invalid", "Sender address must be a valid email format")] // No @ or .
    [InlineData("test@", "Sender address must be a valid email format")] // No domain
    [InlineData("@example.com", "Sender address must be a valid email format")] // No local part
    [InlineData("it-support@example.com", "The sender domain appears to be a placeholder domain (example.com/org/net). Please use an Azure managed domain (recommended): donotreply@{domain-guid}.azurecomm.net or a custom verified domain linked to your Azure Communication Service resource.")] // Placeholder domain example.com
    [InlineData("noreply@example.org", "The sender domain appears to be a placeholder domain (example.com/org/net). Please use an Azure managed domain (recommended): donotreply@{domain-guid}.azurecomm.net or a custom verified domain linked to your Azure Communication Service resource.")] // Placeholder domain example.org
    [InlineData("admin@EXAMPLE.NET", "The sender domain appears to be a placeholder domain (example.com/org/net). Please use an Azure managed domain (recommended): donotreply@{domain-guid}.azurecomm.net or a custom verified domain linked to your Azure Communication Service resource.")] // Placeholder domain example.net (case insensitive)
    public void ValidateSenderAddress_ShouldReturnExpectedResult(string senderAddress, string? expectedError)
    {
        // Use reflection to call the private ValidateSenderAddress method
        var method = typeof(EmailTools).GetMethod("ValidateSenderAddress", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method?.Invoke(null, new object[] { senderAddress }) as string;

        // Assert
        result.Should().Be(expectedError);
    }
}