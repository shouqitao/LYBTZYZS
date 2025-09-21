using System;
using System.Threading.Tasks;
using FluentAssertions;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using LYBT.Entities.Consultation;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AutoMapper;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    public class MedicalCaseBusinessServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicalCaseBusinessService _service;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<MedicalCaseBusinessService>> _mockLogger;

        public MedicalCaseBusinessServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<MedicalCaseBusinessService>>();

            _service = new MedicalCaseBusinessService(
                _context,
                _mockMapper.Object,
                _mockLogger.Object);

            SetupMockMapper();
        }

        private void SetupMockMapper()
        {
            _mockMapper.Setup(x => x.Map<MedicalCaseDto>(It.IsAny<MedicalCaseEntity>()))
                .Returns((MedicalCaseEntity mc) => new MedicalCaseDto
                {
                    Id = mc.Id,
                    PatientId = mc.PatientId,
                    PatientName = mc.PatientName,
                    DoctorId = mc.DoctorId,
                    DoctorName = mc.DoctorName,
                    CaseStatus = mc.Status,
                    ConsultationDate = mc.ConsultationDate
                });

            _mockMapper.Setup(x => x.Map<MedicalCaseEntity>(It.IsAny<MedicalCaseCreateDto>()))
                .Returns((MedicalCaseCreateDto dto) => new MedicalCaseEntity
                {
                    PatientId = dto.PatientId,
                    PatientName = "测试患者",
                    DoctorId = dto.DoctorId,
                    DoctorName = "测试医生",
                    CreatedBy = Guid.NewGuid()
                });
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_Should_Create_MedicalCase_Successfully()
        {
            // Arrange
            var dto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var createdCase = await _context.MedicalCases
                .FirstOrDefaultAsync(mc => mc.PatientId == dto.PatientId);
            createdCase.Should().NotBeNull();
            createdCase!.Status.Should().Be(MedicalCaseStatus.Active);
        }

        #endregion

        #region Status Machine Tests - Methods not in current implementation

        /* These tests are commented out as the methods are not in the current implementation
        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task CompleteAsync_Should_Change_Status_To_Completed()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.CompleteAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
//
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Status.Should().Be(MedicalCaseStatus.Closed);
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task CompleteAsync_Should_Fail_When_Already_Completed()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Closed,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.CompleteAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeFalse();
        //            result.ErrorMessage.Should().Contain("已完成");
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task SuspendAsync_Should_Change_Status_To_Suspended()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.SuspendAsync(medicalCase.Id, "暂停原因");
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
//
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Status.Should().Be(MedicalCaseStatus.Active);
        //            updatedCase.Remark.Should().Contain("暂停原因");
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task ResumeAsync_Should_Change_Status_From_Suspended_To_Active()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.ResumeAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
//
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Status.Should().Be(MedicalCaseStatus.Active);
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task ArchiveAsync_Should_Change_Status_To_Archived()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Closed,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.ArchiveAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
//
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Status.Should().Be(MedicalCaseStatus.Closed);
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task ArchiveAsync_Should_Fail_When_Not_Completed()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.ArchiveAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeFalse();
        //            result.ErrorMessage.Should().Contain("只能归档已完成");
        //        }
        */

        #endregion

        #region Status Transition Tests

        /* Status transition tests - methods not in current implementation
        [Theory]
        [InlineData(MedicalCaseStatus.Active, MedicalCaseStatus.Closed, true)]
        [InlineData(MedicalCaseStatus.Closed, MedicalCaseStatus.Active, false)]
        public async Task Status_Transitions_Should_Follow_Rules(
            MedicalCaseStatus fromStatus,
            MedicalCaseStatus toStatus,
            bool shouldSucceed)
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                Status = fromStatus,
                CreatedBy = Guid.NewGuid()
            };
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateStatusAsync(medicalCase.Id, toStatus);

            // Assert
            if (shouldSucceed)
            {
                result.IsSuccess.Should().BeTrue();
                var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
                updatedCase!.Status.Should().Be(toStatus);
            }
            else
            {
                result.IsSuccess.Should().BeFalse();
                var unchangedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
                unchangedCase!.Status.Should().Be(fromStatus);
            }
        }
        */

        #endregion

        #region UpdateAsync Tests

        #endregion

        #region State Machine Tests

        // TODO: CompleteAsync 方法尚未实现
        //[Fact]
        //public async Task CompleteAsync_Should_Change_Status_To_Closed()
        //{
        //    // Arrange
        //    var medicalCase = new MedicalCaseEntity
        //    {
        //        Id = Guid.NewGuid(),
        //        PatientId = Guid.NewGuid(),
        //        PatientName = "测试患者",
        //        DoctorId = Guid.NewGuid(),
        //        DoctorName = "测试医生",
        //        Status = MedicalCaseStatus.Active,
        //        CreatedBy = Guid.NewGuid()
        //    };
        //    await _context.MedicalCases.AddAsync(medicalCase);
        //    await _context.SaveChangesAsync();

        //    // Act
        //    var result = await _service.CompleteAsync(medicalCase.Id);

        //    // Assert
        //    result.Should().NotBeNull();
        //    result.IsSuccess.Should().BeTrue();
        //
        //    var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //    updatedCase!.Status.Should().Be(MedicalCaseStatus.Closed);
        //}

        // TODO: CompleteAsync 方法尚未实现
        //[Fact]
        //public async Task CompleteAsync_Should_Fail_When_Already_Closed()
        //{
        //    // Arrange
        //    var medicalCase = new MedicalCaseEntity
        //    {
        //        Id = Guid.NewGuid(),
        //        PatientId = Guid.NewGuid(),
        //        PatientName = "测试患者",
        //        DoctorId = Guid.NewGuid(),
        //        DoctorName = "测试医生",
        //        Status = MedicalCaseStatus.Closed,
        //        CreatedBy = Guid.NewGuid()
        //    };
        //    await _context.MedicalCases.AddAsync(medicalCase);
        //    await _context.SaveChangesAsync();

        //    // Act
        //    var result = await _service.CompleteAsync(medicalCase.Id);

        //    // Assert
        //    result.Should().NotBeNull();
        //    result.IsSuccess.Should().BeFalse();
        //    result.ErrorMessage.Should().Contain("已完成");
        //}

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task SuspendAsync_Should_Add_Suspension_Reason()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.SuspendAsync(medicalCase.Id, "暂停原因");
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
        //            
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Remark.Should().Contain("暂停原因");
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task ResumeAsync_Should_Resume_From_Suspension()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                Remark = "暂停中",
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.ResumeAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
        //            
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Status.Should().Be(MedicalCaseStatus.Active);
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task ArchiveAsync_Should_Archive_Closed_Case()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Closed,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.ArchiveAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
        //        }

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task ArchiveAsync_Should_Fail_When_Not_Closed()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.ArchiveAsync(medicalCase.Id);
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeFalse();
        //            result.ErrorMessage.Should().Contain("只能归档已完成");
        //        }

        // TODO: UpdateStatusAsync 方法尚未实现
        //[Theory]
        //[InlineData(MedicalCaseStatus.Active, MedicalCaseStatus.Closed, true)]
        //[InlineData(MedicalCaseStatus.Closed, MedicalCaseStatus.Active, false)]
        //public async Task UpdateStatusAsync_Should_Follow_State_Rules(
        //    MedicalCaseStatus fromStatus,
        //    MedicalCaseStatus toStatus,
        //    bool shouldSucceed)
        //{
        //    // Arrange
        //    var medicalCase = new MedicalCaseEntity
        //    {
        //        Id = Guid.NewGuid(),
        //        PatientId = Guid.NewGuid(),
        //        PatientName = "测试患者",
        //        DoctorId = Guid.NewGuid(),
        //        DoctorName = "测试医生",
        //        Status = fromStatus,
        //        CreatedBy = Guid.NewGuid()
        //    };
        //    await _context.MedicalCases.AddAsync(medicalCase);
        //    await _context.SaveChangesAsync();
        //
        //    // Act
        //    var result = await _service.UpdateStatusAsync(medicalCase.Id, toStatus, "状态变更");
        //
        //    // Assert
        //    if (shouldSucceed)
        //    {
        //        result.IsSuccess.Should().BeTrue();
        //        var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //        updatedCase!.Status.Should().Be(toStatus);
        //    }
        //    else
        //    {
        //        result.IsSuccess.Should().BeFalse();
        //        var unchangedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //        unchangedCase!.Status.Should().Be(fromStatus);
        //    }
        //}

        // TODO: 以下方法在服务中尚未实现
        //        [Fact]
        //        public async Task CancelConsultationAsync_Should_Update_Status_And_Reason()
        //        {
            // Arrange
        //            var medicalCase = new MedicalCaseEntity
        //            {
        //                Id = Guid.NewGuid(),
        //                PatientId = Guid.NewGuid(),
        //                PatientName = "测试患者",
        //                DoctorId = Guid.NewGuid(),
        //                DoctorName = "测试医生",
        //                Status = MedicalCaseStatus.Active,
        //                CreatedBy = Guid.NewGuid()
        //            };
        //            await _context.MedicalCases.AddAsync(medicalCase);
        //            await _context.SaveChangesAsync();
//
            // Act
        //            var result = await _service.CancelConsultationAsync(medicalCase.Id, "患者取消");
//
            // Assert
        //            result.Should().NotBeNull();
        //            result.IsSuccess.Should().BeTrue();
        //            
        //            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
        //            updatedCase!.Remark.Should().Contain("患者取消");
        //        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_Should_Fail_When_Active_Case_Exists()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            
            // Add existing active case
            var existingCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid()
            };
            await _context.MedicalCases.AddAsync(existingCase);
            await _context.SaveChangesAsync();

            var dto = new MedicalCaseCreateDto
            {
                PatientId = patientId,
                DoctorId = Guid.NewGuid()
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("已有活动病历");
        }

        [Fact]
        public async Task UpdateAsync_Should_Validate_Required_Fields()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid()
            };
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            var dto = new MedicalCaseUpdateDto
            {
                Remark = null // Empty update
            };

            // Act
            var result = await _service.UpdateAsync(medicalCase.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue(); // Should succeed even with minimal update
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_Should_Update_MedicalCase_Successfully()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "原患者名",
                DoctorId = Guid.NewGuid(),
                DoctorName = "原医生名",
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid()
            };
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = "更新备注"
            };

            // Act
            var result = await _service.UpdateAsync(medicalCase.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            var updatedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
            updatedCase!.Remark.Should().Be("更新备注");
        }

        [Fact]
        public async Task UpdateAsync_Should_Fail_When_MedicalCase_Not_Found()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = "更新备注"
            };

            // Act
            var result = await _service.UpdateAsync(Guid.NewGuid(), updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("病历不存在");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_Should_Delete_MedicalCase_Successfully()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid()
            };
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(medicalCase.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            var deletedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
            deletedCase.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_Should_Fail_When_Status_Not_Active()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                Status = MedicalCaseStatus.Closed,
                CreatedBy = Guid.NewGuid()
            };
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(medicalCase.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("只能删除活动状态");
        }

        #endregion
    }
}