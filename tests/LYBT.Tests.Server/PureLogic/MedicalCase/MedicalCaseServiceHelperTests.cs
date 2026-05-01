using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.MedicalCase;

/// <summary>
/// MedicalCaseServiceHelper 单元测试
/// 测试职责: 克隆、权限检查、并发重试
/// 不测试: HTTP、DI、持久化 (集成测试覆盖)
/// </summary>
public class MedicalCaseServiceHelperTests
{
    #region CloneMedicalCaseForAudit 测试

    [Fact]
    public void CloneMedicalCaseForAudit_WithFullData_ShouldCloneAllFields()
    {
        // Arrange
        var source = CreateTestMedicalCase();

        // Act
        var clone = MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        // Assert
        clone.Id.Should().Be(source.Id);
        clone.PatientId.Should().Be(source.PatientId);
        clone.PatientName.Should().Be(source.PatientName);
        clone.UserId.Should().Be(source.UserId);
        clone.DoctorName.Should().Be(source.DoctorName);
        clone.CaseStatus.Should().Be(source.CaseStatus);
        clone.CompletedAt.Should().Be(source.CompletedAt);
        clone.Remark.Should().Be(source.Remark);
        clone.NeedsPrescription.Should().Be(source.NeedsPrescription);
        clone.IsDeleted.Should().Be(source.IsDeleted);
        clone.CreatedAt.Should().Be(source.CreatedAt);
        clone.UpdatedAt.Should().Be(source.UpdatedAt);
    }

    [Fact]
    public void CloneMedicalCaseForAudit_WithConsultation_ShouldCloneConsultation()
    {
        // Arrange
        var source = CreateTestMedicalCase();

        // Act
        var clone = MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        // Assert
        clone.Consultation.Should().NotBeNull();
        clone.Consultation!.Id.Should().Be(source.Consultation!.Id);
        clone.Consultation.PresentIllness.Should().Be(source.Consultation.PresentIllness);
        clone.Consultation.TongueDiagnosis.Should().Be(source.Consultation.TongueDiagnosis);
        clone.Consultation.PulseDiagnosis.Should().Be(source.Consultation.PulseDiagnosis);
        clone.Consultation.TcmDiagnosis.Should().Be(source.Consultation.TcmDiagnosis);
        clone.Consultation.UpdatedAt.Should().Be(source.Consultation.UpdatedAt);
    }

    [Fact]
    public void CloneMedicalCaseForAudit_WithPrescription_ShouldClonePrescription()
    {
        // Arrange
        var source = CreateTestMedicalCase();

        // Act
        var clone = MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        // Assert
        clone.Prescription.Should().NotBeNull();
        clone.Prescription!.Id.Should().Be(source.Prescription!.Id);
        clone.Prescription.MedicalCaseId.Should().Be(source.Prescription.MedicalCaseId);
        clone.Prescription.DosageCount.Should().Be(source.Prescription.DosageCount);
        clone.Prescription.Discount.Should().Be(source.Prescription.Discount);
        clone.Prescription.Advice.Should().Be(source.Prescription.Advice);
        clone.Prescription.ReferencedFormulas.Should().Be(source.Prescription.ReferencedFormulas);
        clone.Prescription.IsDeleted.Should().Be(source.Prescription.IsDeleted);
        clone.Prescription.UpdatedAt.Should().Be(source.Prescription.UpdatedAt);
    }

    [Fact]
    public void CloneMedicalCaseForAudit_WithNullConsultation_ShouldCloneNull()
    {
        // Arrange
        var source = CreateTestMedicalCase();
        source.Consultation = null;

        // Act
        var clone = MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        // Assert
        clone.Consultation.Should().BeNull();
    }

    [Fact]
    public void CloneMedicalCaseForAudit_WithNullPrescription_ShouldCloneNull()
    {
        // Arrange
        var source = CreateTestMedicalCase();
        source.Prescription = null;

        // Act
        var clone = MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        // Assert
        clone.Prescription.Should().BeNull();
    }

    [Fact]
    public void CloneMedicalCaseForAudit_ShouldCreateIndependentCopy()
    {
        // Arrange
        var source = CreateTestMedicalCase();

        // Act
        var clone = MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        // Assert - 修改克隆不应影响源
        clone.PatientName = "Modified";
        clone.Consultation!.TcmDiagnosis = "Modified";
        clone.Prescription!.DosageCount = 99;

        source.PatientName.Should().NotBe("Modified");
        source.Consultation!.TcmDiagnosis.Should().NotBe("Modified");
        source.Prescription!.DosageCount.Should().NotBe(99);
    }

    #endregion

    #region ExecuteWithConcurrencyRetryAsync 测试

    [Fact]
    public async Task ExecuteWithConcurrencyRetryAsync_WithSuccessfulAction_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        var logger = NullLogger.Instance;

        // Act
        var result = await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
            () => Task.FromResult(expectedResult),
            "TestOperation",
            logger);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteWithConcurrencyRetryAsync_WithDbUpdateConcurrencyException_ShouldRetry()
    {
        // Arrange
        var attemptCount = 0;
        var logger = NullLogger.Instance;

        // Act
        var result = await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
            async () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Concurrency conflict");
                return await Task.FromResult("success");
            },
            "TestOperation",
            logger);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteWithConcurrencyRetryAsync_WithMaxRetriesExceeded_ShouldThrow()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act & Assert
        // 注意: 当 attempt == maxRetries 时, catch 条件 (attempt < maxRetries) 为 false,
        // 所以最后一次重试的异常会直接抛出, 而不是 InvalidOperationException
        var act = async () => await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync<string>(
            async () =>
            {
                await Task.CompletedTask;
                throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Concurrency conflict");
            },
            "TestOperation",
            logger,
            maxRetries: 3);

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
    }

    #endregion

    #region EnsureCanEdit 测试

    [Fact]
    public void EnsureCanEdit_WithValidPermission_ShouldNotThrow()
    {
        // Arrange
        var medicalCase = CreateTestMedicalCase();
        var userId = medicalCase.UserId;
        var logger = NullLogger.Instance;

        // 创建一个简单的权限服务 mock
        var permissionService = new TestPermissionService(canEdit: true, canDelete: true);

        // Act
        var act = () => MedicalCaseServiceHelper.EnsureCanEdit(
            permissionService, medicalCase, userId, false, "TestOperation", logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_WithInvalidPermission_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var medicalCase = CreateTestMedicalCase();
        var userId = Guid.NewGuid(); // 不同的用户
        var logger = NullLogger.Instance;

        var permissionService = new TestPermissionService(canEdit: false, canDelete: false);

        // Act
        var act = () => MedicalCaseServiceHelper.EnsureCanEdit(
            permissionService, medicalCase, userId, false, "TestOperation", logger);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*无权限编辑此医案*");
    }

    #endregion

    #region EnsureCanDelete 测试

    [Fact]
    public void EnsureCanDelete_WithValidPermission_ShouldNotThrow()
    {
        // Arrange
        var medicalCase = CreateTestMedicalCase();
        var userId = medicalCase.UserId;
        var logger = NullLogger.Instance;

        var permissionService = new TestPermissionService(canEdit: true, canDelete: true);

        // Act
        var act = () => MedicalCaseServiceHelper.EnsureCanDelete(
            permissionService, medicalCase, userId, false, "TestOperation", logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanDelete_WithInvalidPermission_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var medicalCase = CreateTestMedicalCase();
        var userId = Guid.NewGuid(); // 不同的用户
        var logger = NullLogger.Instance;

        var permissionService = new TestPermissionService(canEdit: false, canDelete: false);

        // Act
        var act = () => MedicalCaseServiceHelper.EnsureCanDelete(
            permissionService, medicalCase, userId, false, "TestOperation", logger);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*无权限执行此操作*");
    }

    #endregion

    #region 测试数据工厂方法

    private static Entities.MedicalCases.MedicalCase CreateTestMedicalCase()
    {
        return new Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "张三",
            UserId = Guid.NewGuid(),
            DoctorName = "李医生",
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = true,
            CompletedAt = null,
            Remark = "测试医案",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            Consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                PresentIllness = "主诉：乏力、气短",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "脉弱",
                TcmDiagnosis = "脾胃气虚",
                UpdatedAt = DateTime.UtcNow
            },
            Prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                Usage = "水煎服",
                Advice = "忌生冷",
                ReferencedFormulas = null,
                IsDeleted = false,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 简单的权限服务测试实现
    /// </summary>
    private class TestPermissionService : LYBT.Module.MedicalCases.Interfaces.IMedicalCasePermissionService
    {
        private readonly bool _canEdit;
        private readonly bool _canDelete;

        public TestPermissionService(bool canEdit, bool canDelete)
        {
            _canEdit = canEdit;
            _canDelete = canDelete;
        }

        public bool CanEdit(Guid userId, UserRole role, Entities.MedicalCases.MedicalCase medicalCase) => _canEdit;
        public bool CanEdit(Guid userId, bool isAdmin, Entities.MedicalCases.MedicalCase medicalCase) => _canEdit;
        public bool CanCreate(Guid userId, UserRole role) => true;
        public bool CanDelete(Guid userId, UserRole role, Entities.MedicalCases.MedicalCase medicalCase) => _canDelete;
        public bool CanDelete(Guid userId, bool isAdmin, Entities.MedicalCases.MedicalCase medicalCase) => _canDelete;
        public bool RequiresEditReason(Entities.MedicalCases.MedicalCase medicalCase) => false;
        public bool RequiresEditReason(Entities.MedicalCases.MedicalCase medicalCase, Guid currentUserId) => false;
        public LYBT.Shared.Models.Contracts.MedicalCase.MedicalCasePermissionDto GetPermissions(Guid userId, UserRole role, Entities.MedicalCases.MedicalCase medicalCase)
            => new() { CanEdit = _canEdit, CanDelete = _canDelete };
    }

    #endregion
}
