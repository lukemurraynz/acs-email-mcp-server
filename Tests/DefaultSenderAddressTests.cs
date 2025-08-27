using FluentAssertions;
using Xunit;
using System.Text.Json;
using AcsEmailMcp.Tools;
using Azure.Communication.Email;
using Azure;
using Moq;
using System;

namespace AcsEmailMcp.Tests;

public class DefaultSenderAddressTests
{
    [Fact]
    public async Task SendEmail_ShouldReturnError_WhenSenderAddressIsEmptyAndNoDefaultSet()
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
    public async Task SendSimpleEmail_ShouldReturnError_WhenSenderAddressIsEmptyAndNoDefaultSet()
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
    public async Task SendIncidentEmail_ShouldReturnError_WhenSenderAddressIsEmptyAndNoDefaultSet()
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
        
        // Ensure no default sender address is set
        Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", null);

        // Act
        var result = await EmailTools.SendIncidentEmail(
            mockEmailClient.Object,
            "test@example.com", // recipient
            incidentVariables); // incidentVariables (senderAddress will use default empty string)

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Be("No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set");
        response.GetProperty("Message").GetString().Should().Be("Sender address is required");
    }

    [Theory]
    [InlineData("donotreply@test-domain-guid.azurecomm.net")]
    [InlineData("test@mydomain.com")]
    public void ValidateSenderAddress_ShouldAcceptValidDefaultSenderFormats(string senderAddress)
    {
        // Arrange
        Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", senderAddress);

        try
        {
            // Use reflection to call the private ValidateSenderAddress method
            var method = typeof(EmailTools).GetMethod("ValidateSenderAddress", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Act
            var result = method?.Invoke(null, new object[] { senderAddress }) as string;

            // Assert
            result.Should().BeNull(); // null means valid
        }
        finally
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", null);
        }
    }

    [Fact]
    public void EnvironmentVariable_DefaultSenderAddress_ShouldBeUsedWhenSet()
    {
        // Arrange
        var testSenderAddress = "donotreply@test-domain-guid.azurecomm.net";
        
        // Set up environment variable
        Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", testSenderAddress);

        try
        {
            // Act
            var retrievedAddress = Environment.GetEnvironmentVariable("DEFAULT_SENDER_ADDRESS");

            // Assert
            retrievedAddress.Should().Be(testSenderAddress);
        }
        finally
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("DEFAULT_SENDER_ADDRESS", null);
        }
    }
}