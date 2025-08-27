using FluentAssertions;
using Xunit;
using System.Text.Json;
using AcsEmailMcp.Tools;
using Azure.Communication.Email;
using Azure;
using Moq;

namespace AcsEmailMcp.Tests;

public class EmailAttachmentTests
{
    [Fact]
    public async Task SendEmailWithMultipleAttachments_ShouldValidateAttachmentSize()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();

        // Create large attachment (simulate over size limit)
        var largeContent = new byte[30 * 1024 * 1024]; // 30MB
        Array.Fill(largeContent, (byte)65); // Fill with 'A'
        var largeAttachmentContent = Convert.ToBase64String(largeContent);

        var attachments = new[]
        {
            new EmailAttachmentInfo { Content = largeAttachmentContent, FileName = "large.txt", MimeType = "text/plain" }
        };

        // Act
        var result = await EmailTools.SendEmailWithMultipleAttachments(
            mockEmailClient.Object,
            "test@example.com",
            "Test Subject", 
            "Test Body",
            "donotreply@abc123-def4-5678-9012-abcdef123456.azurecomm.net",
            attachments: attachments);

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Contain("maximum allowed size");
    }

    [Fact]
    public async Task SendEmailWithMultipleAttachments_ShouldHandleInvalidBase64Content()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();

        var attachments = new[]
        {
            new EmailAttachmentInfo { Content = "invalid-base64-content!", FileName = "test.txt", MimeType = "text/plain" }
        };

        // Act - send with invalid base64 content
        var result = await EmailTools.SendEmailWithMultipleAttachments(
            mockEmailClient.Object,
            "test@example.com",
            "Test Subject",
            "Test Body",
            "donotreply@abc123-def4-5678-9012-abcdef123456.azurecomm.net",
            attachments: attachments);

        // Assert
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        response.GetProperty("Error").GetString().Should().Contain("base64");
    }

    [Fact]
    public async Task ProcessAttachmentAsync_ShouldDetectMimeTypeFromFileName()
    {
        // Test MIME type detection using reflection since the method is private
        var emailToolsType = typeof(EmailTools);
        var getMimeTypeMethod = emailToolsType.GetMethod("GetMimeTypeFromFileName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Test various file extensions
        getMimeTypeMethod!.Invoke(null, new object[] { "document.pdf" }).Should().Be("application/pdf");
        getMimeTypeMethod.Invoke(null, new object[] { "image.jpg" }).Should().Be("image/jpeg");
        getMimeTypeMethod.Invoke(null, new object[] { "text.txt" }).Should().Be("text/plain");
        getMimeTypeMethod.Invoke(null, new object[] { "unknown.xyz" }).Should().Be("application/octet-stream");
    }

    [Fact]
    public void EmailAttachmentInfo_ShouldHaveCorrectProperties()
    {
        // Test the EmailAttachmentInfo class structure
        var attachmentInfo = new EmailAttachmentInfo
        {
            Content = "dGVzdA==", // base64 for "test"
            FileName = "test.txt",
            MimeType = "text/plain",
            FilePath = "/path/to/file.txt"
        };

        attachmentInfo.Content.Should().Be("dGVzdA==");
        attachmentInfo.FileName.Should().Be("test.txt");
        attachmentInfo.MimeType.Should().Be("text/plain");
        attachmentInfo.FilePath.Should().Be("/path/to/file.txt");
    }

    [Fact]
    public async Task SendEmailWithMultipleAttachments_ShouldHandleEmptyAttachments()
    {
        // Arrange
        var mockEmailClient = new Mock<EmailClient>();

        // Act - send with null attachments should fail because EmailClient is not properly mocked
        // but this tests that the method handles null attachments before trying to send
        var result = await EmailTools.SendEmailWithMultipleAttachments(
            mockEmailClient.Object,
            "test@example.com",
            "Test Subject",
            "Test Body",
            "donotreply@abc123-def4-5678-9012-abcdef123456.azurecomm.net",
            attachments: null);

        // Assert that it gets to the EmailClient.SendAsync call (which will fail)
        // but doesn't fail in attachment processing
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.GetProperty("Success").GetBoolean().Should().BeFalse();
        // It should fail on the EmailClient call, not on attachment processing
        response.GetProperty("Error").GetString().Should().NotContain("attachment");
    }
}