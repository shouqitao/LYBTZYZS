using FluentAssertions;
using LYBT.Shared.Primitives.ErrorCodes;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// ErrorCode枚举单元测试
/// consolidate-exception-handling: Phase 9
/// Sprint3-Batch1: X1 MCCEE 统一 - 全模块 MCCEE 码注册 + Auth 迁移
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
    [InlineData(ErrorCode.UserNotFound, "Users/Auth")]
    [InlineData(ErrorCode.UserNameExists, "Users/Auth")]
    [InlineData(ErrorCode.InvalidPassword, "Users/Auth")]
    public void GetModuleName_UserErrors_ReturnsUsersAuth(ErrorCode errorCode, string expectedModule)
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
    [InlineData(ErrorCode.HerbNotFound, "Herbs")]
    [InlineData(ErrorCode.FormulaNotFound, "Formula")]
    public void GetModuleName_OtherModules_ReturnsCorrectModule(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    [Theory]
    [InlineData(ErrorCode.HerbNotDeleted, "Herbs")]
    [InlineData(ErrorCode.HerbInvalidPagination, "Herbs")]
    [InlineData(ErrorCode.HerbBatchImportExceeded, "Herbs")]
    public void GetModuleName_HerbMcceeErrors_ReturnsHerbs(ErrorCode errorCode, string expectedModule)
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
    [InlineData(ErrorCode.HerbInvalidPagination, 400)]
    [InlineData(ErrorCode.HerbBatchImportExceeded, 400)]
    [InlineData(ErrorCode.UserNameExists, 400)]
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
    [InlineData(ErrorCode.AuthAccessTokenExpired, ErrorCategory.Authentication)]
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

    [Theory]
    [InlineData(ErrorCode.ConcurrencyConflict, ErrorCategory.Concurrency)]
    [InlineData(ErrorCode.MedicalCaseVersionConflict, ErrorCategory.Concurrency)]
    public void ToCategory_ConcurrencyErrors_ReturnsConcurrency(ErrorCode errorCode, ErrorCategory expectedCategory)
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
    [InlineData(ErrorCode.HerbNotDeleted, "ERR-50104")]
    [InlineData(ErrorCode.HerbBatchImportExceeded, "ERR-50202")]
    public void ToFormattedString_ReturnsCorrectFormat(ErrorCode errorCode, string expectedFormat)
    {
        // Act
        var result = errorCode.ToFormattedString();

        // Assert
        result.Should().Be(expectedFormat);
    }

    #endregion

    #region Herb MCCEE 错误码测试

    [Fact]
    public void HerbMcceeCodes_AllDefined_HaveMessages()
    {
        // Arrange - 全部 Herb MCCEE 码 (501xx~503xx)
        var herbCodes = new[]
        {
            ErrorCode.HerbValidationFailed,       // 50102
            ErrorCode.HerbNoPermission,            // 50103
            ErrorCode.HerbNotDeleted,              // 50104
            ErrorCode.HerbInvalidPagination,       // 50106
            ErrorCode.HerbBatchEmpty,              // 50201
            ErrorCode.HerbBatchImportExceeded,     // 50202
            ErrorCode.HerbBatchCheckExceeded,      // 50203
            ErrorCode.HerbBatchItemNotFound,       // 50204
            ErrorCode.HerbBatchItemDeletedOrMissing, // 50205
            ErrorCode.HerbBatchItemError,          // 50206
            ErrorCode.HerbImportFileEmpty,         // 50301
            ErrorCode.HerbImportFileFormat,        // 50302
            ErrorCode.HerbImportFileSize,          // 50303
            ErrorCode.HerbImportExcelError         // 50304
        };

        foreach (var code in herbCodes)
        {
            code.GetModuleName().Should().Be("Herbs", $"{code} 应属于 Herbs 模块");
            ErrorMessages.Get(code).Should().NotBe(code.ToString(), $"{code} 应有中文消息");
        }
    }

    #endregion

    #region Patient MCCEE 错误码测试

    [Fact]
    public void PatientMcceeCodes_AllDefined_HaveMessages()
    {
        var patientCodes = new[]
        {
            ErrorCode.PatientPhoneDuplicate,       // 20701
            ErrorCode.PatientNotDeleted,           // 20702
            ErrorCode.PatientBatchOperationEmpty,  // 20703
            ErrorCode.PatientBatchCheckExceeded,   // 20704
            ErrorCode.PatientInvalidPagination,    // 20705
            ErrorCode.PatientImportFileEmpty,      // 20801
            ErrorCode.PatientImportFileFormat,     // 20802
            ErrorCode.PatientImportFileSize,       // 20803
            ErrorCode.PatientImportNoWorksheet,    // 20804
            ErrorCode.PatientImportRowExceeded     // 20805
        };

        patientCodes.Should().HaveCount(10);

        foreach (var code in patientCodes)
        {
            code.GetModuleName().Should().Be("Patients", $"{code} 应属于 Patients 模块");
            ErrorMessages.Get(code).Should().NotBe(code.ToString(), $"{code} 应有中文消息");
        }
    }

    #endregion

    #region Formula MCCEE 错误码测试

    [Fact]
    public void FormulaMcceeCodes_AllDefined_HaveMessages()
    {
        var formulaCodes = new[]
        {
            ErrorCode.FormulaIdInvalid,                   // 60102
            ErrorCode.FormulaNoPermission,                // 60103
            ErrorCode.FormulaCreateFailed,                // 60104
            ErrorCode.FormulaUpdateFailed,                // 60105
            ErrorCode.FormulaDeleteFailed,                // 60106
            ErrorCode.FormulaNotDeleted,                  // 60107
            ErrorCode.FormulaInvalidPagination,           // 60108
            ErrorCode.FormulaHerbItemIdInvalid,           // 60201
            ErrorCode.FormulaHerbItemNotFound,            // 60202
            ErrorCode.FormulaHerbItemAlreadyValidated,    // 60203
            ErrorCode.FormulaSystemHerbNotFound,          // 60204
            ErrorCode.FormulaPendingValidationListFailed, // 60205
            ErrorCode.FormulaBatchEmpty,                  // 60301
            ErrorCode.FormulaBatchImportEmpty,            // 60302
            ErrorCode.FormulaBatchItemNotFound,           // 60303
            ErrorCode.FormulaBatchItemError               // 60304
        };

        formulaCodes.Should().HaveCount(16);

        foreach (var code in formulaCodes)
        {
            code.GetModuleName().Should().Be("Formula", $"{code} 应属于 Formula 模块");
            ErrorMessages.Get(code).Should().NotBe(code.ToString(), $"{code} 应有中文消息");
        }
    }

    #endregion

    #region MedicalCase MCCEE 错误码测试

    [Fact]
    public void MedicalCaseMcceeCodes_AllDefined_HaveMessages()
    {
        var mcCodes = new[]
        {
            // 301xx
            ErrorCode.McPatientNotFound,            // 30101
            ErrorCode.McDoctorNotFound,             // 30102
            ErrorCode.McActiveCaseExists,           // 30103
            ErrorCode.McSuspendedCaseExists,        // 30104
            ErrorCode.McPatientDisabled,            // 30105
            // 302xx
            ErrorCode.McCannotEditCase,             // 30201
            ErrorCode.McCannotDeleteCase,           // 30202
            ErrorCode.McCannotCancelCase,           // 30203
            ErrorCode.McCannotDeletePrescription,   // 30204
            ErrorCode.McCannotSuspendCase,          // 30205
            // 303xx
            ErrorCode.McInvalidStatusTransition,    // 30301
            ErrorCode.McPrescriptionFlagRequired,   // 30302
            ErrorCode.McPrescriptionRequired,       // 30303
            ErrorCode.McCompletedCannotSuspend,     // 30304
            ErrorCode.McDeletedCannotSuspend,       // 30305
            ErrorCode.McCompletedCannotCancel,      // 30306
            ErrorCode.McAlreadyDeleted,             // 30307
            // 304xx
            ErrorCode.McPrescriptionFlagNotSet,     // 30401
            ErrorCode.McPrescriptionAlreadyExists,  // 30402
            ErrorCode.McPrintedRequiresReason,      // 30403
            ErrorCode.McPrintedCannotDelete,        // 30404
            ErrorCode.McConsultationNotFound,       // 30405
            // 305xx
            ErrorCode.McPrescriptionCreateRetryFailed, // 30501
            ErrorCode.McSaveRetryFailed,            // 30502
            // 306xx
            ErrorCode.McRequestIdMismatch,          // 30601
            ErrorCode.McInvalidPagination,          // 30602
            ErrorCode.McBatchQueryExceeded,         // 30603
            ErrorCode.McBatchOperationEmpty,        // 30604
            ErrorCode.McInvalidPatientId,           // 30605
            ErrorCode.McInvalidCountParam,          // 30606
            ErrorCode.McCaseNotFound                // 30607
        };

        mcCodes.Should().HaveCount(31);

        foreach (var code in mcCodes)
        {
            code.GetModuleName().Should().Be("MedicalCase", $"{code} 应属于 MedicalCase 模块");
            ErrorMessages.Get(code).Should().NotBe(code.ToString(), $"{code} 应有中文消息");
        }
    }

    [Theory]
    [InlineData(ErrorCode.McCannotEditCase, 403)]
    [InlineData(ErrorCode.McCannotDeleteCase, 403)]
    [InlineData(ErrorCode.McCannotSuspendCase, 403)]
    public void MedicalCaseMcceeCodes_PermissionErrors_Return403(ErrorCode errorCode, int expectedStatus)
    {
        errorCode.ToHttpStatusCode().Should().Be(expectedStatus);
        errorCode.ToCategory().Should().Be(ErrorCategory.Authorization);
    }

    [Theory]
    [InlineData(ErrorCode.McActiveCaseExists, 422)]
    [InlineData(ErrorCode.McInvalidStatusTransition, 422)]
    [InlineData(ErrorCode.McPrescriptionRequired, 422)]
    public void MedicalCaseMcceeCodes_BusinessErrors_Return422(ErrorCode errorCode, int expectedStatus)
    {
        errorCode.ToHttpStatusCode().Should().Be(expectedStatus);
        errorCode.ToCategory().Should().Be(ErrorCategory.Business);
    }

    #endregion

    #region Auth MCCEE 错误码测试

    [Theory]
    [InlineData(ErrorCode.AuthInvalidCredentials, 401)]
    [InlineData(ErrorCode.AuthTokenInvalid, 401)]
    [InlineData(ErrorCode.AuthTokenRevoked, 401)]
    [InlineData(ErrorCode.AuthRefreshTokenExpired, 401)]
    [InlineData(ErrorCode.AuthRefreshTokenInvalid, 401)]
    [InlineData(ErrorCode.AuthConcurrentSessionLimit, 401)]
    public void AuthMcceeCodes_AllReturn401(ErrorCode errorCode, int expectedStatus)
    {
        errorCode.ToHttpStatusCode().Should().Be(expectedStatus);
        errorCode.ToCategory().Should().Be(ErrorCategory.Authentication);
        errorCode.GetModuleName().Should().Be("Users/Auth");
        ErrorMessages.Get(errorCode).Should().NotBe(errorCode.ToString());
    }

    #endregion

    #region 枚举值唯一性测试

    [Fact]
    public void ErrorCode_AllValues_AreUnique()
    {
        var values = Enum.GetValues<ErrorCode>()
            .Select(e => (int)e)
            .ToList();

        var duplicates = values
            .GroupBy(v => v)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty("ErrorCode 枚举值不应有重复: {0}",
            string.Join(", ", duplicates));
    }

    [Fact]
    public void ErrorCode_AllValues_HaveErrorMessages()
    {
        var allCodes = Enum.GetValues<ErrorCode>();

        foreach (var code in allCodes)
        {
            var message = ErrorMessages.Get(code);
            message.Should().NotBe(code.ToString(),
                $"ErrorCode.{code} ({(int)code}) 应在 ErrorMessages 中注册中文消息");
        }
    }

    #endregion
}
