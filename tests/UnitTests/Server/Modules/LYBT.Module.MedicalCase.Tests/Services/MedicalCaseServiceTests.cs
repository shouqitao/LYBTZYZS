using AutoMapper;
using FluentAssertions;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Consultation;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.UnitTests.Core.Services
{
    /// <summary>
    /// MedicalCaseService服务层单元测试
    /// </summary>
    public class MedicalCaseServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly MedicalCaseService _service;

        public MedicalCaseServiceTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _loggerMock = new Mock<ILogger<MedicalCaseService>>();
            _mapperMock = new Mock<IMapper>();

            // 创建服务实例
            var repository = new MedicalCaseRepository(_context);
            _service = new MedicalCaseService(
                repository,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region CreateWithDetailsAsync Tests

        [Fact]
        public async Task CreateWithDetailsAsync_ShouldCreateCompleteAggregate()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            var caseDto = new MedicalCaseCreateDto
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Remark = "测试医疗案例"
            };

            var consultationDto = new ConsultationCreateDto
            {
                ChiefComplaint = "头痛发热3天",
                PresentIllness = "患者3天前开始出现头痛，伴有发热",
                Diagnosis = "风寒感冒",
                TreatmentPlan = "疏风散寒，解表"
            };

            var prescriptionDto = new PrescriptionCreateDto
            {
                Type = "中药饮片",
                DosageCount = 7,
                DailyDose = 1,
                Usage = "水煎服，每日一剂",
                PayableAmount = 168.50m
            };

            // Act
            var result = await _service.CreateWithDetailsAsync(caseDto, consultationDto, prescriptionDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);
            result.PatientId.Should().Be(patientId);
            result.DoctorId.Should().Be(doctorId);
            result.Status.Should().Be(MedicalCaseStatus.Active);

            // 验证数据库中的数据
            var savedCase = await _context.MedicalCases
                .Include(m => m.Consultation)
                .FirstOrDefaultAsync(m => m.Id == result.Id);

            savedCase.Should().NotBeNull();
            savedCase!.Consultation.Should().NotBeNull();
            savedCase.Consultation!.ChiefComplaint.Should().Be("头痛发热3天");
        }

        [Fact]
        public async Task CreateWithDetailsAsync_ShouldHandleNullPrescription()
        {
            // Arrange
            var caseDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            var consultationDto = new ConsultationCreateDto
            {
                ChiefComplaint = "测试主诉",
                Diagnosis = "测试诊断"
            };

            // Act
            var result = await _service.CreateWithDetailsAsync(caseDto, consultationDto, null);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);

            var savedCase = await _context.MedicalCases
                .Include(m => m.Consultation)
                .FirstOrDefaultAsync(m => m.Id == result.Id);

            savedCase!.Consultation.Should().NotBeNull();
            savedCase.Consultation!.PrescriptionId.Should().BeNull("没有创建处方");
        }

        [Fact]
        public async Task CreateWithDetailsAsync_ShouldRollbackOnError()
        {
            // Arrange
            var caseDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.Empty, // 无效的PatientId
                DoctorId = Guid.NewGuid()
            };

            var consultationDto = new ConsultationCreateDto();

            // Act
            Func<Task> act = async () => await _service.CreateWithDetailsAsync(caseDto, consultationDto, null);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            // 验证数据库中没有数据
            var count = await _context.MedicalCases.CountAsync();
            count.Should().Be(0, "事务应该回滚");
        }

        #endregion

        #region GetByIdWithDetailsAsync Tests

        [Fact]
        public async Task GetByIdWithDetailsAsync_ShouldReturnCompleteAggregate()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC20250927001",
                Status = MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉",
                Diagnosis = "测试诊断",
                Status = ConsultationStatus.Completed
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdWithDetailsAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(medicalCaseId);
            result.ChiefComplaint.Should().Be("测试主诉");
            result.Diagnosis.Should().Be("测试诊断");
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.GetByIdWithDetailsAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Permission Validation Tests

        [Fact]
        public async Task UpdateAsync_ShouldValidateEditPermission()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = doctorId,
                CreatedAt = DateTime.UtcNow.AddDays(-2), // 2天前创建，不能编辑
                Status = MedicalCaseStatus.Active
            };

            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = "尝试更新备注"
            };

            // Act
            Func<Task> act = async () => await _service.UpdateAsync(
                medicalCaseId, 
                updateDto, 
                false, // 非管理员
                doctorId.ToString() // 当前医生
            );

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*没有权限编辑*");
        }

        #endregion

        #region Concurrent Update Tests

        [Fact]
        public async Task UpdateAsync_ShouldHandleConcurrentUpdates()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = MedicalCaseStatus.Active,
                RowVersion = new byte[] { 0, 0, 0, 1 }
            };

            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // 模拟并发更新
            var updateDto1 = new MedicalCaseUpdateDto { Remark = "更新1" };
            var updateDto2 = new MedicalCaseUpdateDto { Remark = "更新2" };

            // Act & Assert
            // 第一个更新应该成功
            var result1 = await _service.UpdateAsync(medicalCaseId, updateDto1, true, null);
            result1.Should().NotBeNull();

            // 第二个更新应该因为版本冲突而失败
            Func<Task> act = async () => await _service.UpdateAsync(medicalCaseId, updateDto2, true, null);
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}