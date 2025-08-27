using FluentAssertions;
using Xunit;
using System.Text.Json;
using AcsEmailMcp.Tools;

namespace AcsEmailMcp.Tests;

public class EmailToolsIntegrationTests
{
    [Fact]
    public void EmailTemplate_Integration_ShouldProcessIncidentTemplate()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["SystemName"] = "Payment Service",
            ["IncidentId"] = "INC-2024-001",
            ["Severity"] = "Critical",
            ["StartTime"] = "2024-01-15 14:30 UTC",
            ["EstimatedResolution"] = "2024-01-15 16:00 UTC",
            ["ImpactDescription"] = "Users unable to process payments",
            ["Workaround"] = "Use backup payment gateway",
            ["NextUpdate"] = "Update in 30 minutes",
            ["ContactInfo"] = "support@example.com"
        };

        // Act
        var template = EmailTemplates.GetTemplate("incident-outage");
        var processedSubject = EmailTemplates.ProcessTemplate(template!.Subject, variables);
        var processedHtmlBody = EmailTemplates.ProcessTemplate(template.HtmlBody, variables);
        var processedPlainBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

        // Assert
        processedSubject.Should().Contain("Payment Service");
        processedSubject.Should().NotContain("{{SystemName}}");

        processedHtmlBody.Should().Contain("INC-2024-001");
        processedHtmlBody.Should().Contain("Critical");
        processedHtmlBody.Should().Contain("Users unable to process payments");
        processedHtmlBody.Should().NotContain("{{");

        processedPlainBody.Should().Contain("Payment Service");
        processedPlainBody.Should().Contain("support@example.com");
        processedPlainBody.Should().NotContain("{{");
    }

    [Fact]
    public void EmailTemplate_Integration_ShouldProcessWelcomeTemplate()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["CompanyName"] = "Microsoft",
            ["UserName"] = "John Doe",
            ["StartDate"] = "2024-01-15",
            ["Department"] = "Engineering",
            ["NextSteps"] = "Check your email for onboarding instructions"
        };

        // Act
        var template = EmailTemplates.GetTemplate("welcome");
        var processedSubject = EmailTemplates.ProcessTemplate(template!.Subject, variables);
        var processedHtmlBody = EmailTemplates.ProcessTemplate(template.HtmlBody, variables);

        // Assert
        processedSubject.Should().Be("Welcome to Microsoft, John Doe!");
        processedHtmlBody.Should().Contain("Hello John Doe");
        processedHtmlBody.Should().Contain("Microsoft");
        processedHtmlBody.Should().Contain("Engineering");
        processedHtmlBody.Should().Contain("Check your email for onboarding instructions");
    }

    [Fact]
    public void EmailTemplate_Integration_ShouldHandleDefaultValues()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["SystemName"] = "Database Server",
            ["IncidentId"] = "INC-2024-002"
            // Deliberately missing other variables to test defaults
        };

        // Act
        var template = EmailTemplates.GetTemplate("incident-outage");
        var allVariables = new Dictionary<string, string>(variables);
        
        // Add default values for missing keys
        foreach (var kvp in template!.DefaultValues)
        {
            if (!allVariables.ContainsKey(kvp.Key))
                allVariables[kvp.Key] = kvp.Value;
        }

        var processedSubject = EmailTemplates.ProcessTemplate(template.Subject, allVariables);
        var processedBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, allVariables);

        // Assert
        processedSubject.Should().Contain("Database Server");
        processedBody.Should().Contain("INC-2024-002");
        processedBody.Should().Contain("High"); // Default severity
        processedBody.Should().Contain("No workaround available"); // Default workaround
        processedBody.Should().Contain("support@example.com"); // Default contact info
    }

    [Fact]
    public void EmailTemplates_Integration_ShouldReturnAllAvailableTemplates()
    {
        // Act
        var availableTemplates = EmailTemplates.GetAvailableTemplates();

        // Assert
        availableTemplates.Should().NotBeEmpty();
        availableTemplates.Should().Contain("incident-outage");
        availableTemplates.Should().Contain("welcome");

        // Verify each template can be retrieved and has required properties
        foreach (var templateName in availableTemplates)
        {
            var template = EmailTemplates.GetTemplate(templateName);
            template.Should().NotBeNull();
            template!.Subject.Should().NotBeEmpty();
            template.HtmlBody.Should().NotBeEmpty();
            template.PlainTextBody.Should().NotBeEmpty();
            template.DefaultValues.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("{{Name}}", "John", "John")]
    [InlineData("Hello {{Name}}!", "Alice", "Hello Alice!")]
    [InlineData("{{First}} {{Last}}", "Bob", "Bob {{Last}}")] // Partial replacement
    [InlineData("No variables here", "Test", "No variables here")] // No variables
    [InlineData("", "Test", "")] // Empty template
    public void ProcessTemplate_Integration_ShouldHandleVariousScenarios(
        string template, string value, string expected)
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["Name"] = value,
            ["First"] = value
        };

        // Act
        var result = EmailTemplates.ProcessTemplate(template, variables);

        // Assert
        result.Should().Be(expected);
    }
}