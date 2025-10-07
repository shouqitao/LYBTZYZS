using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Repositories;

/// <summary>
/// ConsultationRepository 单元测试
/// Issue #864 - Phase 2.4: Consultation 模块测试
/// </summary>
public class ConsultationRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ConsultationRepository _sut;

    public ConsultationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new ConsultationRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region 基本查询测试

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithExistingId_ReturnsConsultation()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = consultationId,  // 共享主键
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "测试患者",
            DoctorName = "测试医生"
        };

        var consultation = new LYBT.Entities.Consultation.Consultation
        {
            Id = consultationId,
            ChiefComplaint = "头痛发热",
            TCMDiagnosis = "外感风寒",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase
        };

        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdWithDetailsAsync(consultationId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(consultationId);
        result.ChiefComplaint.Should().Be("头痛发热");
        result.MedicalCase.Should().NotBeNull();
        result.MedicalCase.PatientName.Should().Be("测试患者");
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetByIdWithDetailsAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMedicalCaseIdAsync_WithExistingId_ReturnsConsultation()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = medicalCaseId,
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者A",
            DoctorName = "医生A"
        };

        var consultation = new LYBT.Entities.Consultation.Consultation
        {
            Id = medicalCaseId,  // 共享主键
            ChiefComplaint = "咳嗽",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase
        };

        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByMedicalCaseIdAsync(medicalCaseId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(medicalCaseId);
        result.ChiefComplaint.Should().Be("咳嗽");
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithExistingPatientId_ReturnsConsultations()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        var medicalCase1 = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者B",
            DoctorName = "医生B"
        };

        var consultation1 = new LYBT.Entities.Consultation.Consultation
        {
            Id = medicalCase1.Id,
            ChiefComplaint = "主诉1",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase1,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var medicalCase2 = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者B",
            DoctorName = "医生B"
        };

        var consultation2 = new LYBT.Entities.Consultation.Consultation
        {
            Id = medicalCase2.Id,
            ChiefComplaint = "主诉2",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase2,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.MedicalCases.AddRange(medicalCase1, medicalCase2);
        _context.Consultations.AddRange(consultation1, consultation2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByPatientIdAsync(patientId);

        // Assert
        result.Should().HaveCount(2);
        result[0].ChiefComplaint.Should().Be("主诉2"); // 最新的在前
        result[1].ChiefComplaint.Should().Be("主诉1");
    }

    #endregion

    #region 分页查询测试

    [Fact]
    public async Task GetPagedWithDetailsAsync_WithDefaultParameters_ReturnsPagedResult()
    {
        // Arrange
        var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者C",
            DoctorName = "医生C"
        };

        var consultation = new LYBT.Entities.Consultation.Consultation
        {
            Id = medicalCase.Id,
            ChiefComplaint = "感冒",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase
        };

        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedWithDetailsAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_WithKeyword_ReturnsMatchingRecords()
    {
        // Arrange
        var medicalCase1 = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生"
        };

        var consultation1 = new LYBT.Entities.Consultation.Consultation
        {
            Id = medicalCase1.Id,
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase1
        };

        var medicalCase2 = new LYBT.Entities.MedicalCase.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "李四",
            DoctorName = "王医生"
        };

        var consultation2 = new LYBT.Entities.Consultation.Consultation
        {
            Id = medicalCase2.Id,
            ChiefComplaint = "咳嗽",
            TCMDiagnosis = "痰热咳嗽",
            CreatedBy = Guid.NewGuid(),
            MedicalCase = medicalCase2
        };

        _context.MedicalCases.AddRange(medicalCase1, medicalCase2);
        _context.Consultations.AddRange(consultation1, consultation2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedWithDetailsAsync(1, 20, "头痛");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].ChiefComplaint.Should().Be("头痛");
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CreatedBy = Guid.NewGuid(),
                PatientName = $"患者{i}",
                DoctorName = "医生"
            };

            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = medicalCase.Id,
                ChiefComplaint = $"主诉{i}",
                CreatedBy = Guid.NewGuid(),
                MedicalCase = medicalCase
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedWithDetailsAsync(2, 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.CurrentPage.Should().Be(2);
    }

    #endregion
}
