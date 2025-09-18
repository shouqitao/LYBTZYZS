using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests
{
    /// <summary>
    /// MedicalCaseService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimpleMedicalCaseServiceTests
    {
        private readonly MedicalCaseService _medicalCaseService;
        private readonly Mock<IMedicalCaseQueryService> _mockQueryService;
        private readonly Mock<IMedicalCaseBusinessService> _mockBusinessService;

        public SimpleMedicalCaseServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IMedicalCaseQueryService>();
            _mockBusinessService = new Mock<IMedicalCaseBusinessService>();

            // 创建 MedicalCaseService 实例 (主Service委托模式)
            _medicalCaseService = new MedicalCaseService(
                _mockQueryService.Object,
                _mockBusinessService.Object);
        }

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_MedicalCase_Detail()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCaseDto = new MedicalCaseDto
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生",
                ConsultationDate = DateTime.Now,
                CaseStatus = MedicalCaseStatus.Active,
                Remark = "测试医案"
            };

            var expectedResult = ServiceResult<MedicalCaseDto>.Success(medicalCaseDto);

            _mockQueryService
                .Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(medicalCaseId);
            result.Data.PatientName.Should().Be("张三");
            result.Data.DoctorName.Should().Be("李医生");
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paged_Result()
        {
            // Arrange
            var query = new PagedQueryBaseDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            var medicalCases = new List<MedicalCaseDto>
            {
                new() { Id = Guid.NewGuid(), PatientName = "张三", DoctorName = "李医生", CaseStatus = MedicalCaseStatus.Active },
                new() { Id = Guid.NewGuid(), PatientName = "王五", DoctorName = "陈医生", CaseStatus = MedicalCaseStatus.Closed }
            };

            var expectedResult = ServiceResult<PagedResult<MedicalCaseDto>>.Success(new PagedResult<MedicalCaseDto>
            {
                Items = medicalCases,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        #endregion

        #region GetByPatientIdAsync 测试

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_Patient_MedicalCases()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var medicalCases = new List<MedicalCaseDto>
            {
                new() { Id = Guid.NewGuid(), PatientId = patientId, PatientName = "张三", CaseStatus = MedicalCaseStatus.Active },
                new() { Id = Guid.NewGuid(), PatientId = patientId, PatientName = "张三", CaseStatus = MedicalCaseStatus.Closed }
            };

            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(medicalCases);

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().OnlyContain(m => m.PatientId == patientId);
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_Should_Return_Success_When_BusinessService_Succeeds()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Remark = "测试创建医案"
            };

            var createdMedicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                PatientName = "张三", // 从患者服务获取的姓名
                DoctorId = createDto.DoctorId,
                DoctorName = "李医生", // 从用户服务获取的姓名
                CaseStatus = MedicalCaseStatus.Active,
                ConsultationDate = DateTime.Now,
                Remark = createDto.Remark
            };

            _mockBusinessService
                .Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(ServiceResult<MedicalCaseDto>.Success(createdMedicalCase));

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.PatientName.Should().Be("张三");
            result.Data.DoctorName.Should().Be("李医生");
        }

        #endregion

        #region UpdateStatusAsync 测试

        [Fact]
        public async Task UpdateStatus_Should_Return_Success_When_BusinessService_Succeeds()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var newStatus = (int)MedicalCaseStatus.Closed; // 使用int状态值

            _mockBusinessService
                .Setup(x => x.UpdateStatusAsync(medicalCaseId, "closed"))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _medicalCaseService.UpdateStatus(medicalCaseId, newStatus);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Matching_MedicalCases()
        {
            // Arrange
            var keyword = "张三";
            var medicalCases = new List<MedicalCaseDto>
            {
                new() { Id = Guid.NewGuid(), PatientName = "张三", DoctorName = "李医生" },
                new() { Id = Guid.NewGuid(), PatientName = "张三丰", DoctorName = "陈医生" }
            };

            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(medicalCases);

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_No_Match()
        {
            // Arrange
            var keyword = "不存在的患者";
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion

        #region 异常分支和边界值测试 (成功经验应用)

        [Fact]
        public async Task GetPagedAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 业务失败分支测试
            var query = new PagedQueryBaseDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<MedicalCaseDto>>.Failure("查询服务异常");

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("查询服务异常");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_MedicalCase_Not_Found()
        {
            // Arrange - 数据不存在测试
            var medicalCaseId = Guid.NewGuid();
            var expectedResult = ServiceResult<MedicalCaseDto>.Failure("医案不存在");

            _mockQueryService
                .Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("医案不存在");
        }

        [Fact]
        public async Task GetByPatientIdAsync_With_Empty_Guid_Should_Still_Work()
        {
            // Arrange - 边界值测试：空Guid
            var patientId = Guid.Empty;
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPagedAsync_With_Large_PageSize_Should_Handle_Gracefully()
        {
            // Arrange - 极端值测试：大分页尺寸
            var query = new PagedQueryBaseDto { PageIndex = 1, PageSize = 999999 };
            var medicalCases = new List<MedicalCaseDto>
            {
                new() { Id = Guid.NewGuid(), PatientName = "患者1", DoctorName = "医生1" },
                new() { Id = Guid.NewGuid(), PatientName = "患者2", DoctorName = "医生2" }
            };

            var expectedResult = ServiceResult<PagedResult<MedicalCaseDto>>.Success(new PagedResult<MedicalCaseDto>
            {
                Items = medicalCases,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 999999
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.PageSize.Should().Be(999999);
        }

        [Fact]
        public async Task SearchAsync_With_Empty_Keyword_Should_Return_Empty_List()
        {
            // Arrange - 空值测试
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 搜索失败测试
            var keyword = "张三";
            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(ServiceResult<List<MedicalCaseDto>>.Failure("搜索服务异常"));

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("搜索服务异常");
        }

        [Fact]
        public async Task CreateAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 业务操作失败测试
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Remark = "测试医案"
            };

            _mockBusinessService
                .Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(ServiceResult<MedicalCaseDto>.Failure("创建医案失败"));

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("创建医案失败");
        }

        [Fact]
        public async Task UpdateStatus_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 状态更新失败测试
            var medicalCaseId = Guid.NewGuid();
            var newStatus = (int)MedicalCaseStatus.Closed;

            _mockBusinessService
                .Setup(x => x.UpdateStatusAsync(medicalCaseId, "closed"))
                .ReturnsAsync(ServiceResult<bool>.Failure("更新医案状态失败"));

            // Act
            var result = await _medicalCaseService.UpdateStatus(medicalCaseId, newStatus);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("更新医案状态失败");
        }

        #endregion
    }
}