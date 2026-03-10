using LYBT.Entities.Consultations;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Features.Infrastructure;

/// <summary>
/// 性能测试数据播种器。
/// 直接通过 DbContext 批量插入，绕过 API 层以快速创建大量测试数据。
///
/// 目标数据集 (对齐 NFR-DATA-001):
///   - 5000 患者
///   - 200 药材
///   - 25000 医案 (含 Consultation，60% 含 Prescription + Items)
/// </summary>
public static class PerformanceDataSeeder
{
    // 常量: 中医常用诊断/药材模板
    private static readonly string[] TcmDiagnoses =
    [
        "气虚体倦", "血虚头晕", "阴虚火旺", "阳虚畏寒", "湿热蕴结",
        "痰湿内蕴", "气滞血瘀", "肝郁脾虚", "心肾不交", "脾肾阳虚",
        "肺阴不足", "胃阴亏虚", "肝肾阴虚", "气血两虚", "风寒表证"
    ];

    private static readonly string[] HerbNames =
    [
        "黄芪", "党参", "白术", "茯苓", "甘草", "当归", "熟地黄", "白芍",
        "川芎", "陈皮", "半夏", "柴胡", "黄芩", "金银花", "连翘", "板蓝根",
        "桔梗", "枳壳", "厚朴", "苍术", "薏苡仁", "泽泻", "猪苓", "桂枝",
        "麻黄", "杏仁", "石膏", "知母", "生地黄", "牡丹皮", "栀子", "淡竹叶",
        "龙胆草", "车前子", "木通", "黄柏", "苦参", "地肤子", "防风", "荆芥",
        "薄荷", "蝉蜕", "僵蚕", "天麻", "钩藤", "石决明", "牡蛎", "龙骨",
        "酸枣仁", "远志"
    ];

    private static readonly string[] PresentIllnesses =
    [
        "患者近日感到疲乏无力，食欲不振，面色萎黄",
        "头晕目眩反复发作，伴心悸失眠多梦",
        "咳嗽痰多色白，胸闷气短，畏寒肢冷",
        "腰膝酸软，五心烦热，口干咽燥，盗汗",
        "脘腹胀满，纳呆便溏，肢体困重，口淡不渴",
        "胸胁胀痛，情志不畅，善太息，月经不调",
        "心烦不寐，口舌生疮，小便短赤",
        "关节疼痛肿胀，屈伸不利，遇寒加重"
    ];

    private static readonly string[] FamilyNames =
    [
        "王", "李", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴",
        "徐", "孙", "胡", "朱", "高", "林", "何", "郭", "马", "罗"
    ];

    private static readonly string[] GivenNames =
    [
        "伟", "芳", "娜", "敏", "静", "丽", "强", "磊", "洋", "艳",
        "勇", "军", "杰", "娟", "涛", "明", "超", "秀英", "华", "平"
    ];

    /// <summary>
    /// 播种完整性能测试数据集。
    /// 使用批量插入 + SaveChanges 分批提交，避免内存溢出。
    /// </summary>
    public static async Task SeedAsync(
        AppDbContext db,
        Guid doctorUserId,
        int patientCount = 5000,
        int herbCount = 200,
        int medicalCaseCount = 25000,
        ITestOutputHelper? output = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 1. 播种药材
        var herbIds = await SeedHerbsAsync(db, herbCount, doctorUserId, output);

        // 2. 播种患者
        var patientData = await SeedPatientsAsync(db, patientCount, doctorUserId, output);

        // 3. 播种医案 (含 Consultation + Prescription)
        await SeedMedicalCasesAsync(db, patientData, herbIds, doctorUserId,
            medicalCaseCount, output);

        sw.Stop();
        output?.WriteLine($"[Seeder] 全部数据播种完成: {sw.Elapsed.TotalSeconds:F1}s");
    }

    private static async Task<List<Guid>> SeedHerbsAsync(
        AppDbContext db, int count, Guid createdBy, ITestOutputHelper? output)
    {
        var herbIds = new List<Guid>(count);
        var now = DateTime.UtcNow;

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            herbIds.Add(id);
            var nameIndex = i % HerbNames.Length;
            var suffix = i >= HerbNames.Length ? $"_{i / HerbNames.Length}" : "";

            db.Set<Herb>().Add(new Herb
            {
                Id = id,
                Name = $"{HerbNames[nameIndex]}{suffix}",
                PinYinCode = $"HB{i:D4}",
                Category = (i % 5) switch
                {
                    0 => "补气药",
                    1 => "补血药",
                    2 => "清热药",
                    3 => "解表药",
                    _ => "其他"
                },
                Unit = "克",
                Price = 0.5m + (i % 50) * 0.3m,
                Status = CommonStatus.Enabled,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            });
        }

        await db.SaveChangesAsync();
        output?.WriteLine($"[Seeder] 药材: {count} 条");
        return herbIds;
    }

    private static async Task<List<(Guid Id, string Name)>> SeedPatientsAsync(
        AppDbContext db, int count, Guid createdBy, ITestOutputHelper? output)
    {
        var patients = new List<(Guid Id, string Name)>(count);
        var now = DateTime.UtcNow;
        const int batchSize = 1000;

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            var familyName = FamilyNames[i % FamilyNames.Length];
            var givenName = GivenNames[i % GivenNames.Length];
            var name = $"{familyName}{givenName}";
            patients.Add((id, name));

            db.Set<Patient>().Add(new Patient
            {
                Id = id,
                Name = name,
                PinYinCode = $"PY{i:D5}",
                Gender = i % 3 == 0 ? Gender.Female : Gender.Male,
                BirthDate = new DateTime(1950 + (i % 50), (i % 12) + 1, (i % 28) + 1),
                PhoneNumber = $"13{800000000 + i}",
                Status = CommonStatus.Enabled,
                VisitCount = 0,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            });

            // 分批提交
            if ((i + 1) % batchSize == 0)
            {
                await db.SaveChangesAsync();
                // 清除 ChangeTracker 释放内存
                db.ChangeTracker.Clear();
            }
        }

        // 提交剩余
        if (count % batchSize != 0)
        {
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        output?.WriteLine($"[Seeder] 患者: {count} 条");
        return patients;
    }

    private static async Task SeedMedicalCasesAsync(
        AppDbContext db,
        List<(Guid Id, string Name)> patients,
        List<Guid> herbIds,
        Guid doctorUserId,
        int totalCases,
        ITestOutputHelper? output)
    {
        var now = DateTime.UtcNow;
        const int batchSize = 500;
        var rng = new Random(42); // 固定种子保证可重复

        // 追踪已分配 Active 医案的患者 (唯一索引: PatientId + CaseStatus=Active + IsDeleted=0)
        var patientsWithActiveCase = new HashSet<Guid>();

        for (var i = 0; i < totalCases; i++)
        {
            var patient = patients[i % patients.Count];
            var caseId = Guid.NewGuid();
            var hasPrescription = i % 5 < 3; // 60% 有处方

            // 确保每个患者最多一个 Active 医案 (BR-001 业务规则)
            var wantsActive = i % 4 == 0;
            var isCompleted = !(wantsActive && patientsWithActiveCase.Add(patient.Id));

            // MedicalCase
            var mc = new MedicalCase
            {
                Id = caseId,
                PatientId = patient.Id,
                PatientName = patient.Name,
                UserId = doctorUserId,
                DoctorName = "测试医生",
                CaseNumber = $"MC{now:yyyyMMdd}{i:D5}",
                CaseStatus = isCompleted ? MedicalCaseStatus.Completed : MedicalCaseStatus.Active,
                NeedsPrescription = hasPrescription,
                CompletedAt = isCompleted ? now.AddMinutes(-rng.Next(1, 10000)) : null,
                CreatedAt = now.AddMinutes(-rng.Next(1, 100000)),
                UpdatedAt = now,
                CreatedBy = doctorUserId,
                UpdatedBy = doctorUserId
            };
            db.Set<MedicalCase>().Add(mc);

            // Consultation (1:1, 共享主键)
            db.Set<Consultation>().Add(new Consultation
            {
                Id = caseId,
                PresentIllness = PresentIllnesses[i % PresentIllnesses.Length],
                TongueDiagnosis = "舌红苔薄白",
                PulseDiagnosis = "脉弦细",
                TcmDiagnosis = TcmDiagnoses[i % TcmDiagnoses.Length],
                CreatedAt = mc.CreatedAt,
                UpdatedAt = now,
                CreatedBy = doctorUserId,
                UpdatedBy = doctorUserId
            });

            // Prescription + Items (60%)
            if (hasPrescription)
            {
                var prescriptionId = Guid.NewGuid();
                db.Set<Prescription>().Add(new Prescription
                {
                    Id = prescriptionId,
                    MedicalCaseId = caseId,
                    PrescriptionNumber = $"RX-{now:yyyyMMdd}-{i:D4}",
                    DosageCount = 7,
                    Discount = 1.0m,
                    Usage = "每日一剂，水煎服",
                    CreatedAt = mc.CreatedAt,
                    UpdatedAt = now,
                    CreatedBy = doctorUserId,
                    UpdatedBy = doctorUserId
                });

                // 每处方 5-10 味药材
                var itemCount = 5 + rng.Next(6);
                for (var j = 0; j < itemCount; j++)
                {
                    var herbIndex = (i * 7 + j) % herbIds.Count;
                    db.Set<PrescriptionItem>().Add(new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = prescriptionId,
                        HerbId = herbIds[herbIndex],
                        HerbName = HerbNames[herbIndex % HerbNames.Length],
                        Dosage = 5 + rng.Next(26),
                        Unit = "g",
                        UnitPrice = 0.5m + (herbIndex % 50) * 0.3m,
                        DecocteMethod = DecocteMethod.Default
                    });
                }
            }

            // 分批提交
            if ((i + 1) % batchSize == 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                if ((i + 1) % 5000 == 0)
                {
                    output?.WriteLine($"[Seeder] 医案进度: {i + 1}/{totalCases}");
                }
            }
        }

        // 提交剩余
        if (totalCases % batchSize != 0)
        {
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        output?.WriteLine($"[Seeder] 医案: {totalCases} 条 (含 {totalCases * 60 / 100} 处方)");
    }
}
