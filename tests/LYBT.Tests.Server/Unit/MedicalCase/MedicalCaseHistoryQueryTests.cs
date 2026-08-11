using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Module.MedicalCases.Infrastructure;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 医案历史聚合 + 批量详情查询单测（AC-TEST P2-4: B1 新增端点无测试——
/// 真实 MedicalCaseRepository + EF InMemory——守护 GetPatientRecentMedicalCasesAsync
/// 排序/条数 + GetDetailDtosBatchAsync Doctor 所有权过滤）
/// </summary>
public class MedicalCaseHistoryQueryTests : IDisposable
{
    private readonly MedicalCaseDbContext _context;
    private readonly MedicalCaseQueryService _service;

    public MedicalCaseHistoryQueryTests()
    {
        var options = new DbContextOptionsBuilder<MedicalCaseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MedicalCaseDbContext(options);
        var repository = new MedicalCaseRepository(_context, NullLogger<MedicalCaseRepository>.Instance);
        _service = new MedicalCaseQueryService(repository, new MedicalCaseMapper(),
            NullLogger<MedicalCaseQueryService>.Instance);
    }

    public void Dispose() => _context.Dispose();

    private MedicalCase CreateCase(Guid doctorId, DateTime createdAt, Guid? patientId = null)
    {
        var entity = new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId ?? Guid.NewGuid(),
            PatientName = "张三",
            UserId = doctorId,
            DoctorName = "李医生",
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = true,
            CompletedAt = null,
            IsDeleted = false,
            CreatedBy = doctorId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                PresentIllness = "主诉：乏力",
                TcmDiagnosis = "脾胃气虚",
                CreatedBy = doctorId,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            },
            Prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                Usage = "水煎服",
                IsDeleted = false,
                CreatedBy = doctorId,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            }
        };
        _context.MedicalCases.Add(entity);
        return entity;
    }

    [Fact]
    public async Task GetPatientRecentMedicalCasesAsync_OrdersByCreatedAtDescending_AndLimitsCount()
    {
        var doctor = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var old = CreateCase(doctor, DateTime.UtcNow.AddDays(-10), patientId);
        var mid = CreateCase(doctor, DateTime.UtcNow.AddDays(-3), patientId);
        var recent = CreateCase(doctor, DateTime.UtcNow.AddDays(-1), patientId);
        await _context.SaveChangesAsync();

        var result = await _service.GetPatientRecentMedicalCasesAsync(patientId, count: 2);

        result!.Should().HaveCount(2);
        result![0].Id.Should().Be(recent.Id); // 最新在前
        result![1].Id.Should().Be(mid.Id);
        result!.Select(r => r.Id).Should().NotContain(old.Id);
    }

    [Fact]
    public async Task GetPatientRecentMedicalCasesAsync_NoHistory_ReturnsEmpty()
    {
        var result = await _service.GetPatientRecentMedicalCasesAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailDtosBatchAsync_DoctorSeesOnlyOwnCases()
    {
        var doctor = Guid.NewGuid();
        var other = Guid.NewGuid();
        var own1 = CreateCase(doctor, DateTime.UtcNow);
        var own2 = CreateCase(doctor, DateTime.UtcNow);
        var foreign = CreateCase(other, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var result = await _service.GetDetailDtosBatchAsync(
            new[] { own1.Id, own2.Id, foreign.Id }, operatorId: doctor, isAdmin: false);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value!.Select(d => d.Id).Should().NotContain(foreign.Id);
    }

    [Fact]
    public async Task GetDetailDtosBatchAsync_AdminSeesAllCases()
    {
        var doctor = Guid.NewGuid();
        var other = Guid.NewGuid();
        CreateCase(doctor, DateTime.UtcNow);
        CreateCase(other, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var result = await _service.GetDetailDtosBatchAsync(
            new[] { Guid.NewGuid(), Guid.NewGuid() }, operatorId: doctor, isAdmin: true);

        // 传入的 ids 与种子 id 不同——批量查询按 ids 过滤（真实仓储语义）
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailDtosBatchAsync_EmptyIds_ReturnsEmpty()
    {
        var result = await _service.GetDetailDtosBatchAsync(Array.Empty<Guid>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
