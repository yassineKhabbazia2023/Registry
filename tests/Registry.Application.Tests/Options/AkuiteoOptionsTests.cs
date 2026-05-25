using Application.Options;

namespace Registry.Application.Tests.ApplicationOptions;

/// <summary>
/// Tests for <see cref="AkuiteoOptions"/>.
/// </summary>
public class AkuiteoOptionsTests
{
    #region BuildApiBaseUrl

    /// <summary>
    /// Ensures the Akuiteo API base URL keeps the configured path and adds a trailing slash.
    /// </summary>
    [Fact]
    public void BuildApiBaseUrl_ShouldReturnBaseUrlWithTrailingSlash()
    {
        // Arrange
        var options = new AkuiteoOptions
        {
            BaseUrl = "https://apim.example.com/its-val-exp-platform-rydge-v1/api/v1"
        };

        // Act
        var result = options.BuildApiBaseUrl();

        // Assert
        Assert.Equal(new Uri("https://apim.example.com/its-val-exp-platform-rydge-v1/api/v1/"), result);
    }

    /// <summary>
    /// Ensures missing API base URL setting is rejected.
    /// </summary>
    [Fact]
    public void BuildApiBaseUrl_WhenRequiredSettingIsMissing_ShouldThrow()
    {
        // Arrange
        var options = new AkuiteoOptions();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => options.BuildApiBaseUrl());
    }

    #endregion

    #region BuildTokenBaseUrl

    /// <summary>
    /// Ensures the token base URL is built from the configured base URL and tenant.
    /// </summary>
    [Fact]
    public void BuildTokenBaseUrl_ShouldReturnTenantTokenBaseUrl()
    {
        // Arrange
        var options = new AkuiteoOptions
        {
            TokenBaseUrl = "https://login.microsoftonline.com/",
            Tenant = "tenant-id"
        };

        // Act
        var result = options.BuildTokenBaseUrl();

        // Assert
        Assert.Equal(new Uri("https://login.microsoftonline.com/tenant-id/oauth2/v2.0/"), result);
    }

    /// <summary>
    /// Ensures missing token URL settings are rejected.
    /// </summary>
    [Theory]
    [InlineData(null, "tenant-id")]
    [InlineData("https://login.microsoftonline.com", null)]
    public void BuildTokenBaseUrl_WhenRequiredSettingIsMissing_ShouldThrow(string? tokenBaseUrl, string? tenant)
    {
        // Arrange
        var options = new AkuiteoOptions
        {
            TokenBaseUrl = tokenBaseUrl,
            Tenant = tenant
        };

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => options.BuildTokenBaseUrl());
    }

    #endregion
}
