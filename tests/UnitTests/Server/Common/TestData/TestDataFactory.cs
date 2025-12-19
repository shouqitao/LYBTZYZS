using LYBT.Shared.Models.Enums;

namespace LYBT.Server.Tests.Common.TestData;

/// <summary>
/// 测试数据工厂
/// 生成标准的测试实体数据
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// 创建测试用户
    /// </summary>
    public static User CreateUser(Guid? id = null, string? username = null, UserRole role = UserRole.Doctor, CommonStatus status = CommonStatus.Enabled)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username ?? $"testuser_{Guid.NewGuid():N}",
            RealName = $"测试用户_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@lybt.test",
            PhoneNumber = $"138{new Random().Next(10000000, 99999999)}",
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试患者
    /// </summary>
    public static Patient CreatePatient(Guid? id = null, string? name = null, Gender gender = Gender.Male, int? age = null)
    {
        return new Patient
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试患者_{Guid.NewGuid():N}",
            Gender = gender,
            Age = age ?? new Random().Next(1, 80),
            PhoneNumber = $"139{new Random().Next(10000000, 99999999)}",
            Address = $"测试地址_{Guid.NewGuid():N}",
            MedicalHistory = $"测试病史_{Guid.NewGuid():N}",
            Allergies = $"测试过敏史_{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试中药
    /// </summary>
    public static Herb CreateHerb(Guid? id = null, string? name = null, string? category = null, decimal? price = null)
    {
        return new Herb
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试中药_{Guid.NewGuid():N}",
            Pinyin = $"caoyao_{Guid.NewGuid():N}",
            Category = category ?? "补益药",
            Properties = $"性味：甘，平",
            Efficacy = $"功效：补气养血_{Guid.NewGuid():N}",
            Usage = $"用法：水煎服_{Guid.NewGuid():N}",
            Dosage = "9-15g",
            Contraindications = $"禁忌：外感发热者慎用_{Guid.NewGuid():N}",
            Price = price ?? (decimal)(new Random().NextDouble() * 100 + 10),
            Stock = new Random().Next(100, 1000),
            Unit = "g",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试处方
    /// OpenSpec: simplify-medicalcase-dataflow - Prescription作为MedicalCase的子实体
    /// </summary>
    public static Prescription CreatePrescription(Guid? id = null, Guid? medicalCaseId = null)
    {
        var mcId = medicalCaseId ?? Guid.NewGuid();

        return new Prescription
        {
            Id = id ?? Guid.NewGuid(),
            MedicalCaseId = mcId,
            PrescriptionNumber = $"RX-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            DosageCount = 7,
            Usage = "每日一剂，水煎服",
            Advice = "忌辛辣生冷",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试医案
    /// </summary>
    public static MedicalCase CreateMedicalCase(Guid? id = null, Guid? patientId = null, Guid? doctorId = null)
    {
        var doctor = doctorId ?? Guid.NewGuid();
        var patient = patientId ?? Guid.NewGuid();

        return new MedicalCase
        {
            Id = id ?? Guid.NewGuid(),
            PatientId = patient,
            PatientName = "测试患者",
            UserId = doctor,
            DoctorName = "测试医生",
            CaseNumber = $"YZ{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}",
            CaseStatus = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试诊疗记录
    /// OpenSpec: simplify-medicalcase-dataflow - Consultation与MedicalCase共享主键
    /// </summary>
    public static Consultation CreateConsultation(Guid? id = null)
    {
        return new Consultation
        {
            Id = id ?? Guid.NewGuid(),
            PresentIllness = $"现病史_{Guid.NewGuid():N}",
            TongueDiagnosis = "舌淡红，苔薄白",
            PulseDiagnosis = "脉象平和",
            TCMDiagnosis = "气血两虚",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试方剂
    /// </summary>
    public static Formula CreateFormula(Guid? id = null, string? name = null, string? category = null)
    {
        return new Formula
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试方剂_{Guid.NewGuid():N}",
            Category = category ?? "补益剂",
            Origin = $"来源：{Guid.NewGuid():N}",
            Composition = $"组成：黄芪15g，当归10g，川芎6g_{Guid.NewGuid():N}",
            Efficacy = $"功效：补气养血_{Guid.NewGuid():N}",
            Indications = $"主治：气血两虚证_{Guid.NewGuid():N}",
            Usage = $"用法：水煎服，每日一剂_{Guid.NewGuid():N}",
            Dosage = $"用量：成人每日1剂，分2次服用_{Guid.NewGuid():N}",
            Contraindications = $"禁忌：外感发热者忌用_{Guid.NewGuid():N}",
            Notes = $"备注：{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建用户列表
    /// </summary>
    public static List<User> CreateUsers(int count, UserRole role = UserRole.Doctor, CommonStatus status = CommonStatus.Enabled)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateUser(null, $"testuser{i:D3}", role, status))
            .ToList();
    }

    /// <summary>
    /// 创建患者列表
    /// </summary>
    public static List<Patient> CreatePatients(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreatePatient(null, $"测试患者{i:D3}"))
            .ToList();
    }

    /// <summary>
    /// 创建中药列表
    /// </summary>
    public static List<Herb> CreateHerbs(int count)
    {
        var categories = new[] { "补益药", "清热药", "解表药", "理气药", "活血化瘀药" };
        return Enumerable.Range(1, count)
            .Select(i => CreateHerb(null, $"测试中药{i:D3}", categories[i % categories.Length]))
            .ToList();
    }

    /// <summary>
    /// 创建处方列表
    /// </summary>
    public static List<Prescription> CreatePrescriptions(int count)
    {
        var medicalCases = CreateMedicalCases(count);

        return Enumerable.Range(1, count)
            .Select(i => CreatePrescription(null, medicalCases[i-1].Id))
            .ToList();
    }

    /// <summary>
    /// 创建医案列表
    /// </summary>
    public static List<MedicalCase> CreateMedicalCases(int count)
    {
        var patients = CreatePatients(count);
        var doctors = CreateUsers(count, UserRole.Doctor);

        return Enumerable.Range(1, count)
            .Select(i => CreateMedicalCase(null, patients[i-1].Id, doctors[i-1].Id))
            .ToList();
    }

    /// <summary>
    /// 创建诊疗记录列表
    /// 注意：Consultation与MedicalCase使用共享主键
    /// </summary>
    public static List<Consultation> CreateConsultations(int count)
    {
        var medicalCases = CreateMedicalCases(count);

        return Enumerable.Range(1, count)
            .Select(i => CreateConsultation(medicalCases[i-1].Id))
            .ToList();
    }

    /// <summary>
    /// 创建方剂列表
    /// </summary>
    public static List<Formula> CreateFormulas(int count)
    {
        var categories = new[] { "补益剂", "清热剂", "解表剂", "理气剂", "活血化瘀剂" };
        return Enumerable.Range(1, count)
            .Select(i => CreateFormula(null, $"测试方剂{i:D3}", categories[i % categories.Length]))
            .ToList();
    }

    /// <summary>
    /// 随机获取枚举值
    /// </summary>
    private static T GetRandomEnum<T>() where T : Enum
    {
        var values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(new Random().Next(values.Length))!;
    }
}