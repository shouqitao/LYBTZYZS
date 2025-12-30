using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultations.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Consultations.Tests.Repositories;

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
        _sut = new ConsultationRepository(_context, NullLogger<ConsultationRepository>.Instance);
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
        var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = consultationId,  // 共享主键
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "测试患者",
            DoctorName = "测试医生"
        };

        // OpenSpec: refactor-server-ddd-aggregates - Consultation不再有MedicalCase导航属性
        // 使用共享主键关联，先添加MedicalCase，再添加Consultation
        var consultation = new LYBT.Entities.Consultations.Consultation
        {
            Id = consultationId,  // 共享主键
            TcmDiagnosis = "外感风寒",
            CreatedBy = Guid.NewGuid()
        };

        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdWithDetailsAsync(consultationId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(consultationId);
        result.TcmDiagnosis.Should().Be("外感风寒");
        // MedicalCase信息现在通过GetMedicalCaseInfoAsync方法单独获取
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
        var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = medicalCaseId,
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者A",
            DoctorName = "医生A"
        };

        // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
        var consultation = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCaseId,  // 共享主键
            TcmDiagnosis = "咳嗽",
            CreatedBy = Guid.NewGuid()
        };

        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByMedicalCaseIdAsync(medicalCaseId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(medicalCaseId);
        result.TcmDiagnosis.Should().Be("咳嗽");
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithExistingPatientId_ReturnsConsultations()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        var medicalCase1 = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者B",
            DoctorName = "医生B"
        };

        // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
        var consultation1 = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCase1.Id,  // 共享主键
            TcmDiagnosis = "诊断1",
            CreatedBy = Guid.NewGuid()
        };

        // 稍后插入以确保CreatedAt更晚
        await _context.MedicalCases.AddAsync(medicalCase1);
        await _context.Consultations.AddAsync(consultation1);
        await _context.SaveChangesAsync();

        var medicalCase2 = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者B",
            DoctorName = "医生B"
        };

        // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
        var consultation2 = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCase2.Id,  // 共享主键
            TcmDiagnosis = "诊断2",
            CreatedBy = Guid.NewGuid()
        };

        // 后插入，CreatedAt会更晚
        await _context.MedicalCases.AddAsync(medicalCase2);
        await _context.Consultations.AddAsync(consultation2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByPatientIdAsync(patientId);

        // Assert
        result.Should().HaveCount(2);
        result[0].TcmDiagnosis.Should().Be("诊断2"); // 最新的在前（后插入的）
        result[1].TcmDiagnosis.Should().Be("诊断1");
    }

    #endregion

    #region 分页查询测试

    [Fact]
    public async Task GetPagedWithDetailsAsync_WithDefaultParameters_ReturnsPagedResult()
    {
        // Arrange
        var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "患者C",
            DoctorName = "医生C"
        };

        // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
        var consultation = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCase.Id,  // 共享主键
            TcmDiagnosis = "感冒",
            CreatedBy = Guid.NewGuid()
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
        var medicalCase1 = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生"
        };

        // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
        var consultation1 = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCase1.Id,  // 共享主键
            TcmDiagnosis = "风寒感冒",
            CreatedBy = Guid.NewGuid()
        };

        var medicalCase2 = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            PatientName = "李四",
            DoctorName = "王医生"
        };

        // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
        var consultation2 = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCase2.Id,  // 共享主键
            TcmDiagnosis = "痰热咳嗽",
            CreatedBy = Guid.NewGuid()
        };

        _context.MedicalCases.AddRange(medicalCase1, medicalCase2);
        _context.Consultations.AddRange(consultation1, consultation2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedWithDetailsAsync(1, 20, "风寒");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].TcmDiagnosis.Should().Be("风寒感冒");
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                CreatedBy = Guid.NewGuid(),
                PatientName = $"患者{i}",
                DoctorName = "医生"
            };

            // OpenSpec: refactor-server-ddd-aggregates - 使用共享主键关联
            var consultation = new LYBT.Entities.Consultations.Consultation
            {
                Id = medicalCase.Id,  // 共享主键
                TcmDiagnosis = $"诊断{i}",
                CreatedBy = Guid.NewGuid()
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
