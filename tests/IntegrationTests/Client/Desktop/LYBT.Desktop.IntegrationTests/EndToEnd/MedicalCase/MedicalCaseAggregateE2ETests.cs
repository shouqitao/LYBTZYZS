using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.IntegrationTests.LocalMode.Fixtures;
using LYBT.Desktop.LocalData.Context;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.IntegrationTests.EndToEnd.MedicalCase;

/// <summary>
/// 医案聚合根持久化深度测试
/// 验证 MedicalCase 作为 DDD 聚合根，管理 Consultation（1:1 共享主键）和 Prescription（1:0..1）的完整生命周期
///
/// 测试层级: DataSource 层（LocalMedicalCaseDataSource -> LocalDbContext -> SQLite InMemory）
/// 夹具: LocalModeTestFixture（轻量级 DI 容器，不依赖 ViewModel/Prism）
/// </summary>
public class MedicalCaseAggregateE2ETests : IClassFixture<LocalModeTestFixture>
{
    private readonly LocalModeTestFixture _fixture;

    public MedicalCaseAggregateE2ETests(LocalModeTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region 辅助方法

    /// <summary>
    /// 创建已初始化的 ServiceProvider 和 DataSource
    /// 每个测试使用独立的 InMemory SQLite 连接，确保数据隔离
    /// </summary>
    private (IServiceProvider sp, IMedicalCaseDataSource ds, LocalDbContext db) CreateTestContext()
    {
        var sp = _fixture.CreateServiceProvider();
        var db = sp.GetRequiredService<LocalDbContext>();
        db.Database.EnsureCreated();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();
        return (sp, ds, db);
    }

    /// <summary>
    /// 构建测试用医案输入DTO（不含 Consultation 和 Prescription）
    /// </summary>
    private static MedicalCaseInputDto BuildBaseMedicalCaseInput(
        Guid? patientId = null,
        Guid? userId = null,
        string patientName = "测试患者",
        string doctorName = "测试医生")
    {
        return new MedicalCaseInputDto
        {
            PatientId = patientId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
        };
    }

    /// <summary>
    /// 构建测试用诊断输入DTO
    /// 四诊合参：现病史、舌诊、脉诊、中医辨证
    /// </summary>
    private static ConsultationInputDto BuildConsultationInput(
        string presentIllness = "头痛三天，伴眩晕",
        string tongueDiagnosis = "舌淡苔白",
        string pulseDiagnosis = "脉细弱",
        string tcmDiagnosis = "气虚头痛")
    {
        return new ConsultationInputDto
        {
            PresentIllness = presentIllness,
            TongueDiagnosis = tongueDiagnosis,
            PulseDiagnosis = pulseDiagnosis,
            TcmDiagnosis = tcmDiagnosis,
        };
    }

    /// <summary>
    /// 构建测试用处方输入DTO（含药材明细）
    /// 标准中药处方：帖数、用法、医嘱、药材项
    /// </summary>
    private static PrescriptionInputDto BuildPrescriptionInput(List<PrescriptionItemInputDto>? items = null)
    {
        var defaultItems = items ?? new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "黄芪",    // 补气要药
                Dosage = 30,
                Unit = "g",
                UnitPrice = 3.5m,
                DecocteMethod = DecocteMethod.Default,
            },
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "当归",    // 补血活血
                Dosage = 15,
                Unit = "g",
                UnitPrice = 5.0m,
                DecocteMethod = DecocteMethod.Default,
            },
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "川芎",    // 活血行气
                Dosage = 10,
                Unit = "g",
                UnitPrice = 4.0m,
                DecocteMethod = DecocteMethod.Default,
            },
        };

        return new PrescriptionInputDto
        {
            DosageCount = 7,           // 7帖
            Discount = 1.0m,
            Usage = "每日一剂，水煎服，分早晚温服",
            Advice = "忌辛辣生冷，注意休息",
            Items = defaultItems,
        };
    }

    /// <summary>
    /// 将 MedicalCaseDetailDto 转换为 MedicalCaseInputDto（用于 UpdateAsync 调用）
    /// </summary>
    private static MedicalCaseInputDto ToInputDto(MedicalCaseDetailDto detail)
    {
        var input = new MedicalCaseInputDto
        {
            Id = detail.Id,
            PatientId = detail.PatientId,
            UserId = detail.UserId,
            NeedsPrescription = detail.HasPrescription ? true : null,
        };

        if (detail.Consultation != null)
        {
            input.Consultation = new ConsultationInputDto
            {
                PresentIllness = detail.Consultation.PresentIllness,
                TongueDiagnosis = detail.Consultation.TongueDiagnosis,
                PulseDiagnosis = detail.Consultation.PulseDiagnosis,
                TcmDiagnosis = detail.Consultation.TcmDiagnosis,
            };
        }

        if (detail.Prescription != null)
        {
            input.NeedsPrescription = true;
            input.Prescription = new PrescriptionInputDto
            {
                Id = detail.Prescription.Id,
                MedicalCaseId = detail.Prescription.MedicalCaseId,
                DosageCount = detail.Prescription.DosageCount,
                Discount = detail.Prescription.Discount,
                Usage = detail.Prescription.Usage,
                Advice = detail.Prescription.Advice,
                ReferencedFormulas = detail.Prescription.ReferencedFormulas,
                Items = detail.Prescription.Items.Select(i => new PrescriptionItemInputDto
                {
                    Id = i.Id,
                    HerbId = i.HerbId,
                    HerbName = i.HerbName,
                    Dosage = i.Dosage,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DecocteMethod = i.DecocteMethod,
                }).ToList(),
            };
        }

        return input;
    }

    #endregion

    #region 场景1: 创建医案 + 诊断（共享主键验证）

    [Fact]
    public async Task CreateMedicalCase_WithConsultation_SharedPrimaryKey_ConsultationIdEqualsMedicalCaseId()
    {
        // Arrange
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.Consultation = BuildConsultationInput();

        // Act - 通过 DataSource 创建（聚合根入口）
        var created = await ds.CreateAsync(mc);

        // Assert - 核心验证: Consultation.Id == MedicalCase.Id（共享主键设计）
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty, "聚合根 ID 应由 DataSource 自动生成");

        // 直接查询数据库验证持久化
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Consultation)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();
        dbMc!.Consultation.Should().NotBeNull("Consultation 应随聚合根一起持久化");

        // 共享主键验证 - DDD 聚合内实体通过共享主键建立 1:1 关系
        dbMc.Consultation!.Id.Should().Be(dbMc.Id,
            "Consultation.Id 必须等于 MedicalCase.Id（共享主键设计，EF Core 通过 HasForeignKey<Consultation>(c => c.Id) 配置）");

        // 四诊字段完整性验证
        dbMc.Consultation.PresentIllness.Should().Be("头痛三天，伴眩晕");
        dbMc.Consultation.TongueDiagnosis.Should().Be("舌淡苔白");
        dbMc.Consultation.PulseDiagnosis.Should().Be("脉细弱");
        dbMc.Consultation.TcmDiagnosis.Should().Be("气虚头痛");
    }

    #endregion

    #region 场景2: 创建医案 + 处方 + 处方药材明细

    [Fact]
    public async Task CreateMedicalCase_WithPrescriptionAndItems_PersistsEntireAggregate()
    {
        // Arrange
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.Consultation = BuildConsultationInput();
        mc.NeedsPrescription = true;
        mc.Prescription = BuildPrescriptionInput();

        // Act
        var created = await ds.CreateAsync(mc);

        // Assert - 聚合根完整持久化验证
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();

        // Prescription 关系验证（1:0..1，外键关系）
        dbMc!.Prescription.Should().NotBeNull("处方应随聚合根一起持久化");
        dbMc.Prescription!.MedicalCaseId.Should().Be(dbMc.Id,
            "Prescription.MedicalCaseId 应指向所属医案");
        dbMc.Prescription.Id.Should().NotBe(dbMc.Id,
            "Prescription 使用独立 ID（非共享主键），与 Consultation 不同");

        // 处方基本字段验证
        dbMc.Prescription.DosageCount.Should().Be(7, "默认7帖");
        dbMc.Prescription.Usage.Should().Be("每日一剂，水煎服，分早晚温服");
        dbMc.Prescription.Advice.Should().Be("忌辛辣生冷，注意休息");

        // PrescriptionItems 持久化验证（1:N 关系）
        dbMc.Prescription.Items.Should().HaveCount(3, "应包含3味中药");

        var herbNames = dbMc.Prescription.Items.Select(i => i.HerbName).ToList();
        herbNames.Should().Contain("黄芪");
        herbNames.Should().Contain("当归");
        herbNames.Should().Contain("川芎");

        // 药材项外键关联验证
        foreach (var item in dbMc.Prescription.Items)
        {
            item.PrescriptionId.Should().Be(dbMc.Prescription.Id,
                "每个药材项的 PrescriptionId 应指向所属处方");
            item.Id.Should().NotBe(Guid.Empty, "药材项应有独立 ID");
        }

        // 验证剂量和单价
        var huangqi = dbMc.Prescription.Items.First(i => i.HerbName == "黄芪");
        huangqi.Dosage.Should().Be(30);
        huangqi.UnitPrice.Should().Be(3.5m);
    }

    #endregion

    #region 场景3: 更新诊断字段

    [Fact]
    public async Task UpdateMedicalCase_ModifyConsultationFields_PersistsChanges()
    {
        // Arrange - 先创建带诊断的医案
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.Consultation = BuildConsultationInput(
            presentIllness: "初诊：头痛三天",
            tongueDiagnosis: "舌红苔黄",
            pulseDiagnosis: "脉弦数",
            tcmDiagnosis: "肝阳上亢");
        var created = await ds.CreateAsync(mc);

        // Act - 复诊更新四诊信息（模拟医生修改诊断）
        var toUpdate = await ds.GetWithDetailsAsync(created.Id);
        toUpdate.Should().NotBeNull();

        // 转换为 InputDto 并更新四诊字段
        var updateInput = ToInputDto(toUpdate!);
        updateInput.Consultation!.PresentIllness = "复诊：头痛减轻，仍有眩晕";
        updateInput.Consultation.TongueDiagnosis = "舌淡红苔薄白";
        updateInput.Consultation.PulseDiagnosis = "脉弦";
        updateInput.Consultation.TcmDiagnosis = "肝阳上亢（好转）";

        await ds.UpdateAsync(updateInput);

        // Assert - 验证更新持久化
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Consultation)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();
        dbMc!.Consultation.Should().NotBeNull();

        // 共享主键不变
        dbMc.Consultation!.Id.Should().Be(created.Id,
            "更新后 Consultation.Id 仍等于 MedicalCase.Id");

        // 四诊字段已更新
        dbMc.Consultation.PresentIllness.Should().Be("复诊：头痛减轻，仍有眩晕");
        dbMc.Consultation.TongueDiagnosis.Should().Be("舌淡红苔薄白");
        dbMc.Consultation.PulseDiagnosis.Should().Be("脉弦");
        dbMc.Consultation.TcmDiagnosis.Should().Be("肝阳上亢（好转）");
    }

    #endregion

    #region 场景4: 更新处方（删除旧药材项，添加新药材项）

    [Fact]
    public async Task UpdateMedicalCase_ReplacePrescriptionItems_DeletesOldAndAddsNew()
    {
        // Arrange - 创建带处方的医案
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.Consultation = BuildConsultationInput();
        mc.NeedsPrescription = true;
        mc.Prescription = BuildPrescriptionInput(); // 初始3味药：黄芪、当归、川芎
        var created = await ds.CreateAsync(mc);

        // 记录原始处方项 ID（用于验证删除）
        var originalDetail = await ds.GetWithDetailsAsync(created.Id);
        var originalItemIds = originalDetail!.Prescription!.Items.Select(i => i.Id).ToList();
        originalItemIds.Should().HaveCount(3, "初始应有3味药");

        // Act - 换方：删除所有旧药，添加新药（中医常见的调方操作）
        var toUpdate = await ds.GetWithDetailsAsync(created.Id);
        toUpdate.Should().NotBeNull();

        // 新处方药材：补中益气汤加减
        var newItems = new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "党参",    // 替代黄芪补气
                Dosage = 20,
                Unit = "g",
                UnitPrice = 4.0m,
                DecocteMethod = DecocteMethod.Default,
            },
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "白术",    // 健脾燥湿
                Dosage = 15,
                Unit = "g",
                UnitPrice = 3.0m,
                DecocteMethod = DecocteMethod.Default,
            },
        };

        var updateInput = ToInputDto(toUpdate!);
        updateInput.Prescription!.Items = newItems;
        updateInput.Prescription.DosageCount = 14;   // 帖数调整为14帖
        updateInput.Prescription.Usage = "每日一剂，水煎服，饭前温服";

        await ds.UpdateAsync(updateInput);

        // Assert - 验证处方项完全替换
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();
        dbMc!.Prescription.Should().NotBeNull();

        // 新药材项验证
        dbMc.Prescription!.Items.Should().HaveCount(2, "换方后应只有2味新药");
        dbMc.Prescription.DosageCount.Should().Be(14, "帖数应更新为14");
        dbMc.Prescription.Usage.Should().Be("每日一剂，水煎服，饭前温服");

        var newHerbNames = dbMc.Prescription.Items.Select(i => i.HerbName).ToList();
        newHerbNames.Should().Contain("党参");
        newHerbNames.Should().Contain("白术");
        newHerbNames.Should().NotContain("黄芪", "旧药材项应已删除");
        newHerbNames.Should().NotContain("当归", "旧药材项应已删除");
        newHerbNames.Should().NotContain("川芎", "旧药材项应已删除");

        // 验证新药材项有新 ID（DataSource 会为每个新项生成 Guid）
        foreach (var item in dbMc.Prescription.Items)
        {
            originalItemIds.Should().NotContain(item.Id,
                "新药材项应有全新 ID，旧 ID 不应复用");
        }

        // 验证旧药材项已从数据库删除（使用 IgnoreQueryFilters 确保不被软删除过滤）
        var allItems = await db.PrescriptionItems
            .IgnoreQueryFilters()
            .Where(i => originalItemIds.Contains(i.Id))
            .ToListAsync();
        allItems.Should().BeEmpty("旧药材项应从数据库物理删除");
    }

    #endregion

    #region 场景5: 医案编号自动生成

    [Fact]
    public async Task CreateMedicalCase_AutoGeneratesCaseNumber_CorrectFormat()
    {
        // Arrange
        var (_, ds, _) = CreateTestContext();

        // Act - 创建第一个医案
        var mc1 = BuildBaseMedicalCaseInput();
        var created1 = await ds.CreateAsync(mc1);

        // Act - 创建第二个医案
        var mc2 = BuildBaseMedicalCaseInput();
        var created2 = await ds.CreateAsync(mc2);

        // Act - 创建第三个医案
        var mc3 = BuildBaseMedicalCaseInput();
        var created3 = await ds.CreateAsync(mc3);

        // Assert - 编号格式: MC + YYYYMMDD + 3位序号
        var today = DateTime.Today.ToString("yyyyMMdd");

        created1.CaseNumber.Should().NotBeNullOrEmpty("医案编号应自动生成");
        created1.CaseNumber.Should().StartWith("MC", "编号前缀为 MC");
        created1.CaseNumber.Should().Contain(today, "编号应包含当天日期");
        created1.CaseNumber.Should().Be($"MC{today}001", "第一个医案序号为 001");

        created2.CaseNumber.Should().Be($"MC{today}002", "第二个医案序号为 002");
        created3.CaseNumber.Should().Be($"MC{today}003", "第三个医案序号为 003");

        // 验证编号总长度: MC(2) + YYYYMMDD(8) + NNN(3) = 13
        created1.CaseNumber!.Length.Should().Be(13, "医案编号固定13位");
    }

    [Fact]
    public async Task CreateMedicalCase_CaseNumberSequence_IsMonotonicallyIncreasing()
    {
        // Arrange - 创建多个医案验证序号递增
        var (_, ds, _) = CreateTestContext();

        // Act
        var cases = new List<MedicalCaseDetailDto>();
        for (int i = 0; i < 5; i++)
        {
            var mc = BuildBaseMedicalCaseInput();
            cases.Add(await ds.CreateAsync(mc));
        }

        // Assert - 序号单调递增
        for (int i = 1; i < cases.Count; i++)
        {
            var prevNum = int.Parse(cases[i - 1].CaseNumber!.Substring(10)); // 提取3位序号
            var currNum = int.Parse(cases[i].CaseNumber!.Substring(10));
            currNum.Should().Be(prevNum + 1,
                $"第{i + 1}个医案序号应比第{i}个大1");
        }
    }

    #endregion

    #region 场景6: 同一患者多个医案

    [Fact]
    public async Task MultipleMedicalCases_SamePatient_IndependentAggregates()
    {
        // Arrange - 同一患者复诊场景
        var (_, ds, db) = CreateTestContext();
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 首诊医案
        var mc1 = BuildBaseMedicalCaseInput(patientId: patientId, userId: userId);
        mc1.Consultation = BuildConsultationInput(
            presentIllness: "首诊：咳嗽一周",
            tcmDiagnosis: "风寒犯肺");
        mc1.NeedsPrescription = true;
        mc1.Prescription = BuildPrescriptionInput(new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "麻黄",    // 辛温解表
                Dosage = 6,
                Unit = "g",
                UnitPrice = 2.0m,
            },
        });

        // 复诊医案（同一患者，不同诊断）
        var mc2 = BuildBaseMedicalCaseInput(patientId: patientId, userId: userId);
        mc2.Consultation = BuildConsultationInput(
            presentIllness: "复诊：咳嗽减轻，转为干咳",
            tcmDiagnosis: "阴虚燥咳");
        mc2.NeedsPrescription = true;
        mc2.Prescription = BuildPrescriptionInput(new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "沙参",    // 养阴润肺
                Dosage = 15,
                Unit = "g",
                UnitPrice = 6.0m,
            },
            new()
            {
                HerbId = Guid.NewGuid(),
                HerbName = "麦冬",    // 滋阴润燥
                Dosage = 12,
                Unit = "g",
                UnitPrice = 5.0m,
            },
        });

        // Act
        var created1 = await ds.CreateAsync(mc1);
        var created2 = await ds.CreateAsync(mc2);

        // Assert - 两个医案独立存在
        created1.Id.Should().NotBe(created2.Id, "两次就诊应生成不同的医案 ID");
        created1.PatientId.Should().Be(created2.PatientId, "同一患者的 PatientId 相同");

        // 通过 DataSource 按患者查询
        var patientCases = await ds.GetByPatientIdAsync(patientId);
        patientCases.Should().HaveCount(2, "同一患者应有2个医案");

        // 验证各自的诊断独立
        var detail1 = await ds.GetWithDetailsAsync(created1.Id);
        var detail2 = await ds.GetWithDetailsAsync(created2.Id);

        detail1!.Consultation!.TcmDiagnosis.Should().Be("风寒犯肺");
        detail2!.Consultation!.TcmDiagnosis.Should().Be("阴虚燥咳");

        // 验证各自的处方独立
        detail1.Prescription!.Items.Should().HaveCount(1);
        detail1.Prescription.Items.First().HerbName.Should().Be("麻黄");

        detail2.Prescription!.Items.Should().HaveCount(2);
        detail2.Prescription.Items.Select(i => i.HerbName).Should().Contain("沙参");
        detail2.Prescription.Items.Select(i => i.HerbName).Should().Contain("麦冬");

        // 共享主键独立验证
        detail1.Consultation.Id.Should().Be(created1.Id);
        detail2.Consultation.Id.Should().Be(created2.Id);

        // 数据库层验证: 总计2条 MedicalCase, 2条 Consultation, 2条 Prescription
        var totalMc = await db.MedicalCases.AsNoTracking().CountAsync();
        var totalConsultation = await db.Consultations.AsNoTracking().CountAsync();
        var totalPrescription = await db.Prescriptions.AsNoTracking().CountAsync();
        var totalItems = await db.PrescriptionItems.AsNoTracking().CountAsync();

        totalMc.Should().Be(2);
        totalConsultation.Should().Be(2);
        totalPrescription.Should().Be(2);
        totalItems.Should().Be(3, "首诊1味药 + 复诊2味药 = 3");
    }

    #endregion

    #region 场景7: NeedsPrescription=false 不应创建空处方

    [Fact]
    public async Task CreateMedicalCase_NeedsPrescriptionFalse_NoPrescriptionCreated()
    {
        // Arrange - 仅诊断、不开方的医案（中医常见：先诊断，后续再决定是否开方）
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.NeedsPrescription = false;
        mc.Consultation = BuildConsultationInput(
            presentIllness: "失眠一月",
            tcmDiagnosis: "心脾两虚");
        // 明确不附加 Prescription 对象

        // Act
        var created = await ds.CreateAsync(mc);

        // Assert - 医案和诊断应存在，处方不应存在
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();
        dbMc!.NeedsPrescription.Should().BeFalse("应标记为不需要处方");

        // 诊断应正常创建
        dbMc.Consultation.Should().NotBeNull("即使不开方，诊断也应存在");
        dbMc.Consultation!.TcmDiagnosis.Should().Be("心脾两虚");

        // 核心断言: 不应创建空处方
        dbMc.Prescription.Should().BeNull(
            "NeedsPrescription=false 且未提供 Prescription 时，不应创建空处方记录");

        // 数据库层验证: Prescriptions 表应无记录
        var prescriptionCount = await db.Prescriptions
            .AsNoTracking()
            .CountAsync(p => p.MedicalCaseId == created.Id);
        prescriptionCount.Should().Be(0, "数据库中不应有该医案的处方记录");
    }

    [Fact]
    public async Task CreateMedicalCase_NeedsPrescriptionNull_NoPrescriptionCreated()
    {
        // Arrange - 未标记是否需要处方（用户还未做决策的中间状态）
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.NeedsPrescription = null; // 未决策
        mc.Consultation = BuildConsultationInput(tcmDiagnosis: "待辨证");
        // 不附加 Prescription

        // Act
        var created = await ds.CreateAsync(mc);

        // Assert
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Prescription)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();
        dbMc!.NeedsPrescription.Should().BeNull("未决策状态应保持 null");
        dbMc.Prescription.Should().BeNull("未决策时也不应创建处方");
    }

    #endregion

    #region 补充: 聚合完整性验证

    [Fact]
    public async Task CreateMedicalCase_StatusDefaultsToDraft_AfterDataSourceCreate()
    {
        // Arrange - DataSource.CreateAsync 内部会将状态设为 Draft
        var (_, ds, _) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();

        // Act
        var created = await ds.CreateAsync(mc);

        // Assert - DataSource 内部强制设为 Suspended
        created.CaseStatus.Should().Be(MedicalCaseStatus.Suspended,
            "CreateAsync 内部会将状态重置为 Suspended，无论传入什么值");
    }

    [Fact]
    public async Task GetWithDetails_ReturnsCompleteAggregate_IncludingAllNavigationProperties()
    {
        // Arrange - 创建完整聚合（MedicalCase + Consultation + Prescription + Items）
        var (_, ds, _) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.Consultation = BuildConsultationInput();
        mc.NeedsPrescription = true;
        mc.Prescription = BuildPrescriptionInput();
        var created = await ds.CreateAsync(mc);

        // Act - 通过 GetWithDetailsAsync 加载完整聚合
        var detail = await ds.GetWithDetailsAsync(created.Id);

        // Assert - 验证所有导航属性均已加载
        detail.Should().NotBeNull();

        detail!.Consultation.Should().NotBeNull("Consultation 应通过 Include 加载");
        detail.Consultation!.Id.Should().Be(detail.Id, "共享主键验证");

        detail.Prescription.Should().NotBeNull("Prescription 应通过 Include 加载");
        detail.Prescription!.MedicalCaseId.Should().Be(detail.Id);

        detail.Prescription.Items.Should().NotBeNullOrEmpty("PrescriptionItems 应通过 ThenInclude 加载");
        detail.Prescription.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateMedicalCase_AddPrescriptionToExistingCase_CreatesNewPrescription()
    {
        // Arrange - 先创建仅有诊断的医案（后续再开方的常见场景）
        var (_, ds, db) = CreateTestContext();

        var mc = BuildBaseMedicalCaseInput();
        mc.Consultation = BuildConsultationInput(tcmDiagnosis: "脾虚湿盛");
        // 初始不带处方
        var created = await ds.CreateAsync(mc);

        // 验证初始状态无处方
        var initial = await ds.GetWithDetailsAsync(created.Id);
        initial!.Prescription.Should().BeNull("初始创建时无处方");

        // Act - 后续追加处方（医生复查后决定开方）
        var updateInput = ToInputDto(initial);
        updateInput.NeedsPrescription = true;
        updateInput.Prescription = new PrescriptionInputDto
        {
            MedicalCaseId = created.Id,
            DosageCount = 7,
            Usage = "每日一剂，水煎服",
            Items = new List<PrescriptionItemInputDto>
            {
                new()
                {
                    HerbId = Guid.NewGuid(),
                    HerbName = "茯苓",    // 健脾利水
                    Dosage = 15,
                    Unit = "g",
                    UnitPrice = 3.0m,
                },
                new()
                {
                    HerbId = Guid.NewGuid(),
                    HerbName = "白术",    // 健脾燥湿
                    Dosage = 12,
                    Unit = "g",
                    UnitPrice = 3.5m,
                },
            }
        };

        await ds.UpdateAsync(updateInput);

        // Assert - 处方已添加
        var dbMc = await db.MedicalCases
            .AsNoTracking()
            .Include(m => m.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(m => m.Id == created.Id);

        dbMc.Should().NotBeNull();
        dbMc!.Prescription.Should().NotBeNull("更新后应有处方");
        dbMc.Prescription!.MedicalCaseId.Should().Be(created.Id);
        dbMc.Prescription.Items.Should().HaveCount(2, "应有2味药");

        var herbNames = dbMc.Prescription.Items.Select(i => i.HerbName).ToList();
        herbNames.Should().Contain("茯苓");
        herbNames.Should().Contain("白术");
    }

    #endregion
}
