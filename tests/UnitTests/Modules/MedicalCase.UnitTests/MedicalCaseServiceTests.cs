using AutoMapper;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests
{
    public class MedicalCaseServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicalCaseService _service;
        private readonly Mock<IMedicalCaseRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<MedicalCaseService>> _mockLogger;

        public MedicalCaseServiceTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _mockRepository = new Mock<IMedicalCaseRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<MedicalCaseService>>();

            _service = new MedicalCaseService(
                _context,
                _mockRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object);

            // 初始化测试数据
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // 添加测试患者
            var patient = new PatientModel
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "测试患者",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now
            };
            _context.Patients.Add(patient);

            // 添加测试医生
            var doctor = new UserModel
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "testdoctor",
                RealName = "测试医生",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now
            };
            _context.Users.Add(doctor);

            _context.SaveChanges();
        }

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnMedicalCaseDetail()
        {
            // Arrange
            var id = Guid.NewGuid();
            var medicalCaseModel = CreateMedicalCaseModel(id: id);
            var expectedDto = new MedicalCaseDetailDto
            {
                Id = id,
                PatientId = medicalCaseModel.PatientId,
                Status = medicalCaseModel.Status,
                CreateTime = medicalCaseModel.CreateTime
            };

            _mockRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(medicalCaseModel);
            _mockMapper.Setup(x => x.Map<MedicalCaseDetailDto>(medicalCaseModel))
                .Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.PatientId.Should().Be(medicalCaseModel.PatientId);
            result.Status.Should().Be(medicalCaseModel.Status);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(invalidId))
                .ReturnsAsync((MedicalCaseModel?)null);

            // Act
            var result = await _service.GetByIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithException_ShouldThrowAndLogError()
        {
            // Arrange
            var id = Guid.NewGuid();
            var exception = new Exception("Database error");
            _mockRepository.Setup(x => x.GetByIdAsync(id))
                .ThrowsAsync(exception);

            // Act & Assert
            var act = async () => await _service.GetByIdAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        }

        #endregion

        #region GetAllAsync 测试

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllMedicalCases()
        {
            // Arrange
            var medicalCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(),
                CreateMedicalCaseModel(),
                CreateMedicalCaseModel()
            };
            var expectedDtos = new List<MedicalCaseDto>
            {
                new MedicalCaseDto { Id = medicalCases[0].Id },
                new MedicalCaseDto { Id = medicalCases[1].Id },
                new MedicalCaseDto { Id = medicalCases[2].Id }
            };

            _mockRepository.Setup(x => x.GetListAsync())
                .ReturnsAsync(medicalCases);
            _mockMapper.Setup(x => x.Map<List<MedicalCaseDto>>(medicalCases))
                .Returns(expectedDtos);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(expectedDtos);
        }

        [Fact]
        public async Task GetAllAsync_WithException_ShouldThrowAndLogError()
        {
            // Arrange
            var exception = new Exception("Repository error");
            _mockRepository.Setup(x => x.GetListAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            var act = async () => await _service.GetAllAsync();
            await act.Should().ThrowAsync<Exception>().WithMessage("Repository error");
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateMedicalCase()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Remark = "测试创建"
            };

            var createdModel = CreateMedicalCaseModel(
                patientId: createDto.PatientId,
                userId: createDto.DoctorId,
                remark: createDto.Remark);

            var expectedDto = new MedicalCaseDetailDto
            {
                Id = createdModel.Id,
                PatientId = createDto.PatientId,
                Status = MedicalCaseStatus.Registered,
                Remark = createDto.Remark
            };

            _mockMapper.Setup(x => x.Map<MedicalCaseModel>(createDto))
                .Returns(createdModel);
            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync(createdModel);
            _mockMapper.Setup(x => x.Map<MedicalCaseDetailDto>(createdModel))
                .Returns(expectedDto);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.PatientId.Should().Be(createDto.PatientId);
            result.Status.Should().Be(MedicalCaseStatus.Registered);
            result.Remark.Should().Be(createDto.Remark);
        }

        [Fact]
        public async Task CreateAsync_WithException_ShouldThrowAndLogError()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };
            var exception = new Exception("Create failed");

            _mockMapper.Setup(x => x.Map<MedicalCaseModel>(createDto))
                .Returns(CreateMedicalCaseModel());
            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<MedicalCaseModel>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var act = async () => await _service.CreateAsync(createDto);
            await act.Should().ThrowAsync<Exception>().WithMessage("Create failed");
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldUpdateMedicalCase()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existingModel = CreateMedicalCaseModel(id: id, status: MedicalCaseStatus.Registered);
            var updateDto = new MedicalCaseUpdateDto
            {
                Status = MedicalCaseStatus.InConsultation,
                Remark = "更新备注",
                CompleteTime = DateTime.Now
            };

            _mockRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(existingModel);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(id, updateDto);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<MedicalCaseModel>(m =>
                m.Id == id &&
                m.Status == MedicalCaseStatus.InConsultation &&
                m.Remark == updateDto.Remark &&
                m.CompleteTime == updateDto.CompleteTime &&
                m.UpdateTime != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto { Status = MedicalCaseStatus.Completed };

            _mockRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel?)null);

            // Act
            var result = await _service.UpdateAsync(id, updateDto);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseModel>()), Times.Never);
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldSoftDelete()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existingModel = CreateMedicalCaseModel(id: id, isActive: true);

            _mockRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(existingModel);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<MedicalCaseModel>(m =>
                m.Id == id &&
                m.IsActive == false &&
                m.UpdateTime != null)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel?)null);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetByPatientIdAsync 测试

        [Fact]
        public async Task GetByPatientIdAsync_WithValidPatientId_ShouldReturnPatientCases()
        {
            // Arrange
            var patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var medicalCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(patientId: patientId),
                CreateMedicalCaseModel(patientId: patientId)
            };
            var expectedDtos = new List<MedicalCaseDto>
            {
                new MedicalCaseDto { Id = medicalCases[0].Id, PatientId = patientId },
                new MedicalCaseDto { Id = medicalCases[1].Id, PatientId = patientId }
            };

            _mockRepository.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(medicalCases);
            _mockMapper.Setup(x => x.Map<List<MedicalCaseDto>>(medicalCases))
                .Returns(expectedDtos);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(dto => dto.PatientId.Should().Be(patientId));
        }

        #endregion

        #region 工作流测试

        [Fact]
        public async Task UpdateStatusAsync_WithValidId_ShouldUpdateStatus()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = CreateMedicalCaseModel(id: caseId, status: MedicalCaseStatus.Registered);
            var newStatus = MedicalCaseStatus.InConsultation;

            _mockRepository.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateStatusAsync(caseId, newStatus);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<MedicalCaseModel>(m =>
                m.Id == caseId &&
                m.Status == newStatus &&
                m.UpdateTime != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ToCompleted_ShouldSetCompleteTime()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = CreateMedicalCaseModel(id: caseId, status: MedicalCaseStatus.InConsultation);

            _mockRepository.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateStatusAsync(caseId, MedicalCaseStatus.Completed);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<MedicalCaseModel>(m =>
                m.Id == caseId &&
                m.Status == MedicalCaseStatus.Completed &&
                m.CompleteTime != null &&
                m.UpdateTime != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _service.UpdateStatusAsync(invalidId, MedicalCaseStatus.Completed);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task StartConsultationAsync_WithValidData_ShouldUpdateCase()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var consultationId = Guid.NewGuid();
            var medicalCase = CreateMedicalCaseModel(id: caseId, status: MedicalCaseStatus.Registered);
            
            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.StartConsultationAsync(caseId, consultationId);

            // Assert
            result.Should().BeTrue();
            
            var updatedCase = await _context.MedicalCases.FindAsync(caseId);
            updatedCase!.ConsultationId.Should().Be(consultationId);
            updatedCase.Status.Should().Be(MedicalCaseStatus.InConsultation);
            updatedCase.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task StartConsultationAsync_WithNonExistentCase_ShouldReturnFalse()
        {
            // Arrange
            var invalidCaseId = Guid.NewGuid();
            var consultationId = Guid.NewGuid();

            // Act
            var result = await _service.StartConsultationAsync(invalidCaseId, consultationId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteConsultationAsync_WithValidData_ShouldCompleteCase()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var medicalCase = CreateMedicalCaseModel(id: caseId, status: MedicalCaseStatus.InConsultation);
            
            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CompleteConsultationAsync(caseId, prescriptionId);

            // Assert
            result.Should().BeTrue();
            
            var updatedCase = await _context.MedicalCases.FindAsync(caseId);
            updatedCase!.Status.Should().Be(MedicalCaseStatus.Completed);
            updatedCase.UpdateTime.Should().NotBeNull();
            updatedCase.CompleteTime.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteMedicalCaseAsync_WithValidId_ShouldCompleteCase()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = CreateMedicalCaseModel(id: caseId, status: MedicalCaseStatus.InConsultation);

            _mockRepository.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CompleteMedicalCaseAsync(caseId);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<MedicalCaseModel>(m =>
                m.Id == caseId &&
                m.Status == MedicalCaseStatus.Completed)), Times.Once);
        }

        [Fact]
        public async Task CancelMedicalCaseAsync_WithValidData_ShouldCancelCase()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var reason = "患者临时有事";
            var medicalCase = CreateMedicalCaseModel(id: caseId, status: MedicalCaseStatus.Registered);
            
            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CancelMedicalCaseAsync(caseId, reason);

            // Assert
            result.Should().BeTrue();
            
            var updatedCase = await _context.MedicalCases.FindAsync(caseId);
            updatedCase!.Status.Should().Be(MedicalCaseStatus.Cancelled);
            updatedCase.Remark.Should().Be(reason);
            updatedCase.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTodayByUserIdAsync_WithTodayCases_ShouldReturnTodayOnly()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var today = DateTime.Today;
            
            var todayCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(userId: doctorId),
                CreateMedicalCaseModel(userId: doctorId)
            };
            
            var yesterdayCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(userId: doctorId)
            };
            yesterdayCases[0].CreateTime = today.AddDays(-1);

            _context.MedicalCases.AddRange(todayCases);
            _context.MedicalCases.AddRange(yesterdayCases);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetTodayByUserIdAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(c => c.UserId.Should().Be(doctorId));
            result.Should().AllSatisfy(c => c.CreateTime.Date.Should().Be(today));
        }

        [Fact]
        public async Task GetPendingCasesByStatusAsync_WithSpecificStatus_ShouldReturnFilteredCases()
        {
            // Arrange
            var registeredCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(status: MedicalCaseStatus.Registered),
                CreateMedicalCaseModel(status: MedicalCaseStatus.Registered)
            };
            
            var inConsultationCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(status: MedicalCaseStatus.InConsultation)
            };
            
            var inactiveCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(status: MedicalCaseStatus.Registered, isActive: false)
            };

            _context.MedicalCases.AddRange(registeredCases);
            _context.MedicalCases.AddRange(inConsultationCases);
            _context.MedicalCases.AddRange(inactiveCases);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetPendingCasesByStatusAsync(MedicalCaseStatus.Registered);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(c => c.Status.Should().Be(MedicalCaseStatus.Registered));
            result.Should().AllSatisfy(c => c.IsActive.Should().BeTrue());
            result.Should().BeInAscendingOrder(c => c.CreateTime);
        }

        [Fact]
        public async Task GetPagedAsync_WithFilters_ShouldReturnFilteredResults()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            
            var validCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(status: MedicalCaseStatus.Registered),
                CreateMedicalCaseModel(status: MedicalCaseStatus.Registered)
            };
            validCases[0].CreateTime = today.AddHours(9);
            validCases[1].CreateTime = today.AddHours(10);
            
            var invalidCases = new List<MedicalCaseModel>
            {
                CreateMedicalCaseModel(status: MedicalCaseStatus.InConsultation),
                CreateMedicalCaseModel(status: MedicalCaseStatus.Registered)
            };
            invalidCases[1].CreateTime = yesterday;

            _context.MedicalCases.AddRange(validCases);
            _context.MedicalCases.AddRange(invalidCases);
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _service.GetPagedAsync(
                pageIndex: 1,
                pageSize: 10,
                status: MedicalCaseStatus.Registered,
                startDate: today,
                endDate: today.AddDays(1));

            // Assert
            items.Should().NotBeNull();
            items.Should().HaveCount(2);
            total.Should().Be(2);
            items.Should().AllSatisfy(c => c.Status.Should().Be(MedicalCaseStatus.Registered));
            items.Should().AllSatisfy(c => c.CreateTime.Date.Should().Be(today));
            items.Should().BeInDescendingOrder(c => c.CreateTime);
        }

        [Fact]
        public async Task GetPagedAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var cases = new List<MedicalCaseModel>();
            for (int i = 0; i < 15; i++)
            {
                cases.Add(CreateMedicalCaseModel());
            }
            
            _context.MedicalCases.AddRange(cases);
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _service.GetPagedAsync(pageIndex: 2, pageSize: 5);

            // Assert
            items.Should().NotBeNull();
            items.Should().HaveCount(5);
            total.Should().Be(15);
        }

        #endregion

        #region 辅助方法

        private MedicalCaseModel CreateMedicalCaseModel(
            Guid? id = null,
            Guid? patientId = null,
            Guid? userId = null,
            MedicalCaseStatus status = MedicalCaseStatus.Registered,
            bool isActive = true,
            string remark = "测试案例")
        {
            return new MedicalCaseModel
            {
                Id = id ?? Guid.NewGuid(),
                PatientId = patientId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = userId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Status = status,
                IsActive = isActive,
                CreateTime = DateTime.Now,
                Remark = remark
            };
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}