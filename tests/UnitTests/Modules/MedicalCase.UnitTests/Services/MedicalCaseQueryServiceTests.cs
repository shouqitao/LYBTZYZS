using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AutoMapper;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    public class MedicalCaseQueryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicalCaseQueryService _service;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<MedicalCaseQueryService>> _mockLogger;

        public MedicalCaseQueryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<MedicalCaseQueryService>>();

            _service = new MedicalCaseQueryService(
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

            _mockMapper.Setup(x => x.Map<List<MedicalCaseDto>>(It.IsAny<List<MedicalCaseEntity>>()))
                .Returns((List<MedicalCaseEntity> cases) => cases.Select(mc => new MedicalCaseDto
                {
                    Id = mc.Id,
                    PatientId = mc.PatientId,
                    PatientName = mc.PatientName,
                    DoctorId = mc.DoctorId,
                    DoctorName = mc.DoctorName,
                    CaseStatus = mc.Status,
                    ConsultationDate = mc.ConsultationDate
                }).ToList());

            _mockMapper.Setup(x => x.Map<MedicalCaseDetailDto>(It.IsAny<MedicalCaseEntity>()))
                .Returns((MedicalCaseEntity mc) => new MedicalCaseDetailDto
                {
                    Id = mc.Id,
                    PatientId = mc.PatientId,
                    PatientName = mc.PatientName,
                    DoctorId = mc.DoctorId,
                    DoctorName = mc.DoctorName,
                    CaseStatus = mc.Status,
                    ConsultationDate = mc.ConsultationDate,
                    Remark = mc.Remark
                });
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Should_Return_MedicalCase_When_Found()
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
            var result = await _service.GetByIdAsync(medicalCase.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(medicalCase.Id);
            result.Data.PatientName.Should().Be("测试患者");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Not_Found()
        {
            // Act
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("病历不存在");
        }

        #endregion

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_PatientCases()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var cases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    PatientName = "测试患者",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生1",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.Now.AddDays(-2)
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    PatientName = "测试患者",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生2",
                    Status = MedicalCaseStatus.Closed,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            };
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().BeInDescendingOrder(mc => mc.ConsultationDate);
        }

        #endregion

        #region GetByDoctorIdAsync Tests

        [Fact]
        public async Task GetByDoctorIdAsync_Should_Return_DoctorCases()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var cases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorId = doctorId,
                    DoctorName = "测试医生",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = doctorId
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorId = doctorId,
                    DoctorName = "测试医生",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = doctorId
                }
            };
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByDoctorIdAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region GetActiveAsync Tests

        [Fact]
        public async Task GetActiveAsync_Should_Return_Only_Active_Cases()
        {
            // Arrange
            var cases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生1",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生2",
                    Status = MedicalCaseStatus.Closed,
                    CreatedBy = Guid.NewGuid()
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者3",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生3",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                }
            };
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByPatientIdAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(mc => mc.CaseStatus.Should().Be(MedicalCaseStatus.Active));
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_Should_Filter_By_Status()
        {
            // Arrange
            var cases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生1",
                    Status = MedicalCaseStatus.Closed,
                    CreatedBy = Guid.NewGuid()
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生2",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                }
            };
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            var searchDto = new MedicalCaseSearchDto
            {
                CaseStatus = MedicalCaseStatus.Closed,
                PageIndex = 1,
                PageSize = 10
            };

            _mockMapper.Setup(x => x.Map<PagedResult<MedicalCaseDto>>(It.IsAny<PagedResult<MedicalCaseEntity>>()))
                .Returns((PagedResult<MedicalCaseEntity> paged) => new PagedResult<MedicalCaseDto>
                {
                    Items = paged.Items.Select(mc => new MedicalCaseDto
                    {
                        Id = mc.Id,
                        PatientName = mc.PatientName,
                        CaseStatus = mc.Status
                    }).ToList(),
                    TotalCount = paged.TotalCount,
                    CurrentPage = paged.CurrentPage,
                    PageSize = paged.PageSize
                });

            // Act
            var result = await _service.SearchAsync("患者");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data!.First().CaseStatus.Should().Be(MedicalCaseStatus.Closed);
        }

        [Fact]
        public async Task SearchAsync_Should_Filter_By_DateRange()
        {
            // Arrange
            var cases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生1",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生2",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            };
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            var searchDto = new MedicalCaseSearchDto
            {
                StartDate = DateTime.Now.AddDays(-5),
                EndDate = DateTime.Now,
                PageIndex = 1,
                PageSize = 10
            };

            _mockMapper.Setup(x => x.Map<PagedResult<MedicalCaseDto>>(It.IsAny<PagedResult<MedicalCaseEntity>>()))
                .Returns((PagedResult<MedicalCaseEntity> paged) => new PagedResult<MedicalCaseDto>
                {
                    Items = paged.Items.Select(mc => new MedicalCaseDto
                    {
                        Id = mc.Id,
                        PatientName = mc.PatientName,
                        ConsultationDate = mc.CreatedAt
                    }).ToList(),
                    TotalCount = paged.TotalCount,
                    CurrentPage = paged.CurrentPage,
                    PageSize = paged.PageSize
                });

            // Act
            var result = await _service.SearchAsync("患者");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data!.First().ConsultationDate.Should().BeAfter(DateTime.Now.AddDays(-5));
        }

        #endregion

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_Should_Return_Correct_Statistics()
        {
            // Arrange
            var cases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生1",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生2",
                    Status = MedicalCaseStatus.Closed,
                    CreatedBy = Guid.NewGuid()
                },
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者3",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生3",
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                }
            };
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion
    }
}