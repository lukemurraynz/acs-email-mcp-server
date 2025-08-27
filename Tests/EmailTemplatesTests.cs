using FluentAssertions;
using AcsEmailMcp.Tools;
using Xunit;

namespace AcsEmailMcp.Tests;

public class EmailTemplatesTests
{
    [Fact]
    public void GetTemplate_ShouldReturnIncidentTemplate_WhenValidTemplateName()
    {
        // Arrange
        var templateName = "incident-outage";

        // Act
        var template = EmailTemplates.GetTemplate(templateName);

        // Assert
        template.Should().NotBeNull();
        template!.Subject.Should().Contain("{{SystemName}}");
        template.HtmlBody.Should().Contain("{{IncidentId}}");
        template.PlainTextBody.Should().Contain("{{Severity}}");
        template.DefaultValues.Should().ContainKey("Severity");
    }

    [Fact]
    public void GetTemplate_ShouldReturnWelcomeTemplate_WhenValidTemplateName()
    {
        // Arrange
        var templateName = "welcome";

        // Act
        var template = EmailTemplates.GetTemplate(templateName);

        // Assert
        template.Should().NotBeNull();
        template!.Subject.Should().Contain("{{CompanyName}}");
        template.HtmlBody.Should().Contain("{{UserName}}");
        template.DefaultValues.Should().ContainKey("NextSteps");
    }

    [Fact]
    public void GetTemplate_ShouldReturnNull_WhenInvalidTemplateName()
    {
        // Arrange
        var templateName = "nonexistent-template";

        // Act
        var template = EmailTemplates.GetTemplate(templateName);

        // Assert
        template.Should().BeNull();
    }

    [Fact]
    public void GetAvailableTemplates_ShouldReturnAllTemplateNames()
    {
        // Act
        var templates = EmailTemplates.GetAvailableTemplates();

        // Assert
        templates.Should().NotBeNull();
        templates.Should().Contain("incident-outage");
        templates.Should().Contain("welcome");
        templates.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProcessTemplate_ShouldReplaceVariables_WhenValidVariablesProvided()
    {
        // Arrange
        var template = "Hello {{UserName}}, welcome to {{CompanyName}}!";
        var variables = new Dictionary<string, string>
        {
            ["UserName"] = "John Doe",
            ["CompanyName"] = "Microsoft"
        };

        // Act
        var result = EmailTemplates.ProcessTemplate(template, variables);

        // Assert
        result.Should().Be("Hello John Doe, welcome to Microsoft!");
    }

    [Fact]
    public void ProcessTemplate_ShouldHandleEmptyVariables_WhenNoVariablesProvided()
    {
        // Arrange
        var template = "Hello {{UserName}}, welcome to {{CompanyName}}!";
        var variables = new Dictionary<string, string>();

        // Act
        var result = EmailTemplates.ProcessTemplate(template, variables);

        // Assert
        result.Should().Be("Hello {{UserName}}, welcome to {{CompanyName}}!");
    }

    [Fact]
    public void ProcessTemplate_ShouldHandlePartialReplacement_WhenSomeVariablesMissing()
    {
        // Arrange
        var template = "Hello {{UserName}}, welcome to {{CompanyName}}!";
        var variables = new Dictionary<string, string>
        {
            ["UserName"] = "John Doe"
        };

        // Act
        var result = EmailTemplates.ProcessTemplate(template, variables);

        // Assert
        result.Should().Be("Hello John Doe, welcome to {{CompanyName}}!");
    }

    [Theory]
    [InlineData("incident-outage")]
    [InlineData("welcome")]
    public void EmailTemplate_ShouldHaveRequiredProperties_WhenCreated(string templateName)
    {
        // Act
        var template = EmailTemplates.GetTemplate(templateName);

        // Assert
        template.Should().NotBeNull();
        template!.Subject.Should().NotBeEmpty();
        template.HtmlBody.Should().NotBeEmpty();
        template.PlainTextBody.Should().NotBeEmpty();
        template.DefaultValues.Should().NotBeNull();
    }
}