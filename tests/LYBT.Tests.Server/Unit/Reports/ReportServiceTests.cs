using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Data;
using LYBT.Module.Reports.Infrastructure;
using LYBT.Module.Reports.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// ReportService 单元测试 — 趋势组装/时间桶汇总/平均处方金额（EF InMemory 真实实现零 mock）。
/// </summary>
public class ReportServiceTests : IDisposable
{
    private static readonly DateTime Day1 = new(2026, 8, 1);
    private static readonly DateTime Day2 = new(2026, 8, 2);
    private static readonly DateTime Day3 = new(2026, 8, 3);

    private readonly AppDbContext _context;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new ReportService(new ReportRepository(_context));
    }

    public void Dispose() => _context.Dispose();

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

    private static MedicalCase CreateCase(string doctorName, MedicalCaseStatus status = MedicalCaseStatus.Completed)
    {
        return new MedicalCase
        {
            PatientId = Guid.NewGuid(),
            PatientName = "患者",
            UserId = Guid.NewGuid(),
            DoctorName = doctorName,
            CaseStatus = status,
            CreatedBy = Guid.NewGuid()
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

    private static Registration CreateRegistration(Guid medicalCaseId, decimal fee)
    {
        return new Registration
        {
            PatientId = Guid.NewGuid(),
            PatientName = "患者",
            DoctorId = Guid.NewGuid(),
            DoctorName = "张医生",
            MedicalCaseId = medicalCaseId,
            Source = RegistrationSource.Receptionist,
            Status = RegistrationStatus.Completed,
            RegistrationFee = fee,
            CreatedBy = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task GetIncomeTrendAsync_DayGranularity_AssemblesLabelsAndTotals()
    {
        await AddRegistrationAsync(CreateRegistration(Guid.NewGuid(), 50), Day1);
        await AddRegistrationAsync(CreateRegistration(Guid.NewGuid(), 30), Day2);

        var case1 = CreateCase("张医生");
        var case2 = CreateCase("李医生");
        await AddCaseAsync(case1, Day1);
        await AddCaseAsync(case2, Day2);
        await AddAsync(
            CreatePrescription(case1.Id, CreateItem("黄芪", 10, 25m)), // 250
            CreatePrescription(case2.Id, CreateItem("甘草", 10, 10m))); // 100

        var result = await _sut.GetIncomeTrendAsync(Day1, Day2, ReportGranularity.Day, CancellationToken.None);

        result.Labels.Should().Equal("08-01", "08-02");
        result.Registration.Should().Equal(50m, 30m);
        result.Medicine.Should().Equal(250m, 100m);
        result.Total.Should().Equal(300m, 130m);
    }

    [Fact]
    public async Task GetIncomeTrendAsync_WeekGranularity_RollsUpWholeWeek()
    {
        // 2026-08-03 是周一，区间 [08-03, 08-09] 恰好一个周桶
        var monday = new DateTime(2026, 8, 3);
        var sunday = new DateTime(2026, 8, 9);
        await AddRegistrationAsync(CreateRegistration(Guid.NewGuid(), 50), monday);
        await AddRegistrationAsync(CreateRegistration(Guid.NewGuid(), 20), new DateTime(2026, 8, 7));

        var case1 = CreateCase("张医生");
        await AddCaseAsync(case1, monday);
        await AddAsync(CreatePrescription(case1.Id, CreateItem("黄芪", 10, 10m))); // 100

        var result = await _sut.GetIncomeTrendAsync(monday, sunday, ReportGranularity.Week, CancellationToken.None);

        result.Labels.Should().Equal("08-03");
        result.Registration.Should().Equal(70m);
        result.Medicine.Should().Equal(100m);
        result.Total.Should().Equal(170m);
    }

    [Fact]
    public async Task GetIncomeTrendAsync_MonthGranularity_UsesYearMonthLabel()
    {
        var start = new DateTime(2026, 8, 1);
        var end = new DateTime(2026, 8, 31);
        await AddRegistrationAsync(CreateRegistration(Guid.NewGuid(), 50), new DateTime(2026, 8, 15));

        var result = await _sut.GetIncomeTrendAsync(start, end, ReportGranularity.Month, CancellationToken.None);

        result.Labels.Should().Equal("2026-08");
        result.Registration.Should().Equal(50m);
        result.Medicine.Should().Equal(0m);
        result.Total.Should().Equal(50m);
    }

    [Fact]
    public async Task GetConsultationTrendAsync_ZeroFillsEmptyDays()
    {
        await AddCaseAsync(CreateCase("张医生"), Day1);
        await AddCaseAsync(CreateCase("张医生"), Day1);
        await AddCaseAsync(CreateCase("李医生"), Day3);

        var result = await _sut.GetConsultationTrendAsync(Day1, Day3, ReportGranularity.Day, CancellationToken.None);

        result.Labels.Should().Equal("08-01", "08-02", "08-03");
        result.Counts.Should().Equal(2, 0, 1);
    }

    [Fact]
    public async Task GetDoctorPerformanceAsync_ComputesAveragePrescriptionPrice()
    {
        var caseA1 = CreateCase("张医生");
        var caseA2 = CreateCase("张医生");
        await AddCaseAsync(caseA1, Day1);
        await AddCaseAsync(caseA2, Day2);
        await AddRegistrationAsync(CreateRegistration(caseA1.Id, 50), Day1);
        await AddAsync(
            CreatePrescription(caseA1.Id, CreateItem("黄芪", 10, 10m), CreateItem("甘草", 10, 5m)), // 150
            CreatePrescription(caseA2.Id, CreateItem("当归", 10, 5m))); // 50

        var result = await _sut.GetDoctorPerformanceAsync(Day1, Day2, CancellationToken.None);

        result.Should().ContainSingle();
        var doctor = result[0];
        doctor.ConsultationCount.Should().Be(2);
        doctor.RegistrationFeeTotal.Should().Be(50);
        doctor.MedicineFeeTotal.Should().Be(200);
        doctor.AveragePrescriptionPrice.Should().Be(100);
    }

    [Fact]
    public async Task GetDoctorPerformanceAsync_NoPrescription_ReturnsZeroAverage()
    {
        var case1 = CreateCase("张医生");
        await AddCaseAsync(case1, Day1);

        var result = await _sut.GetDoctorPerformanceAsync(Day1, Day1, CancellationToken.None);

        var doctor = result.Single();
        doctor.MedicineFeeTotal.Should().Be(0);
        doctor.AveragePrescriptionPrice.Should().Be(0);
    }

    [Fact]
    public async Task GetPatientFlowAsync_WeekGranularity_RollsUpByWeek()
    {
        var monday = new DateTime(2026, 8, 3);
        var sunday = new DateTime(2026, 8, 9);

        // P1 首次就诊周一（新），周四再次就诊（回头）
        var p1 = Guid.NewGuid();
        var p1First = CreateCase("张医生");
        p1First.PatientId = p1;
        var p1Again = CreateCase("张医生");
        p1Again.PatientId = p1;
        await AddCaseAsync(p1First, monday);
        await AddCaseAsync(p1Again, new DateTime(2026, 8, 6));

        // P2 首次就诊周四（新）
        var p2 = Guid.NewGuid();
        var p2First = CreateCase("李医生");
        p2First.PatientId = p2;
        await AddCaseAsync(p2First, new DateTime(2026, 8, 6));

        var result = await _sut.GetPatientFlowAsync(monday, sunday, ReportGranularity.Week, CancellationToken.None);

        result.Labels.Should().Equal("08-03");
        result.NewPatients.Should().Equal(2);
        result.ReturningPatients.Should().Equal(1);
    }

    [Fact]
    public async Task GetHerbRankingAsync_AppliesTopLimit()
    {
        var case1 = CreateCase("张医生");
        var case2 = CreateCase("张医生");
        await AddCaseAsync(case1, Day1);
        await AddCaseAsync(case2, Day1);
        await AddAsync(
            CreatePrescription(case1.Id, CreateItem("甘草", 10, 1m), CreateItem("黄芪", 10, 1m)),
            CreatePrescription(case2.Id, CreateItem("当归", 10, 1m)));

        var result = await _sut.GetHerbRankingAsync(Day1, Day1, 1, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].HerbName.Should().Be("甘草");
    }
}
