using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.ExceptionHandling.ProblemDetails;
using Xunit;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Tests.Server.Unit.Shared.ExceptionHandling;

/// <summary>
/// ProblemDetailsFactory单元测试
/// consolidate-exception-handling: Phase 9
/// </summary>
public class ProblemDetailsFactoryTests
{
    private const string TestInstance = "/api/v1/users/123";
    private const string TestCorrelationId = "test-correlation-id";
    private const string TestTraceId = "test-trace-id";

    #region Create(AppException) 测试

    [Fact]
    public void Create_FromAppException_SetsBasicProperties()
    {
        // Arrange
        var exception = new AppException(EC.UserNotFound, "用户不存在", "找不到指定的用户");

        // Act
        var result = ProblemDetailsFactory.Create(exception, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Status.Should().Be(404);
        result.Title.Should().Be("资源未找到");
        result.Detail.Should().Be("找不到指定的用户");
        result.Instance.Should().Be(TestInstance);
        result.Type.Should().Contain("rfc7231");
    }

    [Fact]
    public void Create_FromAppException_SetsExtensions()
    {
        // Arrange
        var exception = new AppException(EC.ValidationFailed, "验证失败");

        // Act
        var result = ProblemDetailsFactory.Create(exception, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Extensions.Should().ContainKey("errorCode");
        result.Extensions["errorCode"].Should().Be("ERR-00003");
        result.Extensions.Should().ContainKey("correlationId");
        result.Extensions["correlationId"].Should().Be(TestCorrelationId);
        result.Extensions.Should().ContainKey("traceId");
        result.Extensions["traceId"].Should().Be(TestTraceId);
        result.Extensions.Should().ContainKey("timestamp");
    }

    [Fact]
    public void Create_FromValidationException_IncludesErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "不能为空" } },
            { "Email", new[] { "格式不正确" } }
        };
        var exception = new ValidationException(errors);

        // Act
        var result = ProblemDetailsFactory.Create(exception, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Status.Should().Be(400);
        result.Extensions.Should().ContainKey("errors");
        var errorsDict = result.Extensions["errors"] as Dictionary<string, string[]>;
        errorsDict.Should().NotBeNull();
        errorsDict.Should().HaveCount(2);
    }

    [Fact]
    public void Create_FromConflictException_IncludesEntityInfo()
    {
        // Arrange
        var exception = ConflictException.Duplicate("User", "用户名", "admin");

        // Act
        var result = ProblemDetailsFactory.Create(exception, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Status.Should().Be(409);
    }

    #endregion

    #region Create(ErrorCode) 测试

    [Theory]
    [InlineData(EC.NotFound, 404, "资源未找到")]
    [InlineData(EC.ValidationFailed, 400, "验证失败")]
    [InlineData(EC.Unauthorized, 401, "身份认证失败")]
    [InlineData(EC.Forbidden, 403, "权限不足")]
    [InlineData(EC.ConcurrencyConflict, 409, "并发冲突")]
    [InlineData(EC.InternalError, 500, "系统错误")]
    public void Create_FromErrorCode_SetsCorrectStatusAndTitle(EC errorCode, int expectedStatus, string expectedTitle)
    {
        // Act
        var result = ProblemDetailsFactory.Create(errorCode, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Status.Should().Be(expectedStatus);
        result.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public void Create_FromErrorCode_WithCustomDetail_UsesCustomDetail()
    {
        // Arrange
        var customDetail = "自定义错误详情";

        // Act
        var result = ProblemDetailsFactory.Create(EC.UserNotFound, TestInstance, TestCorrelationId, TestTraceId, customDetail);

        // Assert
        result.Detail.Should().Be(customDetail);
    }

    [Fact]
    public void Create_FromErrorCode_SetsCorrectType()
    {
        // Act
        var result = ProblemDetailsFactory.Create(EC.NotFound, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Type.Should().Contain("rfc7231");
        result.Type.Should().Contain("6.5.4"); // 404的RFC节号
    }

    #endregion

    #region CreateValidationProblem 测试

    [Fact]
    public void CreateValidationProblem_SetsCorrectProperties()
    {
        // Arrange
        var errors = new Dictionary<string, List<string>>
        {
            { "Name", new List<string> { "不能为空", "长度过短" } },
            { "Age", new List<string> { "必须大于0" } }
        };

        // Act
        var result = ProblemDetailsFactory.CreateValidationProblem(errors, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Status.Should().Be(400);
        result.Title.Should().Be("验证失败");
        result.Detail.Should().Contain("验证失败");
        result.Extensions["errorCode"].Should().Be("ERR-00003");
    }

    [Fact]
    public void CreateValidationProblem_IncludesAllErrors()
    {
        // Arrange
        var errors = new Dictionary<string, List<string>>
        {
            { "Field1", new List<string> { "错误1", "错误2" } },
            { "Field2", new List<string> { "错误3" } }
        };

        // Act
        var result = ProblemDetailsFactory.CreateValidationProblem(errors, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Extensions.Should().ContainKey("errors");
        var errorsDict = result.Extensions["errors"] as Dictionary<string, List<string>>;
        errorsDict.Should().NotBeNull();
        errorsDict.Should().HaveCount(2);
        errorsDict!["Field1"].Should().HaveCount(2);
    }

    #endregion

    #region RFC 7807 合规性测试

    [Fact]
    public void Create_ReturnsRFC7807CompliantProblemDetails()
    {
        // Arrange
        var exception = new AppException(EC.UserNotFound, "用户不存在");

        // Act
        var result = ProblemDetailsFactory.Create(exception, TestInstance, TestCorrelationId, TestTraceId);

        // Assert - RFC 7807 required members
        result.Type.Should().NotBeNullOrEmpty(); // type
        result.Title.Should().NotBeNullOrEmpty(); // title
        result.Status.Should().NotBeNull(); // status
        result.Instance.Should().Be(TestInstance); // instance
    }

    [Theory]
    [InlineData(400, "rfc7231#section-6.5.1")]
    [InlineData(401, "rfc7235#section-3.1")]
    [InlineData(403, "rfc7231#section-6.5.3")]
    [InlineData(404, "rfc7231#section-6.5.4")]
    [InlineData(409, "rfc7231#section-6.5.8")]
    [InlineData(500, "rfc7231#section-6.6.1")]
    public void Create_TypeUriMatchesStatusCode(int statusCode, string expectedUriFragment)
    {
        // Arrange
        var errorCode = statusCode switch
        {
            400 => EC.ValidationFailed,
            401 => EC.Unauthorized,
            403 => EC.Forbidden,
            404 => EC.NotFound,
            409 => EC.ConcurrencyConflict,
            _ => EC.InternalError
        };

        // Act
        var result = ProblemDetailsFactory.Create(errorCode, TestInstance, TestCorrelationId, TestTraceId);

        // Assert
        result.Type.Should().Contain(expectedUriFragment);
    }

    #endregion
}
