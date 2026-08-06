using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Data;
using LYBT.Module.Reports.Infrastructure;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// ReportRepository 单元测试 — 趋势/医生绩效/药材排行/患者流量查询逻辑（EF InMemory 真实实现零 mock）。
/// </summary>
public class ReportRepositoryTests : IDisposable
{
    private static readonly DateTime Day1 = new(2026, 8, 1);
    private static readonly DateTime Day2 = new(2026, 8, 2);
    private static readonly DateTime Day3 = new(2026, 8, 3);

    private readonly AppDbContext _context;
    private readonly ReportRepository _sut;

    public ReportRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new ReportRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static MedicalCase CreateCase(string doctorName, MedicalCaseStatus status = MedicalCaseStatus.Completed, bool isDeleted = false)
    {
        return new MedicalCase
        {
            PatientId = Guid.NewGuid(),
            PatientName = "患者",
            UserId = Guid.NewGuid(),
            DoctorName = doctorName,
            CaseStatus = status,
            CreatedBy = Guid.NewGuid(),
            IsDeleted = isDeleted
        };
    }

    private static Prescription CreatePrescription(Guid medicalCaseId, params PrescriptionItem[] items)
    {
        var prescription = new Prescription { Id = Guid.NewGuid(), MedicalCaseId = medicalCaseId, CreatedBy = Guid.NewGuid() };
        foreach (var item in items)
            item.PrescriptionId = prescription.Id;
        prescription.Items = items.ToList();
        return prescription;
    }

    private static PrescriptionItem CreateItem(string herbName, int dosage, decimal unitPrice)
    {
        return new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            HerbId = Guid.NewGuid(),
            HerbName = herbName,
            Dosage = dosage,
            UnitPrice = unitPrice
        };
    }

    private static Registration CreateRegistration(Guid medicalCaseId, decimal fee, string doctorName = "张医生", bool isDeleted = false)
    {
        return new Registration
        {
            PatientId = Guid.NewGuid(),
            PatientName = "患者",
            DoctorId = Guid.NewGuid(),
            DoctorName = doctorName,
            MedicalCaseId = medicalCaseId,
            Source = RegistrationSource.Receptionist,
            Status = RegistrationStatus.Completed,
            RegistrationFee = fee,
            CreatedBy = Guid.NewGuid(),
            IsDeleted = isDeleted
        };
    }

    // AppDbContext 的 SaveChangesAsync 会在 Added 状态强制写入 CreatedAt，需先保存再改日期二次保存
    private async Task AddCaseAsync(MedicalCase medicalCase, DateTime createdAt)
    {
        _context.MedicalCases.Add(medicalCase);
        await _context.SaveChangesAsync();
        medicalCase.CreatedAt = createdAt;
        await _context.SaveChangesAsync();
    }

    private async Task AddRegistrationAsync(Registration registration, DateTime createdAt)
    {
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();
        registration.CreatedAt = createdAt;
        await _context.SaveChangesAsync();
    }

    private async Task AddAsync(params object[] entities)
    {
        _context.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRegistrationFeeByDayAsync_GroupsFeesByDayAndFiltersRange()
    {
        var reg1 = CreateRegistration(Guid.NewGuid(), 50);
        var reg2 = CreateRegistration(Guid.NewGuid(), 30);
        var reg3 = CreateRegistration(Guid.NewGuid(), 20);
        var reg4 = CreateRegistration(Guid.NewGuid(), 99, isDeleted: true);
        await AddRegistrationAsync(reg1, Day1);
        await AddRegistrationAsync(reg2, Day2);
        await AddRegistrationAsync(reg3, Day2);
        await AddRegistrationAsync(reg4, Day1);

        var result = await _sut.GetRegistrationFeeByDayAsync(Day1, Day2, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(x => x.Date == Day1).Value.Should().Be(50);
        result.Single(x => x.Date == Day2).Value.Should().Be(50);
    }

    [Fact]
    public async Task GetMedicineFeeByDayAsync_SumsOnlyCompletedCaseItemsByDay()
    {
        var completed1 = CreateCase("张医生");
        var completed2 = CreateCase("李医生");
        var active = CreateCase("王医生", MedicalCaseStatus.Active);
        await AddCaseAsync(completed1, Day1);
        await AddCaseAsync(completed2, Day2);
        await AddCaseAsync(active, Day2);

        var p1 = CreatePrescription(completed1.Id, CreateItem("黄芪", 10, 5m), CreateItem("甘草", 20, 10m)); // 50 + 200
        var p2 = CreatePrescription(completed2.Id, CreateItem("当归", 10, 3m)); // 30
        var p3 = CreatePrescription(active.Id, CreateItem("人参", 10, 100m)); // 应排除
        await AddAsync(p1, p2, p3);

        var result = await _sut.GetMedicineFeeByDayAsync(Day1, Day2, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(x => x.Date == Day1).Value.Should().Be(250);
        result.Single(x => x.Date == Day2).Value.Should().Be(30);
    }

    [Fact]
    public async Task GetConsultationCountByDayAsync_CountsCompletedCasesByDay()
    {
        await AddCaseAsync(CreateCase("张医生"), Day1);
        await AddCaseAsync(CreateCase("张医生"), Day1);
        await AddCaseAsync(CreateCase("李医生"), Day2);
        await AddCaseAsync(CreateCase("王医生", MedicalCaseStatus.Active), Day2); // 未完成不计
        await AddCaseAsync(CreateCase("赵医生", isDeleted: true), Day3); // 已删除不计

        var result = await _sut.GetConsultationCountByDayAsync(Day1, Day3, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(x => x.Date == Day1).Count.Should().Be(2);
        result.Single(x => x.Date == Day2).Count.Should().Be(1);
    }

    [Fact]
    public async Task GetDoctorPerformanceAsync_CombinesCountsFeesAndPrescriptions()
    {
        var caseA1 = CreateCase("张医生");
        var caseA2 = CreateCase("张医生");
        var caseB = CreateCase("李医生");
        var caseC = CreateCase("王医生");
        await AddCaseAsync(caseA1, Day1);
        await AddCaseAsync(caseA2, Day2);
        await AddCaseAsync(caseB, Day1);
        await AddCaseAsync(caseC, Day2);

        await AddRegistrationAsync(CreateRegistration(caseA1.Id, 50), Day1);
        await AddRegistrationAsync(CreateRegistration(caseB.Id, 20), Day1);

        var pA1 = CreatePrescription(caseA1.Id, CreateItem("黄芪", 10, 10m), CreateItem("甘草", 5, 10m)); // 100 + 50
        var pA2 = CreatePrescription(caseA2.Id, CreateItem("当归", 10, 3m)); // 30
        var pB = CreatePrescription(caseB.Id, CreateItem("人参", 5, 5m)); // 25
        await AddAsync(pA1, pA2, pB);

        var result = await _sut.GetDoctorPerformanceAsync(Day1, Day2, CancellationToken.None);

        result.Should().HaveCount(3);

        var doctorA = result.Single(x => x.DoctorName == "张医生");
        doctorA.ConsultationCount.Should().Be(2);
        doctorA.RegistrationFeeTotal.Should().Be(50);
        doctorA.MedicineFeeTotal.Should().Be(180);
        doctorA.PrescriptionCount.Should().Be(2);

        var doctorB = result.Single(x => x.DoctorName == "李医生");
        doctorB.ConsultationCount.Should().Be(1);
        doctorB.RegistrationFeeTotal.Should().Be(20);
        doctorB.MedicineFeeTotal.Should().Be(25);
        doctorB.PrescriptionCount.Should().Be(1);

        var doctorC = result.Single(x => x.DoctorName == "王医生");
        doctorC.ConsultationCount.Should().Be(1);
        doctorC.RegistrationFeeTotal.Should().Be(0);
        doctorC.MedicineFeeTotal.Should().Be(0);
        doctorC.PrescriptionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHerbRankingAsync_ReturnsTopNByUsageCount()
    {
        var case1 = CreateCase("张医生");
        var case2 = CreateCase("张医生");
        var case3 = CreateCase("李医生");
        await AddCaseAsync(case1, Day1);
        await AddCaseAsync(case2, Day1);
        await AddCaseAsync(case3, Day2);

        var p1 = CreatePrescription(case1.Id, CreateItem("甘草", 10, 1m), CreateItem("黄芪", 10, 1m));
        var p2 = CreatePrescription(case2.Id, CreateItem("甘草", 20, 1m), CreateItem("甘草", 30, 1m));
        var p3 = CreatePrescription(case3.Id, CreateItem("黄芪", 10, 1m));
        await AddAsync(p1, p2, p3);

        var result = await _sut.GetHerbRankingAsync(Day1, Day2, 2, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].HerbName.Should().Be("甘草");
        result[0].UsageCount.Should().Be(3);
        result[0].TotalDosage.Should().Be(60);
        result[1].HerbName.Should().Be("黄芪");
        result[1].UsageCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPatientFlowByDayAsync_ClassifiesNewAndReturning()
    {
        var patient1 = Guid.NewGuid();
        var patient2 = Guid.NewGuid();
        var patient3 = Guid.NewGuid();

        // P1: 首次就诊 Day1，之后 Day3 再次就诊
        var p1First = CreateCase("张医生");
        p1First.PatientId = patient1;
        var p1Again = CreateCase("张医生");
        p1Again.PatientId = patient1;
        await AddCaseAsync(p1First, Day1);
        await AddCaseAsync(p1Again, Day3);

        // P2: 区间之前已有就诊（Day1 前一天），Day2 再来
        var p2Before = CreateCase("张医生");
        p2Before.PatientId = patient2;
        var p2Visit = CreateCase("张医生");
        p2Visit.PatientId = patient2;
        await AddCaseAsync(p2Before, Day1.AddDays(-1));
        await AddCaseAsync(p2Visit, Day2);

        // P3: 唯一一次就诊 Day2
        var p3Only = CreateCase("李医生");
        p3Only.PatientId = patient3;
        await AddCaseAsync(p3Only, Day2);

        var result = await _sut.GetPatientFlowByDayAsync(Day1, Day3, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Single(x => x.Date == Day1).NewPatients.Should().Be(1);
        result.Single(x => x.Date == Day1).ReturningPatients.Should().Be(0);
        result.Single(x => x.Date == Day2).NewPatients.Should().Be(1);
        result.Single(x => x.Date == Day2).ReturningPatients.Should().Be(1);
        result.Single(x => x.Date == Day3).NewPatients.Should().Be(0);
        result.Single(x => x.Date == Day3).ReturningPatients.Should().Be(1);
    }
}
