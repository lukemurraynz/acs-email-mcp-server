using Azure;
using Azure.Communication.Email;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AcsEmailMcp.Tools;

public class EmailTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultValues { get; set; } = new();
}

public class EmailAttachmentInfo
{
    public string Content { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public static class EmailTemplates
{
    private static readonly Dictionary<string, EmailTemplate> Templates = new()
    {
        ["incident-outage"] = new EmailTemplate
        {
            Subject = "URGENT: IT Systems Outage - {{SystemName}} Affected",
            HtmlBody = "<html><body><h2>URGENT: IT Systems Outage</h2><p><strong>System Affected:</strong> {{SystemName}}</p><p><strong>Incident ID:</strong> {{IncidentId}}</p><p><strong>Severity:</strong> {{Severity}}</p><p><strong>Start Time:</strong> {{StartTime}}</p><p><strong>Estimated Resolution:</strong> {{EstimatedResolution}}</p><h3>Impact Description:</h3><p>{{ImpactDescription}}</p><h3>Workaround:</h3><p>{{Workaround}}</p><h3>Next Update:</h3><p>{{NextUpdate}}</p><hr><p><em>Automated notification</em></p><p>{{ContactInfo}}</p></body></html>",
            PlainTextBody = "URGENT: IT Systems Outage\nSystem Affected: {{SystemName}}\nIncident ID: {{IncidentId}}\nSeverity: {{Severity}}\nStart Time: {{StartTime}}\nEstimated Resolution: {{EstimatedResolution}}\nImpact Description:\n{{ImpactDescription}}\nWorkaround:\n{{Workaround}}\nNext Update:\n{{NextUpdate}}\nContact: {{ContactInfo}}",
            DefaultValues = new Dictionary<string, string>
            {
                ["Severity"] = "High",
                ["Workaround"] = "No workaround available at this time.",
                ["NextUpdate"] = "Update in 30 minutes.",
                ["ContactInfo"] = "support@example.com"
            }
        },
        ["welcome"] = new EmailTemplate
        {
            Subject = "Welcome to {{CompanyName}}, {{UserName}}!",
            HtmlBody = "<html><body><h2>Welcome to {{CompanyName}}!</h2><p>Hello {{UserName}}</p><p>Start Date: {{StartDate}}</p><p>Department: {{Department}}</p><p>{{NextSteps}}</p></body></html>",
            PlainTextBody = "Welcome to {{CompanyName}}! Hello {{UserName}}. Start Date: {{StartDate}} Department: {{Department}} Next Steps: {{NextSteps}}",
            DefaultValues = new Dictionary<string, string> { ["NextSteps"] = "Please review the onboarding guide." }
        }
    };

    public static EmailTemplate? GetTemplate(string templateName) => Templates.TryGetValue(templateName, out var t) ? t : null;
    public static string[] GetAvailableTemplates() => Templates.Keys.ToArray();
    public static string ProcessTemplate(string template, Dictionary<string, string> variables)
    {
        foreach (var kvp in variables)
        {
            template = template.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return template;
    }
}

[McpServerToolType]
public sealed class EmailTools
{
    private const int MAX_ATTACHMENT_SIZE = 25 * 1024 * 1024; // 25MB limit
    private const int MAX_TOTAL_ATTACHMENTS_SIZE = 25 * 1024 * 1024; // 25MB total limit

    private static async Task<(EmailAttachment?, string?)> ProcessAttachmentAsync(
        string attachmentFilePath,
        string attachmentContent,
        string attachmentFileName,
        string attachmentMimeType)
    {
        try
        {
            byte[] attachmentBytes;
            string fileName;
            string mimeType;

            if (!string.IsNullOrEmpty(attachmentFilePath))
            {
                if (!File.Exists(attachmentFilePath))
                {
                    return (null, JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Error = $"Attachment file not found: {attachmentFilePath}",
                        Message = "Failed to send email due to missing attachment file"
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
                attachmentBytes = await File.ReadAllBytesAsync(attachmentFilePath);
                fileName = string.IsNullOrEmpty(attachmentFileName) ? Path.GetFileName(attachmentFilePath) : attachmentFileName;
                mimeType = string.IsNullOrEmpty(attachmentMimeType) ? GetMimeType(attachmentFilePath) : attachmentMimeType;
            }
            else if (!string.IsNullOrEmpty(attachmentContent) && !string.IsNullOrEmpty(attachmentFileName))
            {
                attachmentBytes = Convert.FromBase64String(attachmentContent);
                fileName = attachmentFileName;
                // Improved MIME type detection: use filename if no explicit MIME type provided
                mimeType = string.IsNullOrEmpty(attachmentMimeType) ? GetMimeTypeFromFileName(attachmentFileName) : attachmentMimeType;
            }
            else
            {
                return (null, JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = "No valid attachment source provided",
                    Message = "Failed to process attachment: no file path or content provided"
                }, new JsonSerializerOptions { WriteIndented = true }));
            }

            // Validate attachment size
            if (attachmentBytes.Length > MAX_ATTACHMENT_SIZE)
            {
                return (null, JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = $"Attachment size ({attachmentBytes.Length:N0} bytes) exceeds maximum allowed size ({MAX_ATTACHMENT_SIZE:N0} bytes)",
                    Message = "Failed to process attachment due to size limit exceeded"
                }, new JsonSerializerOptions { WriteIndented = true }));
            }

            return (new EmailAttachment(fileName, mimeType, new BinaryData(attachmentBytes)), null);
        }
        catch (FormatException)
        {
            return (null, JsonSerializer.Serialize(new { Success = false, Error = "Invalid base64 format for attachment content", Message = "Failed to process attachment due to invalid base64 format" }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (IOException ex)
        {
            return (null, JsonSerializer.Serialize(new { Success = false, Error = $"Error reading attachment file: {ex.Message}", Message = "Failed to process attachment due to file access error" }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return (null, JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to process attachment due to unexpected error" }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static async Task<(List<EmailAttachment>?, string?)> ProcessMultipleAttachmentsAsync(IEnumerable<EmailAttachmentInfo> attachments)
    {
        try
        {
            var emailAttachments = new List<EmailAttachment>();
            var totalSize = 0L;

            foreach (var attachmentInfo in attachments)
            {
                var (attachment, errorJson) = await ProcessAttachmentAsync(
                    attachmentInfo.FilePath,
                    attachmentInfo.Content,
                    attachmentInfo.FileName,
                    attachmentInfo.MimeType);

                if (errorJson != null)
                {
                    return (null, errorJson);
                }

                if (attachment != null)
                {
                    // Check total size
                    var attachmentSize = attachment.Content.ToArray().Length;
                    totalSize += attachmentSize;

                    if (totalSize > MAX_TOTAL_ATTACHMENTS_SIZE)
                    {
                        return (null, JsonSerializer.Serialize(new
                        {
                            Success = false,
                            Error = $"Total attachments size ({totalSize:N0} bytes) exceeds maximum allowed size ({MAX_TOTAL_ATTACHMENTS_SIZE:N0} bytes)",
                            Message = "Failed to process attachments due to total size limit exceeded"
                        }, new JsonSerializerOptions { WriteIndented = true }));
                    }

                    emailAttachments.Add(attachment);
                }
            }

            return (emailAttachments, null);
        }
        catch (Exception ex)
        {
            return (null, JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to process multiple attachments due to unexpected error" }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    [McpServerTool, Description("Send an email with multiple attachments using Azure Communication Services.")]
    public static async Task<string> SendEmailWithMultipleAttachments(
        EmailClient emailClient,
        [Description("The recipient email address.")] string recipientAddress,
        [Description("The email subject.")] string subject,
        [Description("The email body content (HTML supported).")] string body,
        [Description("Optional: The sender email address (must be from a verified domain). Leave empty to use default sender from environment.")] string senderAddress = "",
        [Description("Optional: The sender display name.")] string senderDisplayName = "",
        [Description("Optional: The recipient display name.")] string recipientDisplayName = "",
        [Description("Optional: Whether the body is HTML (default: false).")] bool isHtml = false,
        [Description("Optional: Array of attachments with Content (base64), FileName, and optional MimeType.")] EmailAttachmentInfo[]? attachments = null)
    {
        try
        {
            // Use default sender address from environment if none provided
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                senderAddress = Environment.GetEnvironmentVariable("DEFAULT_SENDER_ADDRESS") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(senderAddress))
                {
                    return JsonSerializer.Serialize(new { 
                        Success = false, 
                        Error = "No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set", 
                        Message = "Sender address is required" 
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
            }

            // Validate sender address
            var validationError = ValidateSenderAddress(senderAddress);
            if (validationError != null)
            {
                return JsonSerializer.Serialize(new { 
                    Success = false, 
                    Error = validationError, 
                    Message = "Invalid sender address format" 
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var emailContent = new EmailContent(subject)
            {
                Html = isHtml ? body : null,
                PlainText = isHtml ? null : body
            };

            var emailMessage = new EmailMessage(
                senderAddress,
                new EmailRecipients(new List<EmailAddress>
                {
                    new(recipientAddress, recipientDisplayName)
                }),
                emailContent);

            // Process multiple attachments if provided
            if (attachments != null && attachments.Length > 0)
            {
                var (emailAttachments, errorJson) = await ProcessMultipleAttachmentsAsync(attachments);
                if (errorJson != null) return errorJson;
                
                if (emailAttachments != null)
                {
                    foreach (var attachment in emailAttachments)
                    {
                        emailMessage.Attachments.Add(attachment);
                    }
                }
            }

            var op = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
            return JsonSerializer.Serialize(new
            {
                Success = op.HasCompleted,
                OperationId = op.Id,
                Status = op.HasCompleted ? op.Value.Status.ToString() : "InProgress",
                Message = op.HasCompleted ? "Email sent successfully" : "Email send operation is still in progress",
                AttachmentCount = attachments?.Length ?? 0
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (RequestFailedException ex)
        {
            return CreateErrorResponse(ex, "email with multiple attachments");
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to send email due to unexpected error" }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool, Description("Send an email using Azure Communication Services.")]
    public static async Task<string> SendEmail(
        EmailClient emailClient,
        [Description("The recipient email address.")] string recipientAddress,
        [Description("The email subject.")] string subject,
        [Description("The email body content (HTML supported).") ] string body,
        [Description("Optional: The sender email address (must be from a verified domain). Leave empty to use default sender from environment.") ] string senderAddress = "",
        [Description("Optional: The sender display name.")] string senderDisplayName = "",
        [Description("Optional: The recipient display name.")] string recipientDisplayName = "",
        [Description("Optional: Whether the body is HTML (default: false).") ] bool isHtml = false,
        [Description("Optional: Path to file for attachment (will be automatically encoded).") ] string attachmentFilePath = "",
        [Description("Optional: Base64 encoded file content for attachment (alternative).") ] string attachmentContent = "",
        [Description("Optional: Filename for the attachment.")] string attachmentFileName = "",
        [Description("Optional: MIME type of the attachment.")] string attachmentMimeType = "")
    {
        try
        {
            // Use default sender address from environment if none provided
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                senderAddress = Environment.GetEnvironmentVariable("DEFAULT_SENDER_ADDRESS") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(senderAddress))
                {
                    return JsonSerializer.Serialize(new { 
                        Success = false, 
                        Error = "No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set", 
                        Message = "Sender address is required" 
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
            }

            // Validate sender address
            var validationError = ValidateSenderAddress(senderAddress);
            if (validationError != null)
            {
                return JsonSerializer.Serialize(new { 
                    Success = false, 
                    Error = validationError, 
                    Message = "Invalid sender address format" 
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var emailContent = new EmailContent(subject)
            {
                Html = isHtml ? body : null,
                PlainText = isHtml ? null : body
            };

            var emailMessage = new EmailMessage(
                senderAddress,
                new EmailRecipients(new List<EmailAddress>
                {
                    new(recipientAddress, recipientDisplayName)
                }),
                emailContent);

            if (!string.IsNullOrEmpty(attachmentFilePath) || (!string.IsNullOrEmpty(attachmentContent) && !string.IsNullOrEmpty(attachmentFileName)))
            {
                var (attachment, errorJson) = await ProcessAttachmentAsync(attachmentFilePath, attachmentContent, attachmentFileName, attachmentMimeType);
                if (errorJson != null) return errorJson;
                if (attachment != null) emailMessage.Attachments.Add(attachment);
            }

            var op = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
            return JsonSerializer.Serialize(new
            {
                Success = op.HasCompleted,
                OperationId = op.Id,
                Status = op.HasCompleted ? op.Value.Status.ToString() : "InProgress",
                Message = op.HasCompleted ? "Email sent successfully" : "Email send operation is still in progress",
                HasAttachment = !string.IsNullOrEmpty(attachmentFilePath) || !string.IsNullOrEmpty(attachmentContent)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (RequestFailedException ex)
        {
            return CreateErrorResponse(ex, "email");
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to send email due to unexpected error" }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool, Description("Send a simple email using Azure Communication Services with plain text content.")]
    public static async Task<string> SendSimpleEmail(
        EmailClient emailClient,
        [Description("The recipient email address.")] string recipientAddress,
        [Description("The email subject.")] string subject,
        [Description("The plain text email body content.")] string plainTextBody,
        [Description("Optional: The sender email address (must be from a verified domain). Leave empty to use default sender from environment.") ] string senderAddress = "")
    {
        try
        {
            // Use default sender address from environment if none provided
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                senderAddress = Environment.GetEnvironmentVariable("DEFAULT_SENDER_ADDRESS") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(senderAddress))
                {
                    return JsonSerializer.Serialize(new { 
                        Success = false, 
                        Error = "No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set", 
                        Message = "Sender address is required" 
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
            }

            // Validate sender address
            var validationError = ValidateSenderAddress(senderAddress);
            if (validationError != null)
            {
                return JsonSerializer.Serialize(new { 
                    Success = false, 
                    Error = validationError, 
                    Message = "Invalid sender address format" 
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var op = await emailClient.SendAsync(WaitUntil.Completed, senderAddress, recipientAddress, subject, plainTextBody);
            return JsonSerializer.Serialize(new
            {
                Success = op.HasCompleted,
                OperationId = op.Id,
                Status = op.HasCompleted ? op.Value.Status.ToString() : "InProgress",
                Message = op.HasCompleted ? "Email sent successfully" : "Email send operation is still in progress"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (RequestFailedException ex)
        {
            return CreateErrorResponse(ex, "simple email");
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to send email due to unexpected error" }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool, Description("Send an incident email using predefined incident template.")]
    public static async Task<string> SendIncidentEmail(
        EmailClient emailClient,
        [Description("The recipient email address.")] string recipientAddress,
        [Description("JSON object containing incident variables.")] string incidentVariables,
        [Description("Optional: The sender email address (must be from a verified domain). Leave empty to use default sender from environment.") ] string senderAddress = "",
        [Description("Optional: Display name of sender.")] string senderDisplayName = "",
        [Description("Optional: Display name of recipient.")] string recipientDisplayName = "",
        [Description("Optional: Whether to send HTML version (default: true).") ] bool useHtml = true,
        [Description("Optional: Path to file for attachment.")] string attachmentFilePath = "",
        [Description("Optional: Base64 encoded attachment content.")] string attachmentContent = "",
        [Description("Optional: Filename for the attachment.")] string attachmentFileName = "",
        [Description("Optional: MIME type for the attachment.")] string attachmentMimeType = "")
    {
        try
        {
            // Use default sender address from environment if none provided
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                senderAddress = Environment.GetEnvironmentVariable("DEFAULT_SENDER_ADDRESS") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(senderAddress))
                {
                    return JsonSerializer.Serialize(new { 
                        Success = false, 
                        Error = "No sender address provided and DEFAULT_SENDER_ADDRESS environment variable not set", 
                        Message = "Sender address is required" 
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
            }

            // Validate sender address
            var validationError = ValidateSenderAddress(senderAddress);
            if (validationError != null)
            {
                return JsonSerializer.Serialize(new { 
                    Success = false, 
                    Error = validationError, 
                    Message = "Invalid sender address format" 
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var template = EmailTemplates.GetTemplate("incident-outage");
            if (template == null)
            {
                return JsonSerializer.Serialize(new { Success = false, Error = "Template not found", Message = "Incident template missing" }, new JsonSerializerOptions { WriteIndented = true });
            }

            Dictionary<string, string> variables;
            try
            {
                variables = new();
                var jsonDoc = JsonDocument.Parse(incidentVariables);
                
                // Map common input variable names to template variable names
                var variableMapping = GetIncidentVariableMapping();
                
                foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                {
                    var value = GetStringFromJsonElement(prop.Value);
                    // Check if this property name needs to be mapped to a different template variable name
                    var mappedName = variableMapping.TryGetValue(prop.Name, out var mapped) ? mapped : prop.Name;
                    variables[mappedName] = value;
                }
                foreach (var kvp in template.DefaultValues)
                {
                    if (!variables.ContainsKey(kvp.Key)) variables[kvp.Key] = kvp.Value;
                }
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(new { Success = false, Error = $"Invalid JSON: {ex.Message}", Message = "Variable parsing failed" }, new JsonSerializerOptions { WriteIndented = true });
            }

            var subject = EmailTemplates.ProcessTemplate(template.Subject, variables);
            var processedBody = useHtml ? EmailTemplates.ProcessTemplate(template.HtmlBody, variables) : EmailTemplates.ProcessTemplate(template.PlainTextBody, variables);

            var content = new EmailContent(subject)
            {
                Html = useHtml ? processedBody : null,
                PlainText = useHtml ? null : processedBody
            };

            var message = new EmailMessage(
                senderAddress,
                new EmailRecipients(new List<EmailAddress> { new(recipientAddress, recipientDisplayName) }),
                content);

            if (!string.IsNullOrEmpty(attachmentFilePath) || (!string.IsNullOrEmpty(attachmentContent) && !string.IsNullOrEmpty(attachmentFileName)))
            {
                var (attachment, errorJson) = await ProcessAttachmentAsync(attachmentFilePath, attachmentContent, attachmentFileName, attachmentMimeType);
                if (errorJson != null) return errorJson;
                if (attachment != null) message.Attachments.Add(attachment);
            }

            var op = await emailClient.SendAsync(WaitUntil.Completed, message);
            return JsonSerializer.Serialize(new
            {
                Success = op.HasCompleted,
                OperationId = op.Id,
                Status = op.HasCompleted ? op.Value.Status.ToString() : "InProgress",
                Message = op.HasCompleted ? "Incident email sent successfully" : "Incident email send operation is still in progress",
                TemplateName = "incident-outage",
                ProcessedSubject = subject
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (RequestFailedException ex)
        {
            return CreateErrorResponse(ex, "incident email");
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to send incident email due to unexpected error" }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool, Description("List all available email templates and their required variables.")]
    public static string ListEmailTemplates()
    {
        try
        {
            var templateDetails = EmailTemplates.GetAvailableTemplates()
                .Select(name => EmailTemplates.GetTemplate(name))
                .Where(t => t != null)
                .Select(t =>
                {
                    var vars = new HashSet<string>();
                    ExtractVariables(t!.Subject, vars);
                    ExtractVariables(t.HtmlBody, vars);
                    ExtractVariables(t.PlainTextBody, vars);
                    return new
                    {
                        Subject = t.Subject,
                        RequiredVariables = vars.ToArray(),
                        DefaultValues = t.DefaultValues
                    };
                }).ToList();

            return JsonSerializer.Serialize(new { Success = true, Templates = templateDetails, Message = "Available email templates retrieved successfully" }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Success = false, Error = ex.Message, Message = "Failed to retrieve email templates" }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private static void ExtractVariables(string text, HashSet<string> variables)
    {
        if (string.IsNullOrEmpty(text)) return;
        var pattern = "\\{\\{(\\w+)\\}\\}";
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, pattern))
        {
            variables.Add(match.Groups[1].Value);
        }
    }

    // Static readonly mapping dictionary to avoid repeated allocations - case insensitive for better usability
    private static readonly Dictionary<string, string> IncidentVariableMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Map common input variable names to template variable names
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

    private static Dictionary<string, string> GetIncidentVariableMapping()
    {
        return IncidentVariableMapping;
    }

    private static string GetStringFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetDecimal().ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText() ?? string.Empty
        };
    }

    private static string? ValidateSenderAddress(string senderAddress)
    {
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            return "Sender address cannot be empty";
        }

        // Basic email format validation
        if (!senderAddress.Contains('@') || !senderAddress.Contains('.'))
        {
            return "Sender address must be a valid email format";
        }

        // Check for proper email structure: local@domain
        var parts = senderAddress.Split('@');
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
        {
            return "Sender address must be a valid email format";
        }

        var domain = parts[1];

        // Check for placeholder domains that are commonly used in examples but not valid for production
        if (domain.Equals("example.com", StringComparison.OrdinalIgnoreCase) ||
            domain.Equals("example.org", StringComparison.OrdinalIgnoreCase) ||
            domain.Equals("example.net", StringComparison.OrdinalIgnoreCase))
        {
            return "The sender domain appears to be a placeholder domain (example.com/org/net). " +
                   "Please use an Azure managed domain (recommended): donotreply@{domain-guid}.azurecomm.net " +
                   "or a custom verified domain linked to your Azure Communication Service resource.";
        }

        // Check if it's an Azure managed domain format
        if (senderAddress.Contains(".azurecomm.net"))
        {
            // Azure managed domain validation - should have GUID format
            if (!parts[0].Equals("donotreply", StringComparison.OrdinalIgnoreCase))
            {
                return "For Azure managed domains, the sender should typically be 'donotreply@{domain-guid}.azurecomm.net'";
            }
        }

        return null; // Valid
    }

    private static string CreateErrorResponse(RequestFailedException ex, string context = "email")
    {
        // Provide specific guidance for common errors
        string helpfulMessage = ex.ErrorCode switch
        {
            "DomainNotLinked" => "The sender domain has not been linked to this Azure Communication Service resource. " +
                               "For easiest setup, use Azure managed domains (recommended): donotreply@{domain-guid}.azurecomm.net. " +
                               "For custom domains, ensure the domain is: 1) Added to an Email Communication Service resource, " +
                               "2) Fully verified with proper DNS records, and 3) Linked to this Communication Service resource.",
            "Unauthorized" => "Authentication failed. Check that the Azure Communication Service resource has proper managed identity permissions.",
            "InvalidSender" => "The sender address is invalid or not from a verified domain. Use a verified domain address.",
            _ => $"Failed to send {context} due to Azure Communication Services error"
        };
        
        return JsonSerializer.Serialize(new { 
            Success = false, 
            Error = ex.Message, 
            ErrorCode = ex.ErrorCode, 
            Message = helpfulMessage,
            TroubleshootingUrl = "https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-domain-configuration-troubleshooting"
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GetMimeType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".zip" => "application/zip",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".tiff" or ".tif" => "image/tiff",
        _ => "application/octet-stream"
    };

    private static string GetMimeTypeFromFileName(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".zip" => "application/zip",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".tiff" or ".tif" => "image/tiff",
        ".html" or ".htm" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".mp3" => "audio/mpeg",
        ".mp4" => "video/mp4",
        ".avi" => "video/x-msvideo",
        ".wav" => "audio/wav",
        _ => "application/octet-stream"
    };
}