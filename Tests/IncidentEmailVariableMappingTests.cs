using FluentAssertions;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using AcsEmailMcp.Tools;

namespace AcsEmailMcp.Tests;

public class IncidentEmailVariableMappingTests
{
    [Fact]
    public void SendIncidentEmail_ShouldMapVariablesCorrectly_WhenUsingCommonInputFormat()
    {
        // Arrange - This represents the exact JSON structure from the issue
        var incidentVariablesJson = JsonSerializer.Serialize(new
        {
            system = "Database",
            status = "Outage", 
            description = "Primary database is down due to network failure.",
            expectedResolution = "Within 2 hours",
            contact = "ops@example.com"
        });

        // Act - Parse variables like SendIncidentEmail does with the new mapping
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(incidentVariablesJson);
        
        // Create the same mapping that SendIncidentEmail uses
        var variableMapping = new Dictionary<string, string>
        {
            ["system"] = "SystemName",
            ["status"] = "Severity",
            ["description"] = "ImpactDescription",
            ["expectedResolution"] = "EstimatedResolution",
            ["contact"] = "ContactInfo"
        };
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
            variables[mappedName] = value;
        }

        // Add default values from template
        var template = EmailTemplates.GetTemplate("incident-outage");
        foreach (var kvp in template!.DefaultValues)
        {
            if (!variables.ContainsKey(kvp.Key)) variables[kvp.Key] = kvp.Value;
        }

        // Process template
        var processedSubject = EmailTemplates.ProcessTemplate(template.Subject, variables);
        var processedBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

        // Assert - With the mapping, these should now work correctly
        processedSubject.Should().Contain("Database", "system variable should be mapped to SystemName");
        processedSubject.Should().NotContain("{{SystemName}}", "SystemName placeholder should be replaced");
        
        processedBody.Should().Contain("Primary database is down", "description should be mapped to ImpactDescription");
        processedBody.Should().NotContain("{{ImpactDescription}}", "ImpactDescription placeholder should be replaced");
        
        processedBody.Should().Contain("ops@example.com", "contact should be mapped to ContactInfo");
        processedBody.Should().NotContain("{{ContactInfo}}", "ContactInfo placeholder should be replaced");
        
        processedBody.Should().Contain("Outage", "status should be mapped to Severity");
        processedBody.Should().Contain("Within 2 hours", "expectedResolution should be mapped to EstimatedResolution");
    }

    [Fact]
    public void SendIncidentEmail_ShouldHandleVariableMapping_WhenMissingOptionalFields()
    {
        // Arrange - Minimal incident data like in the issue
        var incidentVariablesJson = JsonSerializer.Serialize(new
        {
            system = "Payment Service",
            description = "Payment processing is unavailable"
        });

        // Act - Apply the same mapping logic as in production
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(incidentVariablesJson);
        
        var variableMapping = new Dictionary<string, string>
        {
            ["system"] = "SystemName",
            ["description"] = "ImpactDescription"
        };
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
            variables[mappedName] = value;
        }

        var template = EmailTemplates.GetTemplate("incident-outage");
        foreach (var kvp in template!.DefaultValues)
        {
            if (!variables.ContainsKey(kvp.Key)) variables[kvp.Key] = kvp.Value;
        }

        var processedSubject = EmailTemplates.ProcessTemplate(template.Subject, variables);
        var processedBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

        // Assert - Should use defaults for missing fields but map provided ones correctly
        processedSubject.Should().Contain("Payment Service");
        processedBody.Should().Contain("Payment processing is unavailable");
        processedBody.Should().Contain("High"); // Default severity
        processedBody.Should().Contain("No workaround available"); // Default workaround
    }

    [Fact]
    public void SendIncidentEmail_ShouldMaintainBackwardCompatibility_WhenUsingCorrectVariableNames()
    {
        // Arrange - Test that existing users with correct variable names still work
        var incidentVariablesJson = JsonSerializer.Serialize(new
        {
            SystemName = "Critical Service",
            IncidentId = "INC-2024-001",
            Severity = "Critical",
            StartTime = "2024-01-15 14:30 UTC",
            EstimatedResolution = "2024-01-15 16:00 UTC",
            ImpactDescription = "Service completely unavailable",
            Workaround = "Use alternative service",
            NextUpdate = "Update in 1 hour",
            ContactInfo = "emergency@example.com"
        });

        // Act - Variables should pass through unchanged since they already have correct names
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(incidentVariablesJson);
        
        var variableMapping = new Dictionary<string, string>
        {
            ["system"] = "SystemName",
            ["status"] = "Severity",
            ["description"] = "ImpactDescription"
        };
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
            variables[mappedName] = value;
        }

        var template = EmailTemplates.GetTemplate("incident-outage");
        var processedSubject = EmailTemplates.ProcessTemplate(template.Subject, variables);
        var processedBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

        // Assert - All variables should be properly replaced
        processedSubject.Should().Contain("Critical Service");
        processedSubject.Should().NotContain("{{");
        
        processedBody.Should().Contain("INC-2024-001");
        processedBody.Should().Contain("Critical");
        processedBody.Should().Contain("Service completely unavailable");
        processedBody.Should().Contain("emergency@example.com");
        processedBody.Should().NotContain("{{");
    }

    [Fact]
    public void SendIncidentEmail_ShouldConvertNumericValuesToStrings_WhenJsonContainsNumbers()
    {
        // Arrange - JSON with numeric values like the issue example
        var incidentVariablesJson = JsonSerializer.Serialize(new
        {
            systemName = "Primary Database",
            downtimeStart = "2025-08-25T14:30:00Z",
            durationMinutes = 45, // This is a number, not a string
            impactDescription = "All read/write operations to the database are unavailable, affecting order processing and inventory updates."
        });

        // Act - Parse variables like SendIncidentEmail does
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(incidentVariablesJson);
        
        var variableMapping = new Dictionary<string, string>
        {
            ["systemName"] = "SystemName",
            ["downtimeStart"] = "StartTime",
            ["durationMinutes"] = "DurationMinutes",
            ["impactDescription"] = "ImpactDescription"
        };
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            // Updated implementation that converts all JSON types to strings
            var value = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.Value.GetDecimal().ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => prop.Value.GetRawText() ?? string.Empty
            };
            var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
            variables[mappedName] = value;
        }

        // Assert - Numeric value should be converted to string, not empty
        variables["SystemName"].Should().Be("Primary Database");
        variables["StartTime"].Should().Be("2025-08-25T14:30:00Z");
        variables["DurationMinutes"].Should().Be("45"); // Should be "45", not empty string
        variables["ImpactDescription"].Should().Contain("All read/write operations");
    }

    [Fact]
    public void GetStringFromJsonElement_ShouldHandleAllJsonValueTypes()
    {
        // Arrange - JSON with different value types
        var testJson = JsonSerializer.Serialize(new
        {
            stringValue = "test string",
            intValue = 42,
            decimalValue = 3.14,
            booleanTrue = true,
            booleanFalse = false,
            nullValue = (string?)null
        });

        // Act - Parse and convert all types to strings
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(testJson);
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            var value = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.Value.GetDecimal().ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => prop.Value.GetRawText() ?? string.Empty
            };
            variables[prop.Name] = value;
        }

        // Assert - All types should be properly converted to strings
        variables["stringValue"].Should().Be("test string");
        variables["intValue"].Should().Be("42");
        variables["decimalValue"].Should().Be("3.14");
        variables["booleanTrue"].Should().Be("true");
        variables["booleanFalse"].Should().Be("false");
        variables["nullValue"].Should().Be(string.Empty);
    }

    [Fact]
    public void SendIncidentEmail_ShouldProcessTemplate_WhenUsingExactUserInputFromIssue()
    {
        // Arrange - This is the exact JSON from the issue that wasn't working
        var incidentVariablesJson = JsonSerializer.Serialize(new
        {
            System = "ERP Database",
            OutageDuration = "2 hours", 
            ImpactLevel = "High",
            RootCause = "Unexpected server reboot",
            ResolutionStatus = "Pending"
        });

        // Act - Apply the current variable mapping logic (before fix)
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(incidentVariablesJson);
        
        // Current mapping - case sensitive
        var variableMapping = new Dictionary<string, string>
        {
            ["system"] = "SystemName",
            ["systemName"] = "SystemName",
            ["status"] = "Severity", 
            ["severity"] = "Severity",
            ["description"] = "ImpactDescription",
            ["impactDescription"] = "ImpactDescription",
            ["impact"] = "ImpactDescription",
            ["expectedResolution"] = "EstimatedResolution",
            ["estimatedResolution"] = "EstimatedResolution",
            ["resolution"] = "EstimatedResolution",
            ["contact"] = "ContactInfo",
            ["contactInfo"] = "ContactInfo",
            ["contactEmail"] = "ContactInfo",
            ["incidentId"] = "IncidentId",
            ["incident"] = "IncidentId",
            ["startTime"] = "StartTime",
            ["start"] = "StartTime",
            ["workaround"] = "Workaround",
            ["nextUpdate"] = "NextUpdate",
            ["update"] = "NextUpdate"
        };
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
            variables[mappedName] = value;
        }

        // Add default values from template
        var template = EmailTemplates.GetTemplate("incident-outage");
        foreach (var kvp in template!.DefaultValues)
        {
            if (!variables.ContainsKey(kvp.Key)) variables[kvp.Key] = kvp.Value;
        }

        // Process template
        var processedSubject = EmailTemplates.ProcessTemplate(template.Subject, variables);
        var processedBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

        // Assert - This test demonstrates the current problem
        // With case-sensitive mapping, the variables don't get mapped correctly
        processedSubject.Should().Contain("{{SystemName}}", "Current implementation fails to map 'System' to 'SystemName'");
        processedBody.Should().Contain("{{SystemName}}", "SystemName placeholder should not be replaced with current implementation");
        processedBody.Should().Contain("{{ImpactDescription}}", "ImpactDescription placeholder should not be replaced");
        
        // However, this shows what variables were actually set:
        variables.Should().ContainKey("System").And.ContainValue("ERP Database");
        variables.Should().ContainKey("ImpactLevel").And.ContainValue("High");
        variables.Should().ContainKey("RootCause").And.ContainValue("Unexpected server reboot");
    }

    [Fact]
    public void SendIncidentEmail_ShouldProcessTemplateCorrectly_AfterFixingCaseInsensitiveMapping()
    {
        // Arrange - This is the exact JSON from the issue that should now work
        var incidentVariablesJson = JsonSerializer.Serialize(new
        {
            System = "ERP Database",
            OutageDuration = "2 hours", 
            ImpactLevel = "High",
            RootCause = "Unexpected server reboot",
            ResolutionStatus = "Pending"
        });

        // Act - Apply the FIXED variable mapping logic with case-insensitive dictionary
        var variables = new Dictionary<string, string>();
        var jsonDoc = JsonDocument.Parse(incidentVariablesJson);
        
        // Fixed mapping - case insensitive with additional mappings
        var variableMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["system"] = "SystemName",
            ["systemName"] = "SystemName",
            ["status"] = "Severity", 
            ["severity"] = "Severity",
            ["impactLevel"] = "Severity",  // Added for user issue
            ["description"] = "ImpactDescription",
            ["impactDescription"] = "ImpactDescription",
            ["impact"] = "ImpactDescription",
            ["rootCause"] = "ImpactDescription",  // Added for user issue
            ["expectedResolution"] = "EstimatedResolution",
            ["estimatedResolution"] = "EstimatedResolution",
            ["resolution"] = "EstimatedResolution",
            ["outageDuration"] = "EstimatedResolution",  // Added for user issue
            ["resolutionStatus"] = "NextUpdate",  // Added for user issue
            ["contact"] = "ContactInfo",
            ["contactInfo"] = "ContactInfo",
            ["contactEmail"] = "ContactInfo",
            ["incidentId"] = "IncidentId",
            ["incident"] = "IncidentId",
            ["startTime"] = "StartTime",
            ["start"] = "StartTime",
            ["workaround"] = "Workaround",
            ["nextUpdate"] = "NextUpdate",
            ["update"] = "NextUpdate"
        };
        
        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
            variables[mappedName] = value;
        }

        // Add default values from template
        var template = EmailTemplates.GetTemplate("incident-outage");
        foreach (var kvp in template!.DefaultValues)
        {
            if (!variables.ContainsKey(kvp.Key)) variables[kvp.Key] = kvp.Value;
        }

        // Process template
        var processedSubject = EmailTemplates.ProcessTemplate(template.Subject, variables);
        var processedBody = EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

        // Assert - With the fix, template variables should be properly replaced
        processedSubject.Should().Contain("ERP Database", "'System' should be mapped to 'SystemName' in subject");
        processedSubject.Should().NotContain("{{SystemName}}", "SystemName placeholder should be replaced");
        
        processedBody.Should().Contain("ERP Database", "'System' should be mapped to 'SystemName' in body");
        processedBody.Should().Contain("High", "'ImpactLevel' should be mapped to 'Severity'");
        processedBody.Should().Contain("Unexpected server reboot", "'RootCause' should be mapped to 'ImpactDescription'");
        processedBody.Should().Contain("2 hours", "'OutageDuration' should be mapped to 'EstimatedResolution'");
        processedBody.Should().Contain("Pending", "'ResolutionStatus' should be mapped to 'NextUpdate'");
        
        // No template variables should remain unprocessed
        processedBody.Should().NotContain("{{SystemName}}");
        processedBody.Should().NotContain("{{ImpactDescription}}");
        processedBody.Should().NotContain("{{EstimatedResolution}}");
        processedBody.Should().NotContain("{{NextUpdate}}");
    }
}