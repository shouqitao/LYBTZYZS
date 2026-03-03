using FluentAssertions;
using LYBT.Shared.Primitives.ErrorCodes;
using Xunit;

namespace LYBT.Tests.Unit.Shared.ExceptionHandling.ErrorCodes;

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
    public void GetModuleName_OtherModules_ReturnsCorrectModule(ErrorCode errorCode, string expectedModule)
    {
        // Act
        var result = errorCode.GetModuleName();

        // Assert
        result.Should().Be(expectedModule);
    }

    [Theory]
    [InlineData(ErrorCode.UnsupportedEntityType, "Sync")]
    [InlineData(ErrorCode.SyncDataConflict, "Sync")]
    [InlineData(ErrorCode.HerbUploadFailed, "Sync")]
    [InlineData(ErrorCode.SyncPatientNotFound, "Sync")]
    [InlineData(ErrorCode.SyncEntityNotFound, "Sync")]
    [InlineData(ErrorCode.SyncNoEntityTypeSelected, "Sync")]
    [InlineData(ErrorCode.SyncLocalActiveCasesExist, "Sync")]
    public void GetModuleName_SyncErrors_ReturnsSync(ErrorCode errorCode, string expectedModule)
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
    [InlineData(ErrorCode.UnsupportedEntityType, 400)]
    [InlineData(ErrorCode.JsonDeserializeFailed, 400)]
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
    [InlineData(ErrorCode.SyncEntityNotFound, 404)]
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
    [InlineData(ErrorCode.SyncDataConflict, 409)]
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
    [InlineData(ErrorCode.SyncPatientNotFound, 422)]
    [InlineData(ErrorCode.SyncHerbNotFound, 422)]
    [InlineData(ErrorCode.SyncCaseLocked, 422)]
    [InlineData(ErrorCode.SyncHerbHasReference, 422)]
    [InlineData(ErrorCode.SyncPatientHasReference, 422)]
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

    [Theory]
    [InlineData(ErrorCode.HerbUploadFailed, 500)]
    [InlineData(ErrorCode.PatientUploadFailed, 500)]
    [InlineData(ErrorCode.FormulaUploadFailed, 500)]
    [InlineData(ErrorCode.MedicalCaseUploadFailed, 500)]
    [InlineData(ErrorCode.SyncReferenceCheckFailed, 500)]
    public void ToHttpStatusCode_SyncUploadErrors_Returns500(ErrorCode errorCode, int expectedStatus)
    {
        // Act
        var result = errorCode.ToHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    #endregion

    #region 错误类别测试

    [Theory]
    [InlineData(ErrorCode.ValidationFailed, ErrorCategory.Validation)]
    [InlineData(ErrorCode.InvalidRequest, ErrorCategory.Validation)]
    [InlineData(ErrorCode.UnsupportedEntityType, ErrorCategory.Validation)]
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
    [InlineData(ErrorCode.SyncEntityNotFound, ErrorCategory.Resource)]
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
    [InlineData(ErrorCode.HerbUploadFailed, ErrorCategory.System)]
    [InlineData(ErrorCode.SyncReferenceCheckFailed, ErrorCategory.System)]
    public void ToCategory_SystemErrors_ReturnsSystem(ErrorCode errorCode, ErrorCategory expectedCategory)
    {
        // Act
        var result = errorCode.ToCategory();

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(ErrorCode.SyncDataConflict, ErrorCategory.Concurrency)]
    [InlineData(ErrorCode.ConcurrencyConflict, ErrorCategory.Concurrency)]
    [InlineData(ErrorCode.MedicalCaseVersionConflict, ErrorCategory.Concurrency)]
    public void ToCategory_ConcurrencyErrors_ReturnsConcurrency(ErrorCode errorCode, ErrorCategory expectedCategory)
    {
        // Act
        var result = errorCode.ToCategory();

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(ErrorCode.SyncFailed, ErrorCategory.Business)]
    [InlineData(ErrorCode.SyncLocalActiveCasesExist, ErrorCategory.Business)]
    [InlineData(ErrorCode.SyncDependencyNotSynced, ErrorCategory.Business)]
    public void ToCategory_SyncBusinessErrors_ReturnsBusiness(ErrorCode errorCode, ErrorCategory expectedCategory)
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
    [InlineData(ErrorCode.UnsupportedEntityType, "ERR-70101")]
    [InlineData(ErrorCode.SyncDataConflict, "ERR-70103")]
    [InlineData(ErrorCode.HerbUploadFailed, "ERR-70201")]
    [InlineData(ErrorCode.SyncLocalActiveCasesExist, "ERR-70506")]
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

    #region Sync 错误码完整性测试

    [Fact]
    public void SyncErrorCodes_AllDefined_HaveMessages()
    {
        // Arrange - 全部 20 个 Sync 错误码
        var syncCodes = new[]
        {
            ErrorCode.UnsupportedEntityType,      // 70101
            ErrorCode.JsonDeserializeFailed,       // 70102
            ErrorCode.SyncDataConflict,            // 70103
            ErrorCode.HerbUploadFailed,            // 70201
            ErrorCode.PatientUploadFailed,         // 70202
            ErrorCode.FormulaUploadFailed,         // 70203
            ErrorCode.MedicalCaseUploadFailed,     // 70204
            ErrorCode.SyncPatientNotFound,         // 70301
            ErrorCode.SyncHerbNotFound,            // 70302
            ErrorCode.SyncCaseLocked,              // 70304
            ErrorCode.SyncReferenceCheckFailed,    // 70401
            ErrorCode.SyncHerbHasReference,        // 70402
            ErrorCode.SyncPatientHasReference,     // 70403
            ErrorCode.SyncEntityNotFound,          // 70404
            ErrorCode.SyncNoEntityTypeSelected,    // 70501
            ErrorCode.SyncFailed,                  // 70502
            ErrorCode.SyncChecksumTypeError,       // 70503
            ErrorCode.SyncDependencyNotSynced,     // 70504
            ErrorCode.SyncPatientRemapFailed,      // 70505
            ErrorCode.SyncLocalActiveCasesExist    // 70506
        };

        // Act & Assert
        syncCodes.Should().HaveCount(20, "PRD sync.md 定义 20 个错误码");

        foreach (var code in syncCodes)
        {
            code.GetModuleName().Should().Be("Sync", $"{code} 应属于 Sync 模块");
            ErrorMessages.Get(code).Should().NotBe(code.ToString(), $"{code} 应有中文消息");
            ErrorMessages.Get(code, useEnglish: true).Should().NotBe(code.ToString(), $"{code} 应有英文消息");
        }
    }

    [Fact]
    public void SyncErrorCodes_701xx_AreServerCommonErrors()
    {
        // 701xx: 服务端通用错误
        ErrorCode.UnsupportedEntityType.ToHttpStatusCode().Should().Be(400);
        ErrorCode.JsonDeserializeFailed.ToHttpStatusCode().Should().Be(400);
        ErrorCode.SyncDataConflict.ToHttpStatusCode().Should().Be(409);
    }

    [Fact]
    public void SyncErrorCodes_702xx_AreUploadErrors()
    {
        // 702xx: 上传失败都是 500
        var uploadCodes = new[]
        {
            ErrorCode.HerbUploadFailed,
            ErrorCode.PatientUploadFailed,
            ErrorCode.FormulaUploadFailed,
            ErrorCode.MedicalCaseUploadFailed
        };

        foreach (var code in uploadCodes)
        {
            code.ToHttpStatusCode().Should().Be(500, $"{code} 上传失败应返回 500");
            code.ToCategory().Should().Be(ErrorCategory.System, $"{code} 上传失败属于系统错误");
        }
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
            ErrorCode.HerbImportExcelError,        // 50304
            ErrorCode.HerbImportNoData             // 50305
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
        errorCode.GetModuleName().Should().Be("Users");
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
