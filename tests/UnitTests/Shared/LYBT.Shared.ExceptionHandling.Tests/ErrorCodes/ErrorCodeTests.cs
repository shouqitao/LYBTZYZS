using FluentAssertions;
using LYBT.Shared.Primitives.ErrorCodes;
using Xunit;

namespace LYBT.Shared.ExceptionHandling.Tests.ErrorCodes;

/// <summary>
/// ErrorCode枚举单元测试
/// consolidate-exception-handling: Phase 9
/// </summary>
public class ErrorCodeTests
{
    #region 模块分区测试

    [Theory]
    [InlineData(ErrorCode.Unknown, "General")]
    [InlineData(ErrorCode.InternalError, "General")]
    [InlineData(ErrorCode.ValidationFailed, "General")]
    public void GetModuleName_GeneralErrors_ReturnsGeneral(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    [Theory]
    [InlineData(ErrorCode.UserNotFound, "Users")]
    [InlineData(ErrorCode.UserNameExists, "Users")]
    [InlineData(ErrorCode.InvalidPassword, "Users")]
    public void GetModuleName_UserErrors_ReturnsUsers(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    [Theory]
    [InlineData(ErrorCode.PatientNotFound, "Patients")]
    [InlineData(ErrorCode.PatientIdCardExists, "Patients")]
    public void GetModuleName_PatientErrors_ReturnsPatients(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    [Theory]
    [InlineData(ErrorCode.MedicalCaseNotFound, "MedicalCase")]
    [InlineData(ErrorCode.MedicalCaseLocked, "MedicalCase")]
    public void GetModuleName_MedicalCaseErrors_ReturnsMedicalCase(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    [Theory]
    [InlineData(ErrorCode.PrescriptionNotFound, "Prescriptions")]
    [InlineData(ErrorCode.HerbNotFound, "Herbs")]
    [InlineData(ErrorCode.FormulaNotFound, "Formula")]
    [InlineData(ErrorCode.ConsultationNotFound, "Consultation")]
    public void GetModuleName_OtherModules_ReturnsCorrectModule(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    #endregion

    #region HTTP状态码映射测试

    [Theory]
    [InlineData(ErrorCode.ValidationFailed, 400)]
    [InlineData(ErrorCode.InvalidRequest, 400)]
    [InlineData(ErrorCode.ConsultationNoSymptoms, 400)]
    public void ToHttpStatusCode_ValidationErrors_Returns400(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(ErrorCode.Unauthorized, 401)]
    [InlineData(ErrorCode.InvalidPassword, 401)]
    [InlineData(ErrorCode.InvalidRefreshToken, 401)]
    public void ToHttpStatusCode_AuthenticationErrors_Returns401(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(ErrorCode.Forbidden, 403)]
    [InlineData(ErrorCode.UserDisabled, 403)]
    [InlineData(ErrorCode.CannotDeleteSysAdmin, 403)]
    public void ToHttpStatusCode_AuthorizationErrors_Returns403(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(ErrorCode.NotFound, 404)]
    [InlineData(ErrorCode.UserNotFound, 404)]
    [InlineData(ErrorCode.PatientNotFound, 404)]
    [InlineData(ErrorCode.HerbNotFound, 404)]
    public void ToHttpStatusCode_NotFoundErrors_Returns404(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(ErrorCode.ConcurrencyConflict, 409)]
    [InlineData(ErrorCode.UserNameExists, 409)]
    [InlineData(ErrorCode.MedicalCaseVersionConflict, 409)]
    public void ToHttpStatusCode_ConflictErrors_Returns409(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(ErrorCode.InvalidMedicalCaseState, 422)]
    [InlineData(ErrorCode.PrescriptionCompleted, 422)]
    [InlineData(ErrorCode.PasswordChangeRequired, 422)]
    public void ToHttpStatusCode_BusinessRuleErrors_Returns422(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Fact]
    public void ToHttpStatusCode_UnknownError_Returns500()
    {
        // Act
        var result = ErrorCode.Unknown.ToHttpStatusCode();

        // Assert
        result.Should().Be(500);
    }

    #endregion

    #region 错误类别测试

    [Theory]
    [InlineData(ErrorCode.ValidationFailed, ErrorCategory.Validation)]
    [InlineData(ErrorCode.InvalidRequest, ErrorCategory.Validation)]
    public void ToCategory_ValidationErrors_ReturnsValidation(ErrorCode errorCode, ErrorCategory expectedCategory)
    {
        // Act
        var result = errorCode.ToCategory();

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(ErrorCode.Unauthorized, ErrorCategory.Authentication)]
    [InlineData(ErrorCode.InvalidPassword, ErrorCategory.Authentication)]
    public void ToCategory_AuthErrors_ReturnsAuthentication(ErrorCode errorCode, ErrorCategory expectedCategory)
    {
        // Act
        var result = errorCode.ToCategory();

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(ErrorCode.NotFound, ErrorCategory.Resource)]
    [InlineData(ErrorCode.UserNotFound, ErrorCategory.Resource)]
    public void ToCategory_NotFoundErrors_ReturnsResource(ErrorCode errorCode, ErrorCategory expectedCategory)
    {
        // Act
        var result = errorCode.ToCategory();

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(ErrorCode.InternalError, ErrorCategory.System)]
    [InlineData(ErrorCode.DatabaseError, ErrorCategory.System)]
    [InlineData(ErrorCode.ServiceUnavailable, ErrorCategory.System)]
    public void ToCategory_SystemErrors_ReturnsSystem(ErrorCode errorCode, ErrorCategory expectedCategory)
    {
        // Act
        var result = errorCode.ToCategory();

        // Assert
        result.Should().Be(expectedCategory);
    }

    #endregion

    #region 格式化测试

    [Theory]
    [InlineData(ErrorCode.Unknown, "ERR-00000")]
    [InlineData(ErrorCode.UserNotFound, "ERR-10001")]
    [InlineData(ErrorCode.PatientNotFound, "ERR-20001")]
    [InlineData(ErrorCode.MedicalCaseNotFound, "ERR-30001")]
    public void ToFormattedString_ReturnsCorrectFormat(ErrorCode errorCode, string expectedFormat)
    {
        // Act
        var result = errorCode.ToFormattedString();

        // Assert
        result.Should().Be(expectedFormat);
    }

    #endregion
}
