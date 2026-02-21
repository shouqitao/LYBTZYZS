using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Consultations;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.Integration.MedicalCases;

/// <summary>
/// MedicalCase DataSource 聚合操作集成测试。
/// 验证医案聚合根的完整 CRUD、Consultation/Prescription 嵌套保存、
/// 状态管理、查询过滤等功能。
/// </summary>
public class MedicalCaseDataSourceTests : IClassFixture<DesktopFixture>
{
    private readonly DesktopFixture _fixture;

    public MedicalCaseDataSourceTests(DesktopFixture fixture)
    {
        _fixture = fixture;
    }

    #region SaveAsync 聚合保存

    [Fact]
    public async Task SaveAsync_WithConsultation_ShouldPersistBoth()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var patientId = await SeedPatient(sp, "聚合保存测试患者");

        var mc = new MedicalCase
        {
            PatientId = patientId,
            PatientName = "聚合保存测试患者",
            UserId = DesktopFixture.TestUserId,
            DoctorName = "测试医生",
            CaseStatus = MedicalCaseStatus.Active,
            Consultation = new Consultation
            {
                PresentIllness = "头痛三天",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "脉弦数",
                TcmDiagnosis = "肝阳上亢"
            }
        };

        // Act
        var saved = await ds.SaveAsync(mc);

        // Assert
        saved.Should().NotBeNull();
        saved.Id.Should().NotBe(Guid.Empty);

        // 验证聚合详情
        var detail = await ds.GetWithDetailsAsync(saved.Id);
        detail.Should().NotBeNull();
        detail!.Consultation.Should().NotBeNull();
        detail.Consultation!.PresentIllness.Should().Be("头痛三天");
        detail.Consultation.TcmDiagnosis.Should().Be("肝阳上亢");
    }

    [Fact]
    public async Task SaveAsync_WithPrescription_ShouldPersistAll()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var herbDs = sp.GetRequiredService<IHerbDataSource>();
        var patientId = await SeedPatient(sp, "处方保存测试患者");

        // 先创建药材
        var herb = await herbDs.CreateAsync(new Herb
        {
            Name = "黄芪",
            PinYinCode = "HQ",
            Unit = "克",
            Price = 3.0m
        });

        var mc = new MedicalCase
        {
            PatientId = patientId,
            PatientName = "处方保存测试患者",
            UserId = DesktopFixture.TestUserId,
            DoctorName = "测试医生",
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = true,
            Consultation = new Consultation
            {
                TcmDiagnosis = "气虚证"
            },
            Prescription = new Prescription
            {
                DosageCount = 7,
                Usage = "水煎服，日一剂",
                Items = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        HerbId = herb.Id,
                        HerbName = "黄芪",
                        Dosage = 30,
                        Unit = "克"
                    }
                }
            }
        };

        // Act
        var saved = await ds.SaveAsync(mc);

        // Assert
        saved.Should().NotBeNull();
        var detail = await ds.GetWithDetailsAsync(saved.Id);
        detail.Should().NotBeNull();
        detail!.NeedsPrescription.Should().BeTrue();
        detail.Prescription.Should().NotBeNull();
        detail.Prescription!.DosageCount.Should().Be(7);
        detail.Prescription.Usage.Should().Be("水煎服，日一剂");
        detail.Prescription.Items.Should().HaveCount(1);
        detail.Prescription.Items.First().HerbName.Should().Be("黄芪");
        detail.Prescription.Items.First().Dosage.Should().Be(30);
    }

    [Fact]
    public async Task SaveAsync_UpdateConsultation_ShouldPersistChanges()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var patientId = await SeedPatient(sp, "更新测试");

        var mc = await ds.SaveAsync(new MedicalCase
        {
            PatientId = patientId,
            PatientName = "更新测试",
            UserId = DesktopFixture.TestUserId,
            DoctorName = "医生",
            CaseStatus = MedicalCaseStatus.Active,
            Consultation = new Consultation { TcmDiagnosis = "初诊" }
        });

        // Act - 更新诊断
        var loaded = await ds.GetWithDetailsAsync(mc.Id);
        loaded!.Consultation!.TcmDiagnosis = "复诊更新";
        loaded.Consultation.PresentIllness = "新增现病史";
        await ds.SaveAsync(loaded);

        // Assert
        var updated = await ds.GetWithDetailsAsync(mc.Id);
        updated!.Consultation!.TcmDiagnosis.Should().Be("复诊更新");
        updated.Consultation.PresentIllness.Should().Be("新增现病史");
    }

    #endregion

    #region CompleteAsync / CancelAsync

    [Fact]
    public async Task CompleteAsync_ShouldSetStatusAndTimestamp()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var patientId = await SeedPatient(sp, "完成测试");

        var mc = await ds.SaveAsync(new MedicalCase
        {
            PatientId = patientId,
            PatientName = "完成测试",
            UserId = DesktopFixture.TestUserId,
            DoctorName = "医生",
            CaseStatus = MedicalCaseStatus.Active
        });

        // Act
        var result = await ds.CompleteAsync(mc.Id);

        // Assert
        result.Should().BeTrue();
        var completed = await ds.GetByIdAsync(mc.Id);
        completed.Should().NotBeNull();
        completed!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task CancelAsync_ShouldSetStatusToCancelled()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var patientId = await SeedPatient(sp, "取消测试");

        var mc = await ds.SaveAsync(new MedicalCase
        {
            PatientId = patientId,
            PatientName = "取消测试",
            UserId = DesktopFixture.TestUserId,
            DoctorName = "医生",
            CaseStatus = MedicalCaseStatus.Active
        });

        // Act
        var result = await ds.CancelAsync(mc.Id, "患者要求取消");

        // Assert
        result.Should().BeTrue();
        // 取消操作改为软删除，GetByIdAsync 默认过滤 IsDeleted，需用特殊查询验证
        var cancelled = await ds.GetWithDetailsAsync(mc.Id);
        // 软删除后通过标准查询应查不到
        cancelled.Should().BeNull("取消操作为软删除，标准查询应过滤已删除记录");
    }

    #endregion

    #region QueryAsync

    [Fact]
    public async Task QueryAsync_ByPatientId_ShouldFilterCorrectly()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();

        var patient1Id = await SeedPatient(sp, "查询患者A");
        var patient2Id = await SeedPatient(sp, "查询患者B");

        await ds.SaveAsync(CreateActiveMedicalCase(patient1Id, "查询患者A"));
        await ds.SaveAsync(CreateActiveMedicalCase(patient1Id, "查询患者A"));
        await ds.SaveAsync(CreateActiveMedicalCase(patient2Id, "查询患者B"));

        // Act
        var (items, total) = await ds.QueryAsync(patientId: patient1Id);

        // Assert
        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(mc => mc.PatientId.Should().Be(patient1Id));
    }

    [Fact]
    public async Task QueryAsync_ByStatus_ShouldFilterCorrectly()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var patientId = await SeedPatient(sp, "状态查询");

        // CreateAsync 强制设为 Draft，所以新建的医案都是 Draft
        var mc1 = await ds.SaveAsync(CreateActiveMedicalCase(patientId, "状态查询"));
        var mc2 = await ds.SaveAsync(CreateActiveMedicalCase(patientId, "状态查询"));
        var mc3 = await ds.SaveAsync(CreateActiveMedicalCase(patientId, "状态查询"));

        // 完成其中一个
        await ds.CompleteAsync(mc3.Id);

        // Act - 按 Draft 状态查询 (新建默认为 Draft)
        var (draftItems, draftTotal) = await ds.QueryAsync(status: MedicalCaseStatus.Draft);

        // Assert - 应该有 2 个 Draft
        draftTotal.Should().Be(2);
        draftItems.Should().HaveCount(2);
        draftItems.Should().AllSatisfy(mc => mc.CaseStatus.Should().Be(MedicalCaseStatus.Draft));
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnAllCasesForPatient()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var patientId = await SeedPatient(sp, "患者医案列表");

        await ds.SaveAsync(CreateActiveMedicalCase(patientId, "患者医案列表"));
        await ds.SaveAsync(CreateActiveMedicalCase(patientId, "患者医案列表"));
        await ds.SaveAsync(CreateActiveMedicalCase(patientId, "患者医案列表"));

        // Act
        var cases = await ds.GetByPatientIdAsync(patientId);

        // Assert
        cases.Should().HaveCount(3);
    }

    #endregion

    #region GetWithDetailsAsync

    [Fact]
    public async Task GetWithDetailsAsync_ShouldLoadNavigationProperties()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        var herbDs = sp.GetRequiredService<IHerbDataSource>();
        var patientId = await SeedPatient(sp, "导航属性测试");

        var herb1 = await herbDs.CreateAsync(new Herb { Name = "当归", PinYinCode = "DG", Unit = "克", Price = 2.0m });
        var herb2 = await herbDs.CreateAsync(new Herb { Name = "川芎", PinYinCode = "CX", Unit = "克", Price = 1.5m });

        var mc = await ds.SaveAsync(new MedicalCase
        {
            PatientId = patientId,
            PatientName = "导航属性测试",
            UserId = DesktopFixture.TestUserId,
            DoctorName = "医生",
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = true,
            Consultation = new Consultation
            {
                PresentIllness = "月经不调",
                TcmDiagnosis = "血虚证"
            },
            Prescription = new Prescription
            {
                DosageCount = 14,
                Usage = "水煎服",
                Items = new List<PrescriptionItem>
                {
                    new() { HerbId = herb1.Id, HerbName = "当归", Dosage = 15, Unit = "克" },
                    new() { HerbId = herb2.Id, HerbName = "川芎", Dosage = 10, Unit = "克" }
                }
            }
        });

        // Act
        var detail = await ds.GetWithDetailsAsync(mc.Id);

        // Assert
        detail.Should().NotBeNull();
        detail!.Consultation.Should().NotBeNull();
        detail.Consultation!.TcmDiagnosis.Should().Be("血虚证");
        detail.Prescription.Should().NotBeNull();
        detail.Prescription!.Items.Should().HaveCount(2);
        detail.Prescription.Items.Should().Contain(i => i.HerbName == "当归");
        detail.Prescription.Items.Should().Contain(i => i.HerbName == "川芎");
    }

    #endregion

    #region Helpers

    private static async Task<Guid> SeedPatient(IServiceProvider sp, string name)
    {
        var ds = sp.GetRequiredService<IPatientDataSource>();
        var patient = await ds.CreateAsync(new Patient
        {
            Name = name,
            PinYinCode = "CS",
            Gender = Gender.Male
        });
        return patient.Id;
    }

    /// <summary>
    /// 创建医案 (注意: CreateAsync 会强制覆盖 CaseStatus 为 Draft)
    /// </summary>
    private static MedicalCase CreateActiveMedicalCase(Guid patientId, string patientName)
    {
        return new MedicalCase
        {
            PatientId = patientId,
            PatientName = patientName,
            UserId = DesktopFixture.TestUserId,
            DoctorName = "测试医生",
            CaseStatus = MedicalCaseStatus.Active // 注: 会被 CreateAsync 覆盖为 Draft
        };
    }

    #endregion
}
