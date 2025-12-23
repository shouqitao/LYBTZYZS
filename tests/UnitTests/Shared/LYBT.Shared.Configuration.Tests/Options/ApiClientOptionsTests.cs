using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LYBT.Shared.Configuration.Options.Client;
using Xunit;

namespace LYBT.Shared.Configuration.Tests.Options;

/// <summary>
/// ApiClientOptions 单元测试
/// </summary>
public class ApiClientOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeApiClient()
    {
        // Assert
        ApiClientOptions.SectionName.Should().Be("ApiClient");
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var options = new ApiClientOptions();

        // Assert
        options.BaseUrl.Should().Be("https://localhost:5001/");
        options.TimeoutSeconds.Should().Be(60);
        options.IgnoreSslErrors.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validation_BaseUrl_Required(string? baseUrl)
    {
        // Arrange
        var options = new ApiClientOptions { BaseUrl = baseUrl! };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ApiClientOptions.BaseUrl)));
    }

    [Fact]
    public void Validation_BaseUrl_MustBeValidUrl()
    {
        // Arrange
        var options = new ApiClientOptions { BaseUrl = "not-a-url" };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ApiClientOptions.BaseUrl)));
    }

    [Theory]
    [InlineData("https://localhost:5001/")]
    [InlineData("http://api.example.com/")]
    [InlineData("https://192.168.1.1:8080/")]
    public void Validation_BaseUrl_ValidUrls(string baseUrl)
    {
        // Arrange
        var options = new ApiClientOptions { BaseUrl = baseUrl };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(301)]
    public void Validation_TimeoutSeconds_OutOfRange(int seconds)
    {
        // Arrange
        var options = new ApiClientOptions
        {
            BaseUrl = "https://localhost:5001/",
            TimeoutSeconds = seconds
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ApiClientOptions.TimeoutSeconds)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(300)]
    public void Validation_TimeoutSeconds_ValidRange(int seconds)
    {
        // Arrange
        var options = new ApiClientOptions
        {
            BaseUrl = "https://localhost:5001/",
            TimeoutSeconds = seconds
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
    }
}
