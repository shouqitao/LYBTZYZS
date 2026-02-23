using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.Integration.E2E;

/// <summary>
/// 完整业务流程端到端测试。
/// 验证: 创建用户 -> 创建药材 -> 创建验方 -> 创建患者 -> 创建医案(含诊断+处方) -> 完成医案。
/// 所有操作通过真实 DataSource + SQLite InMemory 执行，验证跨模块数据联动。
/// </summary>
public class BusinessFlowTests : IClassFixture<DesktopFixture>
{
    private readonly DesktopFixture _fixture;

    public BusinessFlowTests(DesktopFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FullBusinessFlow_CreateHerbs_Patient_MedicalCase_Complete()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var userDs = sp.GetRequiredService<IUserDataSource>();
        var herbDs = sp.GetRequiredService<IHerbDataSource>();
        var patientDs = sp.GetRequiredService<IPatientDataSource>();
        var formulaDs = sp.GetRequiredService<IFormulaDataSource>();
        var mcDs = sp.GetRequiredService<IMedicalCaseDataSource>();

        // === Step 1: 创建医生用户 ===
        var doctor = await userDs.CreateAsync(new UserInputDto
        {
            UserName = "dr_zhang",
            RealName = "张医生",
            Role = UserRole.Doctor,
            Password = "Doctor123!",
            ConfirmPassword = "Doctor123!"
        });
        doctor.Id.Should().NotBe(Guid.Empty);
        doctor.Role.Should().Be(UserRole.Doctor);

        // === Step 2: 创建药材库 ===
        var huangqi = await herbDs.CreateAsync(new HerbInputDto
        {
            Name = "黄芪",
            PinYinCode = "HQ",
            Category = "补气药",
            Unit = "克",
            Price = 3.0m,
            Effect = "补气升阳，固表止汗"
        });

        var danggui = await herbDs.CreateAsync(new HerbInputDto
        {
            Name = "当归",
            PinYinCode = "DG",
            Category = "补血药",
            Unit = "克",
            Price = 2.5m,
            Effect = "补血活血，调经止痛"
        });

        var baizhu = await herbDs.CreateAsync(new HerbInputDto
        {
            Name = "白术",
            PinYinCode = "BZ",
            Category = "补气药",
            Unit = "克",
            Price = 1.8m,
            Effect = "健脾益气，燥湿利水"
        });

        huangqi.Id.Should().NotBe(Guid.Empty);
        danggui.Id.Should().NotBe(Guid.Empty);
        baizhu.Id.Should().NotBe(Guid.Empty);

        // 验证药材数据持久化
        var (herbs, herbTotal) = await herbDs.GetPagedAsync(1, 10);
        herbTotal.Should().Be(3);

        // === Step 3: 创建验方 (经验方) ===
        var formula = await formulaDs.CreateAsync(new FormulaInputDto
        {
            Name = "当归补血汤",
            Effect = "补气生血",
            Indications = "血虚发热证",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = huangqi.Id, HerbName = "黄芪", Dosage = 30, Unit = "克" },
                new() { HerbId = danggui.Id, HerbName = "当归", Dosage = 6, Unit = "克" }
            }
        });
        formula.Id.Should().NotBe(Guid.Empty);

        // === Step 4: 创建患者 ===
        var patient = await patientDs.CreateAsync(new PatientInputDto
        {
            Name = "李女士",
            PinYinCode = "LNS",
            Gender = Gender.Female,
            BirthDate = new DateTime(1985, 3, 20),
            PhoneNumber = "13800138001",
            Address = "北京市朝阳区",
            AllergyHistory = "无",
            MedicalHistory = "体弱多年"
        });
        patient.Id.Should().NotBe(Guid.Empty);

        // === Step 5: 创建医案 (含诊断+处方) ===
        var mc = await mcDs.SaveAsync(new MedicalCaseInputDto
        {
            PatientId = patient.Id,
            UserId = doctor.Id,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "患者面色萎黄，头晕乏力三月余，劳累后加重",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "脉细弱",
                TcmDiagnosis = "气血两虚证"
            },
            Prescription = new PrescriptionInputDto
            {
                DosageCount = 7,
                Usage = "水煎服，日一剂，分两次温服",
                Advice = "忌辛辣刺激，注意休息",
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = huangqi.Id,
                        HerbName = "黄芪",
                        Dosage = 30,
                        Unit = "克"
                    },
                    new()
                    {
                        HerbId = danggui.Id,
                        HerbName = "当归",
                        Dosage = 15,
                        Unit = "克"
                    },
                    new()
                    {
                        HerbId = baizhu.Id,
                        HerbName = "白术",
                        Dosage = 12,
                        Unit = "克"
                    }
                }
            }
        });

        mc.Id.Should().NotBe(Guid.Empty);
        mc.CaseStatus.Should().Be(MedicalCaseStatus.Draft); // CreateAsync 默认 Draft

        // === Step 6: 验证聚合详情 ===
        var detail = await mcDs.GetWithDetailsAsync(mc.Id);
        detail.Should().NotBeNull();
        detail!.PatientName.Should().Be("李女士");
        detail.DoctorName.Should().Be("张医生");
        detail.Consultation.Should().NotBeNull();
        detail.Consultation!.TcmDiagnosis.Should().Be("气血两虚证");
        detail.Prescription.Should().NotBeNull();
        detail.Prescription!.DosageCount.Should().Be(7);
        detail.Prescription.Items.Should().HaveCount(3);

        // === Step 7: 完成医案 ===
        var completed = await mcDs.CompleteAsync(mc.Id);
        completed.Should().BeTrue();

        var finalCase = await mcDs.GetByIdAsync(mc.Id);
        finalCase!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);

        // === Step 8: 验证患者的医案列表 ===
        var patientCases = await mcDs.GetByPatientIdAsync(patient.Id);
        patientCases.Should().HaveCount(1);
        patientCases.First().Id.Should().Be(mc.Id);
    }

    [Fact]
    public async Task LocalAuth_Login_ShouldValidateCredentials()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var userDs = sp.GetRequiredService<IUserDataSource>();
        var authService = sp.GetRequiredService<ILocalAuthService>();

        // 创建用户
        await userDs.CreateAsync(new UserInputDto
        {
            UserName = "auth_test",
            RealName = "认证测试用户",
            Role = UserRole.Doctor,
            Password = "MyPassword123!",
            ConfirmPassword = "MyPassword123!"
        });

        // Act - 正确密码
        var validUser = await authService.ValidateAsync("auth_test", "MyPassword123!");
        // Act - 错误密码
        var invalidUser = await authService.ValidateAsync("auth_test", "WrongPassword");

        // Assert
        validUser.Should().NotBeNull();
        validUser!.UserName.Should().Be("auth_test");
        invalidUser.Should().BeNull();
    }

    [Fact]
    public async Task MultiplePatients_MultipleMedicalCases_DataIsolation()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var patientDs = sp.GetRequiredService<IPatientDataSource>();
        var mcDs = sp.GetRequiredService<IMedicalCaseDataSource>();

        // 创建两个患者
        var patientA = await patientDs.CreateAsync(new PatientInputDto
        {
            Name = "患者A",
            PinYinCode = "HZA",
            Gender = Gender.Male
        });
        var patientB = await patientDs.CreateAsync(new PatientInputDto
        {
            Name = "患者B",
            PinYinCode = "HZB",
            Gender = Gender.Female
        });

        // 为患者A创建2个医案（需先完成第一个才能创建第二个）
        var mcA1 = await mcDs.SaveAsync(new MedicalCaseInputDto
        {
            PatientId = patientA.Id,
            UserId = DesktopFixture.TestUserId,
        });
        await mcDs.CompleteAsync(mcA1.Id);
        await mcDs.SaveAsync(new MedicalCaseInputDto
        {
            PatientId = patientA.Id,
            UserId = DesktopFixture.TestUserId,
        });

        // 为患者B创建1个医案
        await mcDs.SaveAsync(new MedicalCaseInputDto
        {
            PatientId = patientB.Id,
            UserId = DesktopFixture.TestUserId,
        });

        // Act
        var casesA = await mcDs.GetByPatientIdAsync(patientA.Id);
        var casesB = await mcDs.GetByPatientIdAsync(patientB.Id);

        // Assert - 数据隔离
        casesA.Should().HaveCount(2);
        casesB.Should().HaveCount(1);
        casesA.Should().AllSatisfy(c => c.PatientId.Should().Be(patientA.Id));
        casesB.Should().AllSatisfy(c => c.PatientId.Should().Be(patientB.Id));
    }
}
