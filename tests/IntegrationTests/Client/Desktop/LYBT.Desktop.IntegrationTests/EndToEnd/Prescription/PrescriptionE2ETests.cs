using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.IntegrationTests.LocalMode.Fixtures;
using LYBT.Desktop.LocalData.Context;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.IntegrationTests.EndToEnd.Prescription;

/// <summary>
/// 处方 E2E 集成测试
/// 测试层: DataSource -> LocalDbContext (SQLite InMemory)
/// 处方始终作为 MedicalCase 聚合的一部分，通过 IMedicalCaseDataSource 操作
/// </summary>
public class PrescriptionE2ETests : IClassFixture<LocalModeTestFixture>
{
    private readonly LocalModeTestFixture _fixture;

    public PrescriptionE2ETests(LocalModeTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region 辅助方法

    /// <summary>
    /// 创建测试用药材（处方项需要引用真实的 HerbId）
    /// </summary>
    private static async Task<List<HerbDetailDto>> SeedHerbsAsync(IHerbDataSource herbDataSource)
    {
        var herbs = new List<HerbDetailDto>();

        // 黄芪 - 补气药
        herbs.Add(await herbDataSource.CreateAsync(new HerbInputDto
        {
            Name = "黄芪",
            PinYinCode = "HQ",
            Category = "补气药",
            Unit = "g",
            Price = 3.5m,
            Effect = "补气固表"
        }));

        // 党参 - 补气药
        herbs.Add(await herbDataSource.CreateAsync(new HerbInputDto
        {
            Name = "党参",
            PinYinCode = "DS",
            Category = "补气药",
            Unit = "g",
            Price = 5.0m,
            Effect = "补中益气"
        }));

        // 白术 - 补气药
        herbs.Add(await herbDataSource.CreateAsync(new HerbInputDto
        {
            Name = "白术",
            PinYinCode = "BZ",
            Category = "补气药",
            Unit = "g",
            Price = 2.8m,
            Effect = "健脾益气"
        }));

        // 当归 - 补血药
        herbs.Add(await herbDataSource.CreateAsync(new HerbInputDto
        {
            Name = "当归",
            PinYinCode = "DG",
            Category = "补血药",
            Unit = "g",
            Price = 6.0m,
            Effect = "补血活血"
        }));

        // 附子 - 温里药（先煎）
        herbs.Add(await herbDataSource.CreateAsync(new HerbInputDto
        {
            Name = "附子",
            PinYinCode = "FZ",
            Category = "温里药",
            Unit = "g",
            Price = 8.0m,
            Effect = "回阳救逆"
        }));

        return herbs;
    }

    /// <summary>
    /// 创建包含处方的医案输入DTO
    /// </summary>
    private static MedicalCaseInputDto BuildMedicalCaseWithPrescription(
        Guid patientId,
        Guid userId,
        List<PrescriptionItemInputDto> items,
        int dosageCount = 7,
        string? usage = "每日一剂，水煎服",
        string? referencedFormulas = null)
    {
        return new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = userId,
            NeedsPrescription = true,
            Prescription = new PrescriptionInputDto
            {
                DosageCount = dosageCount,
                Usage = usage,
                Discount = 1.0m,
                ReferencedFormulas = referencedFormulas,
                Items = items
            }
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

    #region 场景1: 创建含多味药材的处方（验证 HerbId 引用有效性）

    [Fact]
    public async Task CreatePrescription_WithMultipleHerbItems_ShouldPersistAllItems()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        sp.GetRequiredService<LocalDbContext>().Database.EnsureCreated();

        var herbDataSource = sp.GetRequiredService<IHerbDataSource>();
        var mcDataSource = sp.GetRequiredService<IMedicalCaseDataSource>();

        // 创建药材基础数据
        var herbs = await SeedHerbsAsync(herbDataSource);
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 构建处方项 - 三味药组成（四君子汤基础方去甘草）
        var items = new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = herbs[0].Id,   // 黄芪
                HerbName = "黄芪",
                Dosage = 30,
                Unit = "g",
                UnitPrice = herbs[0].Price,
                DecocteMethod = DecocteMethod.Default
            },
            new()
            {
                HerbId = herbs[1].Id,   // 党参
                HerbName = "党参",
                Dosage = 15,
                Unit = "g",
                UnitPrice = herbs[1].Price,
                DecocteMethod = DecocteMethod.Default
            },
            new()
            {
                HerbId = herbs[2].Id,   // 白术
                HerbName = "白术",
                Dosage = 10,
                Unit = "g",
                UnitPrice = herbs[2].Price,
                DecocteMethod = DecocteMethod.Default
            }
        };

        var mc = BuildMedicalCaseWithPrescription(patientId, userId, items);

        // Act - 通过聚合根创建医案（含处方）
        var created = await mcDataSource.CreateAsync(mc);

        // Assert - 通过 GetWithDetails 验证完整聚合数据
        var detail = await mcDataSource.GetWithDetailsAsync(created.Id);
        detail.Should().NotBeNull();
        detail!.Prescription.Should().NotBeNull("医案应包含处方");
        detail.Prescription!.Items.Should().HaveCount(3, "处方应包含3味药材");
        detail.Prescription.DosageCount.Should().Be(7, "默认7帖");
        detail.Prescription.Usage.Should().Be("每日一剂，水煎服");

        // 验证每味药材的 HerbId 引用有效性
        var itemList = detail.Prescription.Items.ToList();
        foreach (var item in itemList)
        {
            item.HerbId.Should().NotBe(Guid.Empty, "HerbId 不能为空");
            herbs.Select(h => h.Id).Should().Contain(item.HerbId,
                $"HerbId {item.HerbId} 应存在于药材库中");
        }

        // 验证药材名称映射正确
        itemList.Should().Contain(i => i.HerbName == "黄芪" && i.Dosage == 30);
        itemList.Should().Contain(i => i.HerbName == "党参" && i.Dosage == 15);
        itemList.Should().Contain(i => i.HerbName == "白术" && i.Dosage == 10);

        // 直接查 DB 验证持久化
        var db = sp.GetRequiredService<LocalDbContext>();
        var dbItems = await db.PrescriptionItems
            .Where(pi => pi.PrescriptionId == detail.Prescription.Id)
            .ToListAsync();
        dbItems.Should().HaveCount(3);
    }

    #endregion

    #region 场景2: 处方药材项的剂量与单价计算

    [Fact]
    public async Task PrescriptionItem_DosageAndUnitPrice_ShouldCalculateAmountCorrectly()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        sp.GetRequiredService<LocalDbContext>().Database.EnsureCreated();

        var herbDataSource = sp.GetRequiredService<IHerbDataSource>();
        var mcDataSource = sp.GetRequiredService<IMedicalCaseDataSource>();

        var herbs = await SeedHerbsAsync(herbDataSource);
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 构建处方项 - 明确设置剂量和单价
        var items = new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = herbs[0].Id,   // 黄芪 3.5元/g
                HerbName = "黄芪",
                Dosage = 30,            // 30g
                Unit = "g",
                UnitPrice = 3.5m,       // 小计 = 3.5 * 30 = 105
                DecocteMethod = DecocteMethod.Default
            },
            new()
            {
                HerbId = herbs[3].Id,   // 当归 6.0元/g
                HerbName = "当归",
                Dosage = 15,            // 15g
                Unit = "g",
                UnitPrice = 6.0m,       // 小计 = 6.0 * 15 = 90
                DecocteMethod = DecocteMethod.Default
            },
            new()
            {
                HerbId = herbs[4].Id,   // 附子 8.0元/g（先煎）
                HerbName = "附子",
                Dosage = 10,            // 10g
                Unit = "g",
                UnitPrice = 8.0m,       // 小计 = 8.0 * 10 = 80
                DecocteMethod = DecocteMethod.PreDecoct
            }
        };

        var mc = BuildMedicalCaseWithPrescription(patientId, userId, items, dosageCount: 3);

        // Act
        var created = await mcDataSource.CreateAsync(mc);

        // Assert
        var detail = await mcDataSource.GetWithDetailsAsync(created.Id);
        detail.Should().NotBeNull();
        detail!.Prescription.Should().NotBeNull();
        detail.Prescription!.DosageCount.Should().Be(3, "3帖");

        var itemList = detail.Prescription.Items.ToList();

        // 验证 PrescriptionItem 小计: UnitPrice * Dosage
        var huangqi = itemList.First(i => i.HerbName == "黄芪");
        huangqi.Subtotal.Should().Be(105m, "黄芪小计 = 3.5 * 30 = 105");

        var danggui = itemList.First(i => i.HerbName == "当归");
        danggui.Subtotal.Should().Be(90m, "当归小计 = 6.0 * 15 = 90");

        var fuzi = itemList.First(i => i.HerbName == "附子");
        fuzi.Subtotal.Should().Be(80m, "附子小计 = 8.0 * 10 = 80");
        fuzi.DecocteMethod.Should().Be(DecocteMethod.PreDecoct, "附子应为先煎");

        // 一帖总价 = 105 + 90 + 80 = 275
        var singleDoseTotal = itemList.Sum(i => i.Subtotal);
        singleDoseTotal.Should().Be(275m, "一帖总价 = 275元");
    }

    #endregion

    #region 场景3: 验方导入到处方（Formula -> PrescriptionItems 数据转换）

    [Fact]
    public async Task FormulaImportToPrescription_ShouldConvertHerbItemsCorrectly()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        sp.GetRequiredService<LocalDbContext>().Database.EnsureCreated();

        var herbDataSource = sp.GetRequiredService<IHerbDataSource>();
        var formulaDataSource = sp.GetRequiredService<IFormulaDataSource>();
        var mcDataSource = sp.GetRequiredService<IMedicalCaseDataSource>();

        // 1. 创建药材基础数据
        var herbs = await SeedHerbsAsync(herbDataSource);

        // 2. 创建验方（四君子汤），包含药材组成
        var formula = await formulaDataSource.CreateAsync(new FormulaInputDto
        {
            Name = "四君子汤",
            Effect = "益气健脾",
            Indications = "脾胃气虚证",
            Usage = "水煎温服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new()
                {
                    HerbId = herbs[0].Id,   // 黄芪
                    HerbName = "黄芪",
                    Dosage = 15,
                    Unit = "g",
                    DecocteMethod = DecocteMethod.Default
                },
                new()
                {
                    HerbId = herbs[1].Id,   // 党参
                    HerbName = "党参",
                    Dosage = 10,
                    Unit = "g",
                    DecocteMethod = DecocteMethod.Default
                },
                new()
                {
                    HerbId = herbs[2].Id,   // 白术
                    HerbName = "白术",
                    Dosage = 10,
                    Unit = "g",
                    DecocteMethod = DecocteMethod.Default
                }
            }
        });

        // 3. 读取验方（含药材明细）
        var formulaWithHerbs = await formulaDataSource.GetWithHerbsAsync(formula.Id);
        formulaWithHerbs.Should().NotBeNull();
        formulaWithHerbs!.Herbs.Should().HaveCount(3);

        // 4. 模拟"验方导入到处方"：将 FormulaHerbItemDto 转换为 PrescriptionItemInputDto
        //    实际业务中由 PrescriptionImportHandler/PrescriptionImportExtensions 完成
        //    此处验证数据转换的正确性
        var prescriptionItems = formulaWithHerbs.Herbs.Select(fh => new PrescriptionItemInputDto
        {
            HerbId = fh.HerbId ?? Guid.Empty,
            HerbName = fh.HerbName,
            Dosage = fh.Dosage,
            Unit = fh.Unit,
            // 从药材库获取单价（验方不含价格，处方需要价格用于收费）
            UnitPrice = herbs.FirstOrDefault(h => h.Id == fh.HerbId)?.Price ?? 0m,
            DecocteMethod = fh.DecocteMethod
        }).ToList();

        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var mc = BuildMedicalCaseWithPrescription(
            patientId, userId, prescriptionItems,
            usage: formulaWithHerbs.Usage,
            referencedFormulas: formulaWithHerbs.Name);

        // Act - 创建含导入处方的医案
        var created = await mcDataSource.CreateAsync(mc);

        // Assert
        var detail = await mcDataSource.GetWithDetailsAsync(created.Id);
        detail.Should().NotBeNull();
        detail!.Prescription.Should().NotBeNull();

        // 验证引用验方名称记录
        detail.Prescription!.ReferencedFormulas.Should().Be("四君子汤");
        detail.Prescription.Usage.Should().Be("水煎温服", "用法应从验方继承");

        // 验证处方项与验方药材一一对应
        var rxItems = detail.Prescription.Items.ToList();
        rxItems.Should().HaveCount(3, "处方项数量应与验方药材数量一致");

        // 验证每项数据转换正确
        var rxHuangqi = rxItems.First(i => i.HerbName == "黄芪");
        rxHuangqi.Dosage.Should().Be(15, "剂量应从验方继承");
        rxHuangqi.HerbId.Should().Be(herbs[0].Id, "HerbId 应从验方继承");
        rxHuangqi.UnitPrice.Should().Be(3.5m, "单价应从药材库获取");

        var rxDangshen = rxItems.First(i => i.HerbName == "党参");
        rxDangshen.Dosage.Should().Be(10);
        rxDangshen.UnitPrice.Should().Be(5.0m);

        var rxBaizhu = rxItems.First(i => i.HerbName == "白术");
        rxBaizhu.Dosage.Should().Be(10);
        rxBaizhu.UnitPrice.Should().Be(2.8m);
    }

    #endregion

    #region 场景4: 修改处方（替换药材、调整剂量）

    [Fact]
    public async Task ModifyPrescription_ReplaceHerbAndAdjustDosage_ShouldUpdateCorrectly()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        sp.GetRequiredService<LocalDbContext>().Database.EnsureCreated();

        var herbDataSource = sp.GetRequiredService<IHerbDataSource>();
        var mcDataSource = sp.GetRequiredService<IMedicalCaseDataSource>();

        var herbs = await SeedHerbsAsync(herbDataSource);
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 初始处方：黄芪30g + 党参15g
        var initialItems = new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = herbs[0].Id,
                HerbName = "黄芪",
                Dosage = 30,
                Unit = "g",
                UnitPrice = 3.5m
            },
            new()
            {
                HerbId = herbs[1].Id,
                HerbName = "党参",
                Dosage = 15,
                Unit = "g",
                UnitPrice = 5.0m
            }
        };

        var mc = BuildMedicalCaseWithPrescription(patientId, userId, initialItems);
        var created = await mcDataSource.CreateAsync(mc);

        // 验证初始状态
        var initial = await mcDataSource.GetWithDetailsAsync(created.Id);
        initial!.Prescription!.Items.Should().HaveCount(2);

        // Act - 修改处方：
        //   1. 黄芪剂量调整 30g -> 45g（加量）
        //   2. 移除党参，替换为当归15g
        //   3. 新增附子10g（先煎）
        //   4. 帖数从7改为5
        var updateInput = ToInputDto(initial);
        updateInput.Prescription!.DosageCount = 5;
        updateInput.Prescription.Items = new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = herbs[0].Id,
                HerbName = "黄芪",
                Dosage = 45,            // 剂量调整
                Unit = "g",
                UnitPrice = 3.5m
            },
            new()
            {
                HerbId = herbs[3].Id,   // 当归替换党参
                HerbName = "当归",
                Dosage = 15,
                Unit = "g",
                UnitPrice = 6.0m
            },
            new()
            {
                HerbId = herbs[4].Id,   // 新增附子
                HerbName = "附子",
                Dosage = 10,
                Unit = "g",
                UnitPrice = 8.0m,
                DecocteMethod = DecocteMethod.PreDecoct
            }
        };

        await mcDataSource.UpdateAsync(updateInput);

        // Assert
        var updated = await mcDataSource.GetWithDetailsAsync(created.Id);
        updated.Should().NotBeNull();
        updated!.Prescription.Should().NotBeNull();
        updated.Prescription!.DosageCount.Should().Be(5, "帖数应更新为5");

        var rxItems = updated.Prescription.Items.ToList();
        rxItems.Should().HaveCount(3, "修改后应有3味药材");

        // 验证黄芪剂量已调整
        var huangqi = rxItems.First(i => i.HerbName == "黄芪");
        huangqi.Dosage.Should().Be(45, "黄芪剂量应从30调整到45");

        // 验证党参已被替换为当归
        rxItems.Should().NotContain(i => i.HerbName == "党参", "党参应已被移除");
        var danggui = rxItems.First(i => i.HerbName == "当归");
        danggui.HerbId.Should().Be(herbs[3].Id);

        // 验证新增的附子
        var fuzi = rxItems.First(i => i.HerbName == "附子");
        fuzi.DecocteMethod.Should().Be(DecocteMethod.PreDecoct);

        // 直接查 DB 验证旧的处方项已被删除
        var db = sp.GetRequiredService<LocalDbContext>();
        var allDbItems = await db.PrescriptionItems
            .Where(pi => pi.PrescriptionId == updated.Prescription.Id)
            .ToListAsync();
        allDbItems.Should().HaveCount(3, "DB 中应只有3条处方项（旧的已被替换）");
        allDbItems.Should().NotContain(i => i.HerbName == "党参");
    }

    #endregion

    #region 场景5: NeedsPrescription=false 不应创建空处方

    [Fact]
    public async Task NeedsPrescriptionFalse_ShouldNotCreateEmptyPrescription()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        sp.GetRequiredService<LocalDbContext>().Database.EnsureCreated();

        var mcDataSource = sp.GetRequiredService<IMedicalCaseDataSource>();

        // 创建医案时不附带处方，且标记 NeedsPrescription=false
        var mc = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            NeedsPrescription = false,
            // 不设置 Prescription
            Prescription = null
        };

        // Act
        var created = await mcDataSource.CreateAsync(mc);

        // Assert - 医案应正常创建
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);

        // GetWithDetails 应返回无处方的医案
        var detail = await mcDataSource.GetWithDetailsAsync(created.Id);
        detail.Should().NotBeNull();
        detail!.Prescription.Should().BeNull("NeedsPrescription=false 时不应创建处方");

        // 直接查 DB 确认没有处方记录
        var db = sp.GetRequiredService<LocalDbContext>();
        var prescriptionCount = await db.Prescriptions
            .CountAsync(p => p.MedicalCaseId == created.Id);
        prescriptionCount.Should().Be(0, "数据库中不应存在关联的处方记录");

        // 确认处方项也不存在
        var itemCount = await db.PrescriptionItems.CountAsync();
        itemCount.Should().Be(0, "数据库中不应存在任何处方项");
    }

    #endregion
}
