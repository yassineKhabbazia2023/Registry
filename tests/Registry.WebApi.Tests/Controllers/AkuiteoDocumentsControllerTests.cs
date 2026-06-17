using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Registry.WebApi.Controllers;

namespace Registry.WebApi.Tests.Controllers;

/// <summary>
/// Tests for <see cref="AkuiteoDocumentsController"/>.
/// </summary>
public class AkuiteoDocumentsControllerTests
{
    #region UploadDocumentAsync

    /// <summary>
    /// Ensures the controller returns 201 with the document upload outcome.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_ShouldReturnCreated()
    {
        // Arrange
        AkuiteoDocumentUploadRequest? capturedRequest = null;
        var file = CreateFormFile("sample.pdf", "application/pdf", "document-content");
        var response = new AkuiteoDocumentUploadResponse
        {
            AccountNumber = "9010001695",
            DocumentName = "sample.pdf",
            IsUploaded = true
        };

        var documentServiceMock = new Mock<IAkuiteoDocumentService>();
        documentServiceMock
            .Setup(service => service.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()))
            .Callback<AkuiteoDocumentUploadRequest>(request => capturedRequest = request)
            .ReturnsAsync(response);

        var controller = new AkuiteoDocumentsController(documentServiceMock.Object);

        // Act
        var result = await controller.UploadDocumentAsync("9010001695", file);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<AkuiteoDocumentUploadResponse>(objectResult.Value);
        Assert.Equal("9010001695", payload.AccountNumber);
        Assert.Equal("sample.pdf", payload.DocumentName);
        Assert.True(payload.IsUploaded);
        Assert.NotNull(capturedRequest);
        Assert.Equal("9010001695", capturedRequest!.AccountNumber);
        Assert.Equal("sample.pdf", capturedRequest.DocumentName);
        Assert.Equal("application/pdf", capturedRequest.ContentType);
        Assert.Equal(file.Length, capturedRequest.Length);
    }

    /// <summary>
    /// Ensures the controller translates technical Akuiteo document failures to 409.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenServiceThrowsConflict_ShouldReturnConflict()
    {
        // Arrange
        var file = CreateFormFile("sample.pdf", "application/pdf", "document-content");
        var documentServiceMock = new Mock<IAkuiteoDocumentService>();
        documentServiceMock
            .Setup(service => service.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()))
            .ThrowsAsync(new AkuiteoDocumentUploadTechnicalException("technical failure"));

        var controller = new AkuiteoDocumentsController(documentServiceMock.Object);

        // Act
        var result = await controller.UploadDocumentAsync("9010001695", file);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("technical failure", problemDetails.Title);
    }

    /// <summary>
    /// Ensures the controller translates invalid document payload failures to 400.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenServiceThrowsBadRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var file = CreateFormFile("sample.pdf", "application/pdf", "document-content");
        var documentServiceMock = new Mock<IAkuiteoDocumentService>();
        documentServiceMock
            .Setup(service => service.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()))
            .ThrowsAsync(new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "invalid document"));

        var controller = new AkuiteoDocumentsController(documentServiceMock.Object);

        // Act
        var result = await controller.UploadDocumentAsync("9010001695", file);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    /// <summary>
    /// Creates a form file for controller tests.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="content">The file content.</param>
    /// <returns>The form file.</returns>
    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "document", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
