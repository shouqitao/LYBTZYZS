using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.MedicalCase;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Shared;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// 病案服务单元测试
    /// 测试病案记录的创建、查询、更新、删除等核心业务逻辑
    /// </summary>
    public class MedicalCaseServiceTests : TestBase
    {
        private readonly MedicalCaseService _medicalCaseService;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;
        private readonly AppDbContext _context;

        public MedicalCaseServiceTests()
        {
            // 创建内存数据库上下文
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _loggerMock = CreateLoggerMock<MedicalCaseService>();

            _medicalCaseService = new MedicalCaseService(
                _context,
                _repositoryMock.Object,
                _loggerMock.Object,
                Mapper);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 注册病案服务相关的依赖
            services.AddSingleton(_medicalCaseService);
            services.AddSingleton(_repositoryMock.Object);
        }

        #region 创建病案记录测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Title = "外感风寒证",
                ChiefComplaint = "咳嗽、发热三天",
                IllnessHistory = "患者三天前受凉后出现咳嗽、发热",
                PhysicalExamination = "咽红，双肺呼吸音粗",
                Diagnosis = "外感风寒证",
                TreatmentPlan = "疏风散寒，宣肺止咳",
                Status = "进行中",
                CreatedDate = DateTime.Now
            };

            var medicalCase = new MedicalCaseRecord
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                CaseNumber = createDto.CaseNumber,
                Title = createDto.Title,
                ChiefComplaint = createDto.ChiefComplaint,
                CreatedDate = createDto.CreatedDate,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<MedicalCaseRecord>()))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PatientId.Should().Be(createDto.PatientId);
            result.Data.CaseNumber.Should().Be(createDto.CaseNumber);
            result.Data.Title.Should().Be(createDto.Title);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<MedicalCaseRecord>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNullData_ShouldReturnFailure()
        {
            // Arrange
            MedicalCaseCreateDto createDto = null;

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("数据不能为空");
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateCaseNumber_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Title = "测试病案",
                CreatedDate = DateTime.Now
            };

            _repositoryMock.Setup(x => x.ExistsByCaseNumberAsync(createDto.CaseNumber))
                .ReturnsAsync(true);

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("病案号已存在");

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<MedicalCaseRecord>()), Times.Never);
        }

        #endregion

        #region 查询病案记录测试

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnRecord()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseRecord
            {
                Id = caseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Title = "测试病案",
                ChiefComplaint = "测试主诉",
                CreatedDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(caseId);
            result.Data.CaseNumber.Should().Be(medicalCase.CaseNumber);
        }

        [Fact]
        public async Task GetByPatientIdAsync_WithValidPatientId_ShouldReturnRecords()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var medicalCases = new List<MedicalCaseRecord>
            {
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = Guid.NewGuid(),
                    CaseNumber = "MC202501001",
                    Title = "感冒",
                    CreatedDate = DateTime.Now.AddDays(-10),
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = Guid.NewGuid(),
                    CaseNumber = "MC202501002",
                    Title = "胃痛",
                    CreatedDate = DateTime.Now.AddDays(-5),
                    CreatedAt = DateTime.Now.AddDays(-5)
                }
            };

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(medicalCases);

            // Act
            var result = await _medicalCaseService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.All(c => c.PatientId == patientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetByCaseNumberAsync_WithValidCaseNumber_ShouldReturnRecord()
        {
            // Arrange
            var caseNumber = "MC202501001";
            var medicalCase = new MedicalCaseRecord
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = caseNumber,
                Title = "测试病案",
                CreatedDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.GetByCaseNumberAsync(caseNumber))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _medicalCaseService.GetByCaseNumberAsync(caseNumber);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.CaseNumber.Should().Be(caseNumber);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearchTerm_ShouldReturnFilteredResults()
        {
            // Arrange
            var pageRequest = new PagedRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "感冒"
            };

            var medicalCases = new List<MedicalCaseRecord>
            {
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    CaseNumber = "MC202501001",
                    Title = "风寒感冒",
                    CreatedDate = DateTime.Now
                },
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    CaseNumber = "MC202501002",
                    Title = "风热感冒",
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            };

            _repositoryMock.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((medicalCases, 2));

            // Act
            var result = await _medicalCaseService.GetPagedAsync(pageRequest);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        #endregion

        #region 更新病案记录测试

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = caseId,
                Title = "更新后的标题",
                ChiefComplaint = "更新后的主诉",
                Diagnosis = "更新后的诊断",
                TreatmentPlan = "更新后的治疗方案",
                Status = "已完成"
            };

            var existingCase = new MedicalCaseRecord
            {
                Id = caseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Title = "原始标题",
                ChiefComplaint = "原始主诉",
                CreatedDate = DateTime.Now.AddDays(-1),
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(existingCase);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseRecord>()))
                .ReturnsAsync(existingCase);

            // Act
            var result = await _medicalCaseService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(caseId);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseRecord>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = Guid.NewGuid(),
                Title = "更新的标题"
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(updateDto.Id))
                .ReturnsAsync((MedicalCaseRecord)null);

            // Act
            var result = await _medicalCaseService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("病案记录不存在");

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseRecord>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithStatusChange_ShouldUpdateClosedDate()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = caseId,
                Status = "已完成"
            };

            var existingCase = new MedicalCaseRecord
            {
                Id = caseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Title = "测试病案",
                Status = "进行中",
                CreatedDate = DateTime.Now.AddDays(-1),
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(existingCase);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseRecord>()))
                .ReturnsAsync((MedicalCaseRecord c) => c);

            // Act
            var result = await _medicalCaseService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // 验证ClosedDate被设置
            _repositoryMock.Verify(x => x.UpdateAsync(
                It.Is<MedicalCaseRecord>(c =>
                    c.Status == "已完成" &&
                    c.ClosedDate != null
                )), Times.Once);
        }

        #endregion

        #region 删除病案记录测试

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseRecord
            {
                Id = caseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Title = "测试病案",
                IsDeleted = false
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.DeleteAsync(caseId))
                .ReturnsAsync(true);

            // Act
            var result = await _medicalCaseService.DeleteAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("成功");

            _repositoryMock.Verify(x => x.DeleteAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithActiveCase_ShouldReturnFailure()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseRecord
            {
                Id = caseId,
                CaseNumber = "MC202501001",
                Title = "测试病案",
                Status = "进行中",
                IsDeleted = false
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _medicalCaseService.DeleteAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("不能删除进行中的病案");

            _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region 统计分析测试

        [Fact]
        public async Task GetStatisticsByDoctorAsync_ShouldReturnCorrectStats()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;

            var medicalCases = new List<MedicalCaseRecord>
            {
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    CreatedDate = DateTime.Now.AddDays(-15),
                    Diagnosis = "外感风寒",
                    Status = "已完成"
                },
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    Diagnosis = "脾胃虚弱",
                    Status = "已完成"
                },
                new MedicalCaseRecord
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    Diagnosis = "外感风寒",
                    Status = "进行中"
                }
            };

            _repositoryMock.Setup(x => x.GetByDoctorIdAsync(doctorId, startDate, endDate))
                .ReturnsAsync(medicalCases);

            // Act
            var result = await _medicalCaseService.GetStatisticsByDoctorAsync(doctorId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCases.Should().Be(3);
            result.Data.CompletedCases.Should().Be(2);
            result.Data.ActiveCases.Should().Be(1);
            result.Data.DiagnosisDistribution.Should().HaveCount(2);
            result.Data.DiagnosisDistribution["外感风寒"].Should().Be(2);
            result.Data.DiagnosisDistribution["脾胃虚弱"].Should().Be(1);
        }

        #endregion

        #region 病案号生成测试

        [Fact]
        public async Task GenerateCaseNumberAsync_ShouldGenerateUniqueNumber()
        {
            // Arrange
            var lastCaseNumber = "MC202501099";
            _repositoryMock.Setup(x => x.GetLastCaseNumberAsync())
                .ReturnsAsync(lastCaseNumber);

            // Act
            var result = await _medicalCaseService.GenerateCaseNumberAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Be("MC202501100");
        }

        [Fact]
        public async Task GenerateCaseNumberAsync_WithNoExistingCases_ShouldGenerateFirstNumber()
        {
            // Arrange
            _repositoryMock.Setup(x => x.GetLastCaseNumberAsync())
                .ReturnsAsync((string)null);

            // Act
            var result = await _medicalCaseService.GenerateCaseNumberAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().StartWith("MC" + DateTime.Now.ToString("yyyyMM"));
            result.Should().EndWith("001");
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task CreateAsync_WithEmptyRequiredFields_ShouldReturnValidationError()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.Empty, // 无效的患者ID
                DoctorId = Guid.NewGuid(),
                CaseNumber = "", // 空的病案号
                Title = "", // 空的标题
                CreatedDate = DateTime.Now
            };

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("验证失败");
        }

        [Fact]
        public async Task GetPagedAsync_WithExtremePageSize_ShouldLimitToMaximum()
        {
            // Arrange
            var pageRequest = new PagedRequest
            {
                PageNumber = 1,
                PageSize = 10000 // 极大的页面大小
            };

            _repositoryMock.Setup(x => x.GetPagedAsync(
                    1,
                    100, // 应该限制为最大值100
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((new List<MedicalCaseRecord>(), 0));

            // Act
            var result = await _medicalCaseService.GetPagedAsync(pageRequest);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.PageSize.Should().BeLessThanOrEqualTo(100);
        }

        #endregion

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }
    }
}