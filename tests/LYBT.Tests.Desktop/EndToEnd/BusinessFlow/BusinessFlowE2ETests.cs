using LYBT.Desktop.Contracts.DataSources;
using LYBT.Tests.Desktop.Infrastructure;
using LYBT.Desktop.LocalData.Context;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.EndToEnd.BusinessFlow;

/// <summary>
/// 业务全流程 E2E 集成测试
/// 模拟中医诊所从开业到完成第一个医案的完整业务链路:
///   创建管理员 -> 录入药材 -> 创建验方 -> 登记患者 -> 创建医案(含诊断) -> 添加处方(含药材) -> 验证完整数据链
/// 测试层级: DataSource 层（非 ViewModel）
/// 数据库: SQLite InMemory (由 DesktopFixture 提供)
/// </summary>
public class BusinessFlowE2ETests : IClassFixture<DesktopFixture>
{
    private readonly DesktopFixture _fixture;

    public BusinessFlowE2ETests(DesktopFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 完整业务流程测试: 从诊所开业到完成第一个医案
    ///
    /// 业务场景:
    ///   1. 系统初始化 - 创建管理员用户
    ///   2. 基础数据准备 - 录入常用中药材 (10味)
    ///   3. 验方管理 - 创建经典方剂并关联药材
    ///   4. 患者登记 - 新患者来诊，登记基本信息
    ///   5. 医案创建 - 医生创建医案，记录诊断信息（四诊合参）
    ///   6. 处方开具 - 根据辨证论治选药组方
    ///   7. 数据验证 - 验证完整数据链: 医案 -> 诊断 -> 处方 -> 处方明细 -> 患者
    /// </summary>
    [Fact]
    public async Task FullBusinessFlow_FromClinicOpeningToFirstCompletedMedicalCase()
    {
        // 创建独立的 ServiceProvider，确保测试隔离
        var serviceProvider = _fixture.CreateServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        dbContext.Database.EnsureCreated();

        // 获取所有 DataSource
        var userDataSource = serviceProvider.GetRequiredService<IUserDataSource>();
        var herbDataSource = serviceProvider.GetRequiredService<IHerbDataSource>();
        var formulaDataSource = serviceProvider.GetRequiredService<IFormulaDataSource>();
        var patientDataSource = serviceProvider.GetRequiredService<IPatientDataSource>();
        var medicalCaseDataSource = serviceProvider.GetRequiredService<IMedicalCaseDataSource>();

        // ================================================================
        // Step 1: 创建管理员用户 - 系统初始化的第一步
        // 诊所系统上线后，首先需要创建管理员账号
        // ================================================================
        var adminUserInput = new UserInputDto
        {
            UserName = "admin",
            RealName = "系统管理员",
            PinYinCode = "XTGLY",
            Role = UserRole.Admin,
            Password = "Admin@123456",
        };

        var createdAdmin = await userDataSource.CreateAsync(adminUserInput);

        // 验证管理员创建成功
        createdAdmin.Should().NotBeNull();
        createdAdmin.Id.Should().NotBe(Guid.Empty);
        createdAdmin.UserName.Should().Be("admin");
        createdAdmin.Role.Should().Be(UserRole.Admin);

        // 通过 DbContext 直接验证数据已持久化
        var dbAdmin = await dbContext.Users.FindAsync(createdAdmin.Id);
        dbAdmin.Should().NotBeNull("管理员数据应已持久化到数据库");
        dbAdmin!.RealName.Should().Be("系统管理员");

        // ================================================================
        // Step 2: 录入常用中药材 (10味)
        // 开业前需要将药房常用药材录入系统，包含名称、分类、单价等信息
        // 中药分类: 补气药、补血药、清热药、解表药、理气药等
        // ================================================================
        var herbDefinitions = new (string Name, string PinYin, string Category, decimal Price, string Unit, string Effect)[]
        {
            // 补气药（益气健脾类）
            ("黄芪", "HQ", "补气药", 3.5m, "g", "补气升阳，固表止汗，利水消肿，生津养血"),
            ("党参", "DS", "补气药", 4.0m, "g", "补中益气，健脾益肺"),
            ("白术", "BZ", "补气药", 2.8m, "g", "健脾益气，燥湿利水，止汗"),
            ("茯苓", "FL", "补气药", 2.0m, "g", "利水渗湿，健脾宁心"),
            ("甘草", "GC", "补气药", 1.5m, "g", "补脾益气，清热解毒，调和诸药"),

            // 补血药
            ("当归", "DG", "补血药", 5.0m, "g", "补血活血，调经止痛"),
            ("熟地黄", "SDH", "补血药", 3.0m, "g", "补血滋阴，益精填髓"),

            // 解表药
            ("桂枝", "GZ", "解表药", 1.8m, "g", "发汗解肌，温通经脉"),
            ("白芍", "BS", "补血药", 2.5m, "g", "养血调经，敛阴止汗，柔肝止痛"),

            // 理气药
            ("陈皮", "CP", "理气药", 2.2m, "g", "理气健脾，燥湿化痰"),
        };

        var createdHerbs = new List<HerbDetailDto>();
        foreach (var (name, pinyin, category, price, unit, effect) in herbDefinitions)
        {
            var herbInput = new HerbInputDto
            {
                Name = name,
                PinYinCode = pinyin,
                Category = category,
                Price = price,
                Unit = unit,
                Effect = effect,
            };

            var createdHerb = await herbDataSource.CreateAsync(herbInput);
            createdHerbs.Add(createdHerb);
        }

        // 验证 10 味药材全部创建成功
        createdHerbs.Should().HaveCount(10, "应成功创建 10 味常用中药");
        createdHerbs.Should().OnlyContain(h => h.Id != Guid.Empty, "每味药材都应有有效的 ID");

        // 通过 DbContext 验证数据库中的药材总数
        var dbHerbCount = await dbContext.Herbs.CountAsync();
        dbHerbCount.Should().Be(10, "数据库中应有 10 条药材记录");

        // 验证分类分布
        var categories = createdHerbs.Select(h => h.Category).Distinct().ToList();
        categories.Should().Contain("补气药", "应包含补气药分类");
        categories.Should().Contain("补血药", "应包含补血药分类");

        // ================================================================
        // Step 3: 创建验方 - 以经典方剂"四君子汤"为例
        // 四君子汤: 党参、白术、茯苓、甘草 (益气健脾的基础方)
        // 验方关联药材，记录每味药的标准剂量
        // ================================================================

        // 找到四君子汤所需的药材
        var dangShen = createdHerbs.First(h => h.Name == "党参");
        var baiZhu = createdHerbs.First(h => h.Name == "白术");
        var fuLing = createdHerbs.First(h => h.Name == "茯苓");
        var ganCao = createdHerbs.First(h => h.Name == "甘草");

        var formulaInput = new FormulaInputDto
        {
            Name = "四君子汤",
            Effect = "益气健脾",
            Indications = "脾胃气虚证。面色萎白，语声低微，气短乏力，食少便溏，舌淡苔白，脉虚弱。",
            Usage = "水煎服，每日一剂",
            Category = "补益剂",
            // 验方药材组成: 四味药，按经典剂量配比
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new()
                {
                    HerbId = dangShen.Id,
                    HerbName = "党参",
                    Dosage = 15,       // 君药 (主药)，剂量最大
                    Unit = "g",
                },
                new()
                {
                    HerbId = baiZhu.Id,
                    HerbName = "白术",
                    Dosage = 10,       // 臣药 (辅助药)
                    Unit = "g",
                },
                new()
                {
                    HerbId = fuLing.Id,
                    HerbName = "茯苓",
                    Dosage = 10,       // 佐药
                    Unit = "g",
                },
                new()
                {
                    HerbId = ganCao.Id,
                    HerbName = "甘草",
                    Dosage = 6,        // 使药 (调和药)，剂量最小
                    Unit = "g",
                },
            },
        };

        var createdFormula = await formulaDataSource.CreateAsync(formulaInput);

        // 验证验方创建成功
        createdFormula.Should().NotBeNull();
        createdFormula.Id.Should().NotBe(Guid.Empty);
        createdFormula.Name.Should().Be("四君子汤");

        // 通过 DbContext 验证验方及其药材组成已持久化
        var dbFormula = await dbContext.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == createdFormula.Id);
        dbFormula.Should().NotBeNull("验方数据应已持久化到数据库");
        dbFormula!.Herbs.Should().HaveCount(4, "四君子汤应包含 4 味药材");

        // 验证药材关联正确
        var formulaHerbNames = dbFormula.Herbs.Select(h => h.HerbName).ToList();
        formulaHerbNames.Should().Contain("党参");
        formulaHerbNames.Should().Contain("白术");
        formulaHerbNames.Should().Contain("茯苓");
        formulaHerbNames.Should().Contain("甘草");

        // ================================================================
        // Step 4: 患者登记 - 新患者来诊
        // 患者基本信息: 姓名、性别、出生日期、联系方式、地址等
        // ================================================================
        var patientInput = new PatientInputDto
        {
            Name = "王建国",
            PinYinCode = "WJG",
            Gender = Gender.Male,
            BirthDate = new DateTime(1975, 3, 20),
            PhoneNumber = "13812345678",
            Address = "北京市朝阳区建国路88号",
            AllergyHistory = "无",
            MedicalHistory = "高血压病史5年",
        };

        var createdPatient = await patientDataSource.CreateAsync(patientInput);

        // 验证患者创建成功
        createdPatient.Should().NotBeNull();
        createdPatient.Id.Should().NotBe(Guid.Empty);
        createdPatient.Name.Should().Be("王建国");
        createdPatient.Gender.Should().Be(Gender.Male);

        // 通过 DbContext 直接验证
        var dbPatient = await dbContext.Patients.FindAsync(createdPatient.Id);
        dbPatient.Should().NotBeNull("患者数据应已持久化到数据库");
        dbPatient!.PhoneNumber.Should().Be("13812345678");

        // ================================================================
        // Step 5: 创建医案 + 诊断 (Consultation)
        // 医案是聚合根，包含诊断信息 (共享主键: Consultation.Id == MedicalCase.Id)
        // 中医四诊: 望(舌诊)、闻、问(现病史)、切(脉诊)
        // 辨证论治: 根据四诊信息，得出中医诊断
        // ================================================================
        var medicalCaseInput = new MedicalCaseInputDto
        {
            PatientId = createdPatient.Id,
            UserId = createdAdmin.Id,
            NeedsPrescription = true,  // 本次就诊需要开处方
            // 诊断记录
            Consultation = new ConsultationInputDto
            {
                // 现病史 - 患者主诉及病程描述
                PresentIllness = "患者近半月来感到倦怠乏力，食欲不振，食后腹胀，大便偏溏，精神萎靡，自汗。",
                // 舌诊 - 望诊的重要组成
                TongueDiagnosis = "舌淡胖，边有齿痕，苔薄白",
                // 脉诊 - 切诊
                PulseDiagnosis = "脉细弱",
                // 中医辨证 - 综合四诊得出的诊断结论
                TcmDiagnosis = "脾胃气虚证",
            },
        };

        var createdMedicalCase = await medicalCaseDataSource.CreateAsync(medicalCaseInput);

        // 验证医案创建成功
        createdMedicalCase.Should().NotBeNull();
        createdMedicalCase.Id.Should().NotBe(Guid.Empty);
        createdMedicalCase.PatientId.Should().Be(createdPatient.Id, "医案应关联到正确的患者");
        createdMedicalCase.UserId.Should().Be(createdAdmin.Id, "医案应关联到正确的医生");
        createdMedicalCase.CaseNumber.Should().NotBeNullOrEmpty("应自动生成医案编号");
        createdMedicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Suspended, "新建医案初始状态应为 Suspended");

        // 验证诊断记录（共享主键）
        var dbConsultation = await dbContext.Consultations.FindAsync(createdMedicalCase.Id);
        dbConsultation.Should().NotBeNull("诊断记录应随医案一起创建");
        dbConsultation!.Id.Should().Be(createdMedicalCase.Id, "诊断记录 Id 应与医案 Id 相同（共享主键）");
        dbConsultation.TcmDiagnosis.Should().Be("脾胃气虚证");
        dbConsultation.TongueDiagnosis.Should().Be("舌淡胖，边有齿痕，苔薄白");
        dbConsultation.PulseDiagnosis.Should().Be("脉细弱");

        // ================================================================
        // Step 6: 添加处方 - 根据辨证结果，选用四君子汤加减
        // 处方以四君子汤为基础，加减黄芪、陈皮
        // 通过 UpdateAsync 将处方添加到已有医案
        // ================================================================

        // 构建处方药材明细
        // 基础方: 四君子汤 (党参、白术、茯苓、甘草)
        // 加味: 黄芪 (加强补气)、陈皮 (理气健脾)
        var huangQi = createdHerbs.First(h => h.Name == "黄芪");
        var chenPi = createdHerbs.First(h => h.Name == "陈皮");

        var prescriptionItems = new List<PrescriptionItemInputDto>
        {
            // 四君子汤原方药材
            new() { HerbId = dangShen.Id, HerbName = "党参", Dosage = 15, Unit = "g", UnitPrice = dangShen.Price },
            new() { HerbId = baiZhu.Id, HerbName = "白术", Dosage = 10, Unit = "g", UnitPrice = baiZhu.Price },
            new() { HerbId = fuLing.Id, HerbName = "茯苓", Dosage = 10, Unit = "g", UnitPrice = fuLing.Price },
            new() { HerbId = ganCao.Id, HerbName = "甘草", Dosage = 6, Unit = "g", UnitPrice = ganCao.Price },
            // 加味药材
            new() { HerbId = huangQi.Id, HerbName = "黄芪", Dosage = 20, Unit = "g", UnitPrice = huangQi.Price },
            new() { HerbId = chenPi.Id, HerbName = "陈皮", Dosage = 6, Unit = "g", UnitPrice = chenPi.Price },
        };

        // 通过 UpdateAsync 将处方添加到医案
        var medicalCaseUpdateInput = new MedicalCaseInputDto
        {
            Id = createdMedicalCase.Id,
            PatientId = createdMedicalCase.PatientId,
            UserId = createdMedicalCase.UserId,
            NeedsPrescription = true,
            // 保留原有诊断
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "患者近半月来感到倦怠乏力，食欲不振，食后腹胀，大便偏溏，精神萎靡，自汗。",
                TongueDiagnosis = "舌淡胖，边有齿痕，苔薄白",
                PulseDiagnosis = "脉细弱",
                TcmDiagnosis = "脾胃气虚证",
            },
            // 新增处方
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = createdMedicalCase.Id,
                DosageCount = 7,           // 7帖
                Discount = 1.0m,           // 原价
                Usage = "每日一剂，水煎服，分早晚两次温服",
                Advice = "忌食生冷油腻，注意休息，避免劳累",
                ReferencedFormulas = "四君子汤",
                Items = prescriptionItems,
            },
        };

        var updatedMedicalCase = await medicalCaseDataSource.UpdateAsync(medicalCaseUpdateInput);

        // 验证处方创建成功
        updatedMedicalCase.Should().NotBeNull();

        // 通过 DbContext 验证处方数据
        var dbPrescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.MedicalCaseId == createdMedicalCase.Id);
        dbPrescription.Should().NotBeNull("处方应已保存到数据库");
        dbPrescription!.DosageCount.Should().Be(7, "处方帖数应为 7");
        dbPrescription.Usage.Should().Contain("水煎服");
        dbPrescription.ReferencedFormulas.Should().Be("四君子汤");
        dbPrescription.Items.Should().HaveCount(6, "处方应包含 6 味药材（四君子汤 4 味 + 加味 2 味）");

        // 验证处方明细中的药材信息
        var prescriptionHerbNames = dbPrescription.Items.Select(i => i.HerbName).ToList();
        prescriptionHerbNames.Should().Contain("党参");
        prescriptionHerbNames.Should().Contain("白术");
        prescriptionHerbNames.Should().Contain("茯苓");
        prescriptionHerbNames.Should().Contain("甘草");
        prescriptionHerbNames.Should().Contain("黄芪");
        prescriptionHerbNames.Should().Contain("陈皮");

        // 验证药材剂量
        var huangQiItem = dbPrescription.Items.First(i => i.HerbName == "黄芪");
        huangQiItem.Dosage.Should().Be(20, "黄芪剂量应为 20g");
        huangQiItem.UnitPrice.Should().Be(3.5m, "黄芪单价应为 3.5 元/g");

        // ================================================================
        // Step 7: 验证完整数据链
        // 使用 GetWithDetailsAsync 一次性加载聚合根及所有关联数据
        // 数据链: MedicalCase -> Consultation -> Prescription -> PrescriptionItems
        //                     -> Patient (通过 PatientId 关联)
        // ================================================================
        var fullMedicalCase = await medicalCaseDataSource.GetWithDetailsAsync(createdMedicalCase.Id);

        // 7.1 医案聚合根
        fullMedicalCase.Should().NotBeNull("应能通过 GetWithDetailsAsync 获取完整医案");
        fullMedicalCase!.Id.Should().Be(createdMedicalCase.Id);
        fullMedicalCase.PatientName.Should().Be("王建国");
        fullMedicalCase.DoctorName.Should().Be("系统管理员");

        // 7.2 诊断记录 (Consultation)
        fullMedicalCase.Consultation.Should().NotBeNull("医案应包含诊断记录");
        fullMedicalCase.Consultation!.Id.Should().Be(fullMedicalCase.Id, "诊断 Id 与医案 Id 共享主键");
        fullMedicalCase.Consultation.TcmDiagnosis.Should().Be("脾胃气虚证");
        fullMedicalCase.Consultation.PresentIllness.Should().Contain("倦怠乏力");

        // 7.3 处方 (Prescription)
        fullMedicalCase.Prescription.Should().NotBeNull("医案应包含处方");
        fullMedicalCase.Prescription!.MedicalCaseId.Should().Be(fullMedicalCase.Id);
        fullMedicalCase.Prescription.DosageCount.Should().Be(7);
        fullMedicalCase.Prescription.Usage.Should().Contain("水煎服");

        // 7.4 处方明细 (PrescriptionItems)
        fullMedicalCase.Prescription.Items.Should().HaveCount(6, "处方应包含 6 味药材");
        fullMedicalCase.Prescription.Items.Should().OnlyContain(
            item => item.HerbId != Guid.Empty,
            "每个处方药材项都应关联到药材库");
        fullMedicalCase.Prescription.Items.Should().OnlyContain(
            item => item.Dosage > 0,
            "每个处方药材项都应有有效剂量");

        // 7.5 患者关联验证 (通过 PatientId 跨聚合引用)
        var linkedPatient = await patientDataSource.GetByIdAsync(fullMedicalCase.PatientId);
        linkedPatient.Should().NotBeNull("应能通过医案的 PatientId 找到关联的患者");
        linkedPatient!.Name.Should().Be("王建国");
        linkedPatient.Gender.Should().Be(Gender.Male);
        linkedPatient.PhoneNumber.Should().Be("13812345678");

        // 7.6 医生关联验证 (通过 UserId 跨聚合引用)
        var linkedDoctor = await userDataSource.GetByIdAsync(fullMedicalCase.UserId);
        linkedDoctor.Should().NotBeNull("应能通过医案的 UserId 找到关联的医生");
        linkedDoctor!.RealName.Should().Be("系统管理员");
        linkedDoctor.Role.Should().Be(UserRole.Admin);

        // 7.7 数据库层面的完整性验证 - 确认所有表都有正确的数据
        var totalUsers = await dbContext.Users.CountAsync();
        var totalHerbs = await dbContext.Herbs.CountAsync();
        var totalFormulas = await dbContext.Formulas.CountAsync();
        var totalFormulaHerbItems = await dbContext.FormulaHerbItems.CountAsync();
        var totalPatients = await dbContext.Patients.CountAsync();
        var totalMedicalCases = await dbContext.MedicalCases.CountAsync();
        var totalConsultations = await dbContext.Consultations.CountAsync();
        var totalPrescriptions = await dbContext.Prescriptions.CountAsync();
        var totalPrescriptionItems = await dbContext.PrescriptionItems.CountAsync();

        totalUsers.Should().Be(1, "应有 1 个管理员用户");
        totalHerbs.Should().Be(10, "应有 10 味中药材");
        totalFormulas.Should().Be(1, "应有 1 个验方（四君子汤）");
        totalFormulaHerbItems.Should().Be(4, "验方应有 4 个药材组成");
        totalPatients.Should().Be(1, "应有 1 个患者");
        totalMedicalCases.Should().Be(1, "应有 1 个医案");
        totalConsultations.Should().Be(1, "应有 1 条诊断记录");
        totalPrescriptions.Should().Be(1, "应有 1 张处方");
        totalPrescriptionItems.Should().Be(6, "处方应有 6 个药材明细");

        // 7.8 通过患者反向查询医案列表
        var patientCases = await medicalCaseDataSource.GetByPatientIdAsync(createdPatient.Id);
        patientCases.Should().HaveCount(1, "该患者应有 1 个医案");
        patientCases.First().Id.Should().Be(createdMedicalCase.Id);
    }
}
