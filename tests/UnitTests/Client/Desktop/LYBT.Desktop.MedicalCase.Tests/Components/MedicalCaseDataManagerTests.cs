using FluentAssertions;
using LYBT.Desktop.Contracts.Api; // Issue #2164: 添加Api接口引用
using LYBT.Desktop.MedicalCase.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseDataManager单元测试 - Issue #1778
    /// </summary>
    public class MedicalCaseDataManagerTests
    {
        private readonly Mock<IMedicalCaseRepository> _mockRepository;
        private readonly Mock<IMedicalCaseApi> _mockApi; // Issue #2164: 添加Api mock
        private readonly Mock<ILogger<MedicalCaseDataManager>> _mockLogger;
        private readonly MedicalCaseDataManager _sut;

        public MedicalCaseDataManagerTests()
        {
            _mockRepository = new Mock<IMedicalCaseRepository>();
            _mockApi = new Mock<IMedicalCaseApi>(); // Issue #2164: 初始化Api mock
            _mockLogger = new Mock<ILogger<MedicalCaseDataManager>>();
            _sut = new MedicalCaseDataManager(_mockRepository.Object, _mockApi.Object, _mockLogger.Object); // Issue #2164: 添加api参数
        }

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_ShouldLoadMedicalCaseDetail_WhenValidId()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var expectedDetail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(expectedDetail);

            // Act
            await _sut.InitializeAsync(medicalCaseId);

            // Assert
            _sut.Current.Should().NotBeNull();
            _sut.Current!.Id.Should().Be(medicalCaseId);
            _sut.CurrentConsultation.Should().NotBeNull();
            _sut.CurrentPrescription.Should().NotBeNull();
            _sut.HasChanges.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeAsync_ShouldThrowException_WhenIdNotFound()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseDetailDto?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.InitializeAsync(medicalCaseId));
        }

        #endregion

        #region SaveAsync Tests

        [Fact]
        public async Task SaveAsync_ShouldReturnTrue_WhenNoChanges()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);
            await _sut.InitializeAsync(medicalCaseId);

            // Act
            var result = await _sut.SaveAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseInputDto>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_ShouldSaveMedicalCase_WhenBasicInfoChanged()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(detail);

            await _sut.InitializeAsync(medicalCaseId);

            // 修改数据触发变更
            _sut.Current!.ChiefComplaint = "新的主诉";

            // Act
            var result = await _sut.SaveAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseInputDto>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ShouldSaveConsultation_WhenConsultationChanged()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);
            _mockRepository.Setup(x => x.UpdateConsultationAsync(It.IsAny<Guid>(), It.IsAny<ConsultationInputDto>()))
                .ReturnsAsync(detail.Consultation);

            await _sut.InitializeAsync(medicalCaseId);

            // 修改诊疗数据
            _sut.CurrentConsultation!.TCMDiagnosis = "新的中医诊断";

            // Act
            var result = await _sut.SaveAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateConsultationAsync(medicalCaseId, It.IsAny<ConsultationInputDto>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ShouldSavePrescription_WhenPrescriptionChanged()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);
            _mockRepository.Setup(x => x.UpdatePrescriptionAsync(It.IsAny<Guid>(), It.IsAny<PrescriptionUpdateDto>()))
                .ReturnsAsync(detail.Prescription);

            await _sut.InitializeAsync(medicalCaseId);

            // 修改处方数据
            _sut.CurrentPrescription!.DosageCount = 7;

            // Act
            var result = await _sut.SaveAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdatePrescriptionAsync(medicalCaseId, It.IsAny<PrescriptionUpdateDto>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ShouldReturnFalse_WhenCurrentIsNull()
        {
            // Arrange - 不初始化数据

            // Act
            var result = await _sut.SaveAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldDeleteAndClearData_WhenSuccessful()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);
            _mockRepository.Setup(x => x.DeleteAsync(medicalCaseId))
                .ReturnsAsync(true);

            await _sut.InitializeAsync(medicalCaseId);

            // Act
            var result = await _sut.DeleteAsync();

            // Assert
            result.Should().BeTrue();
            _sut.Current.Should().BeNull();
            _mockRepository.Verify(x => x.DeleteAsync(medicalCaseId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenCurrentIsNull()
        {
            // Arrange - 不初始化数据

            // Act
            var result = await _sut.DeleteAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ReloadAsync Tests

        [Fact]
        public async Task ReloadAsync_ShouldReloadData_WhenCurrentExists()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var originalDetail = CreateMedicalCaseDetail(medicalCaseId);
            var reloadedDetail = CreateMedicalCaseDetail(medicalCaseId);
            reloadedDetail.ChiefComplaint = "重新加载后的主诉";

            _mockRepository.SetupSequence(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(originalDetail)
                .ReturnsAsync(reloadedDetail);

            await _sut.InitializeAsync(medicalCaseId);
            var originalComplaint = _sut.Current!.ChiefComplaint;

            // Act
            await _sut.ReloadAsync();

            // Assert
            _sut.Current!.ChiefComplaint.Should().Be("重新加载后的主诉");
            _sut.Current.ChiefComplaint.Should().NotBe(originalComplaint);
        }

        #endregion

        #region Prescription Management Tests

        [Fact]
        public async Task CreatePrescriptionAsync_ShouldCreateAndSetPrescription_WhenSuccessful()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            detail.Prescription = null; // 初始无处方

            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);

            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "风寒感冒",
                DosageCount = 3
            };

            var createdPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                Indication = createDto.Diagnosis,
                DosageCount = createDto.DosageCount
            };

            _mockRepository.Setup(x => x.CreatePrescriptionAsync(medicalCaseId, createDto))
                .ReturnsAsync(createdPrescription);

            await _sut.InitializeAsync(medicalCaseId);

            // Act
            var result = await _sut.CreatePrescriptionAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(createdPrescription.Id);
            _sut.CurrentPrescription.Should().NotBeNull();
            _sut.CurrentPrescription!.Id.Should().Be(createdPrescription.Id);
        }

        [Fact]
        public async Task DeletePrescriptionAsync_ShouldDeleteAndClearPrescription_WhenSuccessful()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);
            _mockRepository.Setup(x => x.DeletePrescriptionAsync(medicalCaseId))
                .Returns(Task.CompletedTask);

            await _sut.InitializeAsync(medicalCaseId);

            // Act
            var result = await _sut.DeletePrescriptionAsync();

            // Assert
            result.Should().BeTrue();
            _sut.CurrentPrescription.Should().BeNull();
            _mockRepository.Verify(x => x.DeletePrescriptionAsync(medicalCaseId), Times.Once);
        }

        #endregion

        #region HasChanges Tests

        [Fact]
        public async Task HasChanges_ShouldReturnTrue_WhenMedicalCaseChanged()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);

            await _sut.InitializeAsync(medicalCaseId);
            _sut.HasChanges.Should().BeFalse();

            // Act
            _sut.Current!.CaseNumber = "NEW-2025-001";

            // Assert
            _sut.HasChanges.Should().BeTrue();
        }

        [Fact]
        public async Task HasChanges_ShouldReturnTrue_WhenConsultationChanged()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);

            await _sut.InitializeAsync(medicalCaseId);

            // Act
            _sut.CurrentConsultation!.ChiefComplaint = "新的主诉内容";

            // Assert
            _sut.HasChanges.Should().BeTrue();
        }

        [Fact]
        public async Task HasChanges_ShouldReturnTrue_WhenPrescriptionChanged()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detail = CreateMedicalCaseDetail(medicalCaseId);
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(detail);

            await _sut.InitializeAsync(medicalCaseId);

            // Act
            _sut.CurrentPrescription!.DosageCount = 14;

            // Assert
            _sut.HasChanges.Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private MedicalCaseDetailDto CreateMedicalCaseDetail(Guid medicalCaseId)
        {
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            return new MedicalCaseDetailDto
            {
                Id = medicalCaseId,
                CaseNumber = "MC-2025-001",
                ChiefComplaint = "感冒发热",
                PatientId = patientId,
                PatientName = "张三",
                PatientGender = "男",
                PatientAge = 30,
                DoctorId = doctorId,
                DoctorName = "李医生",
                ConsultationDate = DateTime.Now,
                CaseStatus = (MedicalCaseStatus)CaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Consultation = new ConsultationDto
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCaseId,
                    PatientId = patientId,
                    UserId = doctorId,
                    ChiefComplaint = "感冒发热",
                    PresentIllness = "现病史",
                    Inspection = "望诊",
                    AuscultationOlfaction = "闻诊",
                    Inquiry = "问诊",
                    Palpation = "切诊",
                    TCMDiagnosis = "风寒感冒",
                    TreatmentPrinciple = "辛温解表",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                Prescription = new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PrescriptionNumber = "RX-2025-001",
                    MedicalCaseId = medicalCaseId,
                    PatientId = patientId,
                    UserId = doctorId,
                    Indication = "风寒感冒",
                    DosageCount = 3,
                    Usage = "水煎服，每日一剂",
                    Discount = 1.0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }

        #endregion
    }
}
