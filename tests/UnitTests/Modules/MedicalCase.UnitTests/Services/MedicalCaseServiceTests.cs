using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class MedicalCaseServiceTests
    {
        private readonly MedicalCaseService _medicalCaseService;
        private readonly Mock<IMedicalCaseQueryService> _mockQueryService;
        private readonly Mock<IMedicalCaseBusinessService> _mockBusinessService;

        public MedicalCaseServiceTests()
        {
            _mockQueryService = new Mock<IMedicalCaseQueryService>();
            _mockBusinessService = new Mock<IMedicalCaseBusinessService>();
            _medicalCaseService = new MedicalCaseService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new MedicalCaseService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new MedicalCaseService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new MedicalCaseSearchDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<MedicalCaseDto>>.Success(new PagedResult<MedicalCaseDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var caseDto = new MedicalCaseDto { Id = caseId };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(caseDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(caseId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(caseId), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto { PatientId = Guid.NewGuid() };
            var createdCase = new MedicalCaseDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(createdCase);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto { Id = caseId };
            var updatedCase = new MedicalCaseDto { Id = caseId };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(updatedCase);

            _mockBusinessService.Setup(x => x.UpdateAsync(caseId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.UpdateAsync(caseId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(caseId, updateDto), Times.Once);
        }

        #endregion

        #region 边界值测试

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.DeleteAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.DeleteAsync(caseId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(caseId), Times.Once);
        }

        #endregion

        #region 状态管理测试

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task CompleteAsync_Should_Delegate_To_BusinessService()
        //        {
            // Arrange
        //            var caseId = Guid.NewGuid();
        //            var expectedResult = ServiceResult<bool>.Success(true);
//
        //            _mockBusinessService.Setup(x => x.CompleteAsync(caseId)).ReturnsAsync(expectedResult);
//
            // Act
        //            var result = await _medicalCaseService.CompleteAsync(caseId);
//
            // Assert
        //            result.Should().BeSameAs(expectedResult);
        //            _mockBusinessService.Verify(x => x.CompleteAsync(caseId), Times.Once);
        //        }

        // TODO: Suspend 方法尚未实现
        //[Fact]
        //public void Suspend_Should_Delegate_To_BusinessService()
        //{
        //    // Arrange
        //    var caseId = Guid.NewGuid();
        //    var reason = "暂停原因";
        //    var expectedResult = ServiceResult<bool>.Success(true);
        //
        //    _mockBusinessService.Setup(x => x.SuspendAsync(caseId, reason)).ReturnsAsync(expectedResult);
        //
        //    // Act
        //    var result = _medicalCaseService.Suspend(caseId, reason);
        //
        //    // Assert
        //    result.Should().NotBeNull();
        //    _mockBusinessService.Verify(x => x.SuspendAsync(caseId, reason), Times.Once);
        //}

        // TODO: Resume 方法尚未实现
        //[Fact]
        //public void Resume_Should_Delegate_To_BusinessService()
        //{
        //    // Arrange
        //    var caseId = Guid.NewGuid();
        //    var expectedResult = ServiceResult<bool>.Success(true);
        //
        //    _mockBusinessService.Setup(x => x.ResumeAsync(caseId)).ReturnsAsync(expectedResult);
        //
        //    // Act
        //    var result = _medicalCaseService.Resume(caseId);
        //
        //    // Assert
        //    result.Should().NotBeNull();
        //    _mockBusinessService.Verify(x => x.ResumeAsync(caseId), Times.Once);
        //}

        // TODO: Archive 方法尚未实现
        //[Fact]
        //public void Archive_Should_Delegate_To_BusinessService()
        //{
        //    // Arrange
        //    var caseId = Guid.NewGuid();
        //    var expectedResult = ServiceResult<bool>.Success(true);
        //
        //    _mockBusinessService.Setup(x => x.ArchiveAsync(caseId)).ReturnsAsync(expectedResult);
        //
        //    // Act
        //    var result = _medicalCaseService.Archive(caseId);
        //
        //    // Assert
        //    result.Should().NotBeNull();
        //    _mockBusinessService.Verify(x => x.ArchiveAsync(caseId), Times.Once);
        //}

        // TODO: UpdateStatusAsync 方法尚未实现
        //[Fact]
        //public async Task UpdateStatus_Should_Delegate_To_BusinessService()
        //{
        //    // Arrange
        //    var caseId = Guid.NewGuid();
        //    var status = MedicalCaseStatus.Closed;
        //    var remark = "状态更新备注";
        //    var expectedResult = ServiceResult<bool>.Success(true);
        //
        //    _mockBusinessService.Setup(x => x.UpdateStatusAsync(caseId, status, remark))
        //        .ReturnsAsync(expectedResult);
        //
        //    // Act
        //    var result = await _medicalCaseService.UpdateStatus(caseId, status, remark);
        //
        //    // Assert
        //    result.Should().BeSameAs(expectedResult);
        //    _mockBusinessService.Verify(x => x.UpdateStatusAsync(caseId, status, remark), Times.Once);
        //}

        //[Fact]
        //public async Task UpdateStatus_Should_Handle_Null_Remark()
        //{
        //    // Arrange
        //    var caseId = Guid.NewGuid();
        //    var status = MedicalCaseStatus.Active;
        //    var expectedResult = ServiceResult<bool>.Success(true);
        //
        //    _mockBusinessService.Setup(x => x.UpdateStatusAsync(caseId, status, It.IsAny<string>()))
        //        .ReturnsAsync(expectedResult);
        //
        //    // Act
        //    var result = await _medicalCaseService.UpdateStatus(caseId, status, null);
        //
        //    // Assert
        //    result.Should().BeSameAs(expectedResult);
        //    _mockBusinessService.Verify(x => x.UpdateStatusAsync(caseId, status, It.IsAny<string>()), Times.Once);
        //}

        #endregion

        #region 查询方法测试

        [Fact]
        public async Task GetByPatientIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var cases = new List<MedicalCaseDto> { new MedicalCaseDto { PatientId = patientId } };
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(cases);

            _mockQueryService.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetActiveByPatientIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var activeCase = new MedicalCaseDto { PatientId = patientId, CaseStatus = MedicalCaseStatus.Active };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(activeCase);

            _mockQueryService.Setup(x => x.GetActiveByPatientIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetActiveByPatientIdAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetActiveByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "患者名";
            var cases = new List<MedicalCaseDto> { new MedicalCaseDto { PatientName = "患者名" } };
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(cases);

            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        // TODO: MedicalCaseHistoryDto 不存在，需要更新为正确的 DTO
        //[Fact]
        //public void GetHistory_Should_Delegate_To_QueryService()
        //{
        //    // Arrange
        //    var patientId = Guid.NewGuid();
        //    var startDate = DateTime.Now.AddMonths(-1);
        //    var endDate = DateTime.Now;
        //    var history = new List<MedicalCaseHistoryDto>
        //    {
        //        new MedicalCaseHistoryDto { PatientId = patientId }
        //    };
        //    var expectedResult = ServiceResult<List<MedicalCaseHistoryDto>>.Success(history);
        //
        //    _mockQueryService.Setup(x => x.GetHistoryAsync(patientId, startDate, endDate))
        //        .ReturnsAsync(expectedResult);
        //
        //    // Act
        //    var result = _medicalCaseService.GetHistory(patientId, startDate, endDate);
        //
        //    // Assert
        //    result.Should().NotBeNull();
        //    _mockQueryService.Verify(x => x.GetHistoryAsync(patientId, startDate, endDate), Times.Once);
        //}

        [Fact]
        public async Task HasActiveCaseAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockQueryService.Setup(x => x.HasActiveCaseAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.HasActiveCaseAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.HasActiveCaseAsync(patientId), Times.Once);
        }

        #endregion

        #region 批量操作测试

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task BatchUpdateStatusAsync_Should_Handle_Empty_List()
        //        {
            // Arrange
        //            var emptyIds = new List<Guid>();
        //            var status = MedicalCaseStatus.Closed;
//
            // Act
        //            var result = await _medicalCaseService.BatchUpdateStatusAsync(emptyIds, status);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeFalse();
        //            result.ErrorMessage.Should().Contain("病历ID列表不能为空");
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task BatchUpdateStatusAsync_Should_Update_Multiple_Cases()
        //        {
            // Arrange
        //            var caseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        //            var status = MedicalCaseStatus.Closed;
        //            
        //            _mockBusinessService.Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), status, It.IsAny<string>()))
        //                .ReturnsAsync(ServiceResult<bool>.Success(true));
//
            // Act
        //            var result = await _medicalCaseService.BatchUpdateStatusAsync(caseIds, status);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
        //            result.Data.Should().Be(3);
        //            _mockBusinessService.Verify(x => x.UpdateStatusAsync(It.IsAny<Guid>(), status, It.IsAny<string>()), 
        //                Times.Exactly(3));
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task BatchUpdateStatusAsync_Should_Handle_Partial_Failure()
        //        {
            // Arrange
        //            var caseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        //            var status = MedicalCaseStatus.Closed;
        //            var callCount = 0;
//
        //            _mockBusinessService.Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), status, It.IsAny<string>()))
        //                .ReturnsAsync(() => 
        //                {
        //                    callCount++;
        //                    return callCount == 2 
        //                        ? ServiceResult<bool>.Failure("更新失败") 
        //                        : ServiceResult<bool>.Success(true);
        //                });
//
            // Act
        //            var result = await _medicalCaseService.BatchUpdateStatusAsync(caseIds, status);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
        //            result.Data.Should().Be(2); // Only 2 succeeded
        //        }

        #endregion

        #region 咨询取消测试

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task CancelConsultationAsync_Should_Delegate_To_BusinessService()
        //        {
            // Arrange
        //            var caseId = Guid.NewGuid();
        //            var reason = "取消原因";
        //            var expectedResult = ServiceResult<bool>.Success(true);
//
        //            _mockBusinessService.Setup(x => x.CancelConsultationAsync(caseId, reason))
        //                .ReturnsAsync(expectedResult);
//
            // Act
        //            var result = await _medicalCaseService.CancelConsultationAsync(caseId, reason);
//
            // Assert
        //            result.Should().BeSameAs(expectedResult);
        //            _mockBusinessService.Verify(x => x.CancelConsultationAsync(caseId, reason), Times.Once);
        //        }

        #endregion

        #region 统计功能测试

        [Fact]
        public async Task GetStatisticsAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var stats = new MedicalCaseStatisticsDto
            {
                TotalCount = 100,
                InProgressCount = 20,
                CompletedCount = 80
            };
            var expectedResult = ServiceResult<object>.Success(stats);

            _mockQueryService.Setup(x => x.GetStatisticsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetStatisticsAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetStatisticsAsync(), Times.Once);
        }

        [Fact]
        public void GetStatistics_Should_Return_Statistics_Synchronously()
        {
            // Arrange
            var stats = new MedicalCaseStatisticsDto
            {
                TotalCount = 50,
                InProgressCount = 10,
                CompletedCount = 40
            };
            var expectedResult = ServiceResult<object>.Success(stats);

            _mockQueryService.Setup(x => x.GetStatisticsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = _medicalCaseService.GetStatistics(DateTime.Now.AddMonths(-1), DateTime.Now);

            // Assert
            result.Should().NotBeNull();
            _mockQueryService.Verify(x => x.GetStatisticsAsync(), Times.Once);
        }

        #endregion

        #region 打印功能测试

        [Fact]
        public async Task PrintMedicalRecordAsync_Should_Return_PrintData()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = caseId,
                PatientName = "测试患者",
                DoctorName = "测试医生",
                ConsultationDate = DateTime.Now
            };
            var queryResult = ServiceResult<MedicalCaseDto>.Success(medicalCase);

            _mockQueryService.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(queryResult);

            // Act
            var result = await _medicalCaseService.PrintMedicalRecordAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeOfType<byte[]>();
            ((byte[])result.Data!).Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task PrintMedicalRecordAsync_Should_Return_Failure_When_Case_Not_Found()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var queryResult = ServiceResult<MedicalCaseDto>.Failure("病历不存在");

            _mockQueryService.Setup(x => x.GetByIdAsync(caseId))
                .ReturnsAsync(queryResult);

            // Act
            var result = await _medicalCaseService.PrintMedicalRecordAsync(caseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("病历不存在");
        }

        #endregion

        #region 边界值测试

        [Fact]
        public void MedicalCaseService_Should_Implement_IMedicalCaseService()
        {
            _medicalCaseService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IMedicalCaseService>();
        }

        #endregion
    }
}