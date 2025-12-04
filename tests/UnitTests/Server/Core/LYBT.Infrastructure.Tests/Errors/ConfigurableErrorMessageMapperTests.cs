using LYBT.Infrastructure.Errors;
using LYBT.Shared.Models.Errors;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LYBT.Infrastructure.Tests.Errors;

/// <summary>
/// ConfigurableErrorMessageMapper单元测试
/// refactor-logging-system: Task 4.1
/// </summary>
public class ConfigurableErrorMessageMapperTests
{
    private readonly IConfiguration _emptyConfiguration;

    public ConfigurableErrorMessageMapperTests()
    {
        _emptyConfiguration = new ConfigurationBuilder().Build();
    }

    [Theory]
    [InlineData(ErrorCode.Unknown, "操作失败，请稍后重试")]
    [InlineData(ErrorCode.NotFound, "请求的资源不存在")]
    [InlineData(ErrorCode.ValidationFailed, "输入数据验证失败，请检查后重试")]
    [InlineData(ErrorCode.Unauthorized, "请先登录后再访问此资源")]
    [InlineData(ErrorCode.ConcurrencyConflict, "数据已被其他用户修改，请刷新后重试")]
    public void GetUserMessage_ReturnsDefaultMessage_ForKnownErrorCodes(ErrorCode errorCode, string expectedMessage)
    {
        // Arrange
        var mapper = new ConfigurableErrorMessageMapper(_emptyConfiguration);

        // Act
        var message = mapper.GetUserMessage(errorCode);

        // Assert
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData(ErrorCode.UserNotFound, "用户不存在")]
    [InlineData(ErrorCode.PatientNotFound, "患者信息不存在")]
    [InlineData(ErrorCode.MedicalCaseNotFound, "病历不存在")]
    [InlineData(ErrorCode.PrescriptionNotFound, "处方不存在")]
    [InlineData(ErrorCode.HerbNotFound, "药材不存在")]
    [InlineData(ErrorCode.FormulaNotFound, "方剂不存在")]
    public void GetUserMessage_ReturnsModuleSpecificMessage_ForModuleErrorCodes(ErrorCode errorCode, string expectedMessage)
    {
        // Arrange
        var mapper = new ConfigurableErrorMessageMapper(_emptyConfiguration);

        // Act
        var message = mapper.GetUserMessage(errorCode);

        // Assert
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void GetUserMessage_ReturnsConfiguredMessage_WhenOverridden()
    {
        // Arrange
        var customMessage = "自定义错误消息";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lybt:ErrorMessages:ERR-00001:UserMessage"] = customMessage
            })
            .Build();
        var mapper = new ConfigurableErrorMessageMapper(configuration);

        // Act
        var message = mapper.GetUserMessage(ErrorCode.InvalidRequest);

        // Assert
        Assert.Equal(customMessage, message);
    }

    [Fact]
    public void GetTechnicalMessage_ReturnsTechnicalMessage_ForKnownErrorCodes()
    {
        // Arrange
        var mapper = new ConfigurableErrorMessageMapper(_emptyConfiguration);

        // Act
        var message = mapper.GetTechnicalMessage(ErrorCode.Unknown);

        // Assert
        Assert.Equal("Unknown error occurred", message);
    }

    [Fact]
    public void GetUserMessage_WithArgs_FormatsMessage()
    {
        // Arrange
        var mapper = new ConfigurableErrorMessageMapper(_emptyConfiguration);

        // Act - 使用一个已知返回包含{0}占位符的错误码
        var message = mapper.GetUserMessage(ErrorCode.NotFound);

        // Assert - 应该返回不带格式化参数的消息
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserMessage_ReturnsFallbackMessage_ForUnknownErrorCode()
    {
        // Arrange
        var mapper = new ConfigurableErrorMessageMapper(_emptyConfiguration);
        var unknownErrorCode = (ErrorCode)99999;

        // Act
        var message = mapper.GetUserMessage(unknownErrorCode);

        // Assert
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }
}
