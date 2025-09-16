using Bogus;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Tests.Base;

/// <summary>
/// 处方测试数据生成器
/// </summary>
public static class PrescriptionTestDataGenerator
{
    /// <summary>
    /// 处方数据生成器
    /// </summary>
    public static Faker<Prescription> PrescriptionGenerator => new Faker<Prescription>("zh_CN")
        .RuleFor(p => p.Id, f => Guid.NewGuid())
        .RuleFor(p => p.PatientId, f => Guid.NewGuid())
        .RuleFor(p => p.PatientName, f => f.Name.FullName())
        .RuleFor(p => p.DoctorId, f => Guid.NewGuid())
        .RuleFor(p => p.DoctorName, f => f.Name.FullName())
        .RuleFor(p => p.MedicalCaseId, f => Guid.NewGuid())
        .RuleFor(p => p.ConsultationId, f => Guid.NewGuid())
        .RuleFor(p => p.PrescriptionDate, f => f.Date.Recent(30))
        .RuleFor(p => p.Status, f => f.PickRandom<PrescriptionStatus>())
        .RuleFor(p => p.TotalAmount, f => f.Random.Decimal(50, 500))
        .RuleFor(p => p.Notes, f => f.Lorem.Sentence())
        .RuleFor(p => p.Usage, f => f.Lorem.Sentence(3, 5))
        .RuleFor(p => p.Frequency, f => f.PickRandom("每日三次", "每日两次", "每日一次", "隔日一次"))
        .RuleFor(p => p.Duration, f => f.PickRandom("7天", "14天", "21天", "30天", "按需服用"))
        .RuleFor(p => p.CreateTime, f => f.Date.Recent(30))
        .RuleFor(p => p.UpdateTime, f => f.Date.Recent(5))
        .FinishWith((f, p) =>
        {
            // 确保更新时间不早于创建时间
            if (p.UpdateTime < p.CreateTime)
            {
                p.UpdateTime = p.CreateTime.AddHours(1);
            }
            
            // 初始化Items集合
            p.Items = new List<PrescriptionItem>();
        });

    /// <summary>
    /// 处方项目数据生成器
    /// </summary>
    public static Faker<PrescriptionItem> PrescriptionItemGenerator => new Faker<PrescriptionItem>("zh_CN")
        .RuleFor(pi => pi.Id, f => Guid.NewGuid())
        .RuleFor(pi => pi.PrescriptionId, f => Guid.NewGuid())
        .RuleFor(pi => pi.HerbId, f => Guid.NewGuid())
        .RuleFor(pi => pi.HerbName, f => f.PickRandom(
            "人参", "黄芪", "当归", "川芎", "白术", "茯苓", "甘草", "生地黄",
            "熟地黄", "白芍", "赤芍", "柴胡", "黄芩", "半夏", "陈皮", "枸杞子"))
        .RuleFor(pi => pi.Dosage, f => f.Random.Decimal(3, 30))
        .RuleFor(pi => pi.Unit, f => f.PickRandom("g", "片", "丸", "ml"))
        .RuleFor(pi => pi.UnitPrice, f => f.Random.Decimal(0.5m, 50m))
        .RuleFor(pi => pi.TotalPrice, (f, pi) => pi.Dosage * pi.UnitPrice)
        .RuleFor(pi => pi.Notes, f => f.Random.Bool(0.3f) ? f.Lorem.Sentence() : null)
        .RuleFor(pi => pi.CreateTime, f => f.Date.Recent(30))
        .RuleFor(pi => pi.UpdateTime, f => f.Date.Recent(5))
        .FinishWith((f, pi) =>
        {
            // 确保更新时间不早于创建时间
            if (pi.UpdateTime < pi.CreateTime)
            {
                pi.UpdateTime = pi.CreateTime.AddHours(1);
            }
        });

    /// <summary>
    /// 创建测试处方
    /// </summary>
    public static Prescription CreateTestPrescription(
        Guid? patientId = null,
        Guid? doctorId = null,
        PrescriptionStatus status = PrescriptionStatus.Draft,
        DateTime? prescriptionDate = null)
    {
        var prescription = PrescriptionGenerator.Generate();
        
        if (patientId.HasValue)
            prescription.PatientId = patientId.Value;
            
        if (doctorId.HasValue)
            prescription.DoctorId = doctorId.Value;
            
        prescription.Status = status;
        
        if (prescriptionDate.HasValue)
        {
            prescription.PrescriptionDate = prescriptionDate.Value;
            prescription.CreateTime = prescriptionDate.Value.AddHours(-1);
        }
            
        return prescription;
    }

    /// <summary>
    /// 创建带处方项目的测试处方
    /// </summary>
    public static Prescription CreateTestPrescriptionWithItems(
        int itemCount,
        Guid? patientId = null,
        Guid? doctorId = null,
        PrescriptionStatus status = PrescriptionStatus.Draft)
    {
        var prescription = CreateTestPrescription(patientId, doctorId, status);
        
        // 创建处方项目
        var items = CreatePrescriptionItems(prescription.Id, itemCount);
        prescription.Items = items;
        
        // 计算总金额
        prescription.TotalAmount = items.Sum(item => item.TotalPrice);
        
        return prescription;
    }

    /// <summary>
    /// 批量创建测试处方
    /// </summary>
    public static List<Prescription> CreateTestPrescriptions(
        int count,
        PrescriptionStatus? status = null)
    {
        var generator = PrescriptionGenerator;
        
        if (status.HasValue)
        {
            generator = generator.RuleFor(p => p.Status, status.Value);
        }
        
        return generator.Generate(count);
    }

    /// <summary>
    /// 为特定患者创建处方
    /// </summary>
    public static List<Prescription> CreateTestPrescriptionsForPatient(
        Guid patientId,
        int count)
    {
        var prescriptions = new List<Prescription>();
        
        for (int i = 0; i < count; i++)
        {
            var prescription = CreateTestPrescription(
                patientId: patientId,
                prescriptionDate: DateTime.Today.AddDays(-i * 5)); // 每5天一次
            prescriptions.Add(prescription);
        }
        
        return prescriptions;
    }

    /// <summary>
    /// 为特定医生创建处方
    /// </summary>
    public static List<Prescription> CreateTestPrescriptionsForDoctor(
        Guid doctorId,
        int count)
    {
        var prescriptions = new List<Prescription>();
        
        for (int i = 0; i < count; i++)
        {
            var prescription = CreateTestPrescription(
                doctorId: doctorId,
                prescriptionDate: DateTime.Today.AddDays(-i * 2)); // 每2天一次
            prescriptions.Add(prescription);
        }
        
        return prescriptions;
    }

    /// <summary>
    /// 创建处方项目列表
    /// </summary>
    public static List<PrescriptionItem> CreatePrescriptionItems(
        Guid prescriptionId,
        int count)
    {
        var items = PrescriptionItemGenerator.Generate(count);
        
        foreach (var item in items)
        {
            item.PrescriptionId = prescriptionId;
        }
        
        return items;
    }

    /// <summary>
    /// 创建单个处方项目
    /// </summary>
    public static PrescriptionItem CreateTestPrescriptionItem(
        Guid? prescriptionId = null,
        Guid? herbId = null,
        string? herbName = null,
        decimal? dosage = null)
    {
        var item = PrescriptionItemGenerator.Generate();
        
        if (prescriptionId.HasValue)
            item.PrescriptionId = prescriptionId.Value;
            
        if (herbId.HasValue)
            item.HerbId = herbId.Value;
            
        if (!string.IsNullOrEmpty(herbName))
            item.HerbName = herbName;
            
        if (dosage.HasValue)
        {
            item.Dosage = dosage.Value;
            item.TotalPrice = item.Dosage * item.UnitPrice;
        }
            
        return item;
    }

    /// <summary>
    /// 创建已完成的处方
    /// </summary>
    public static Prescription CreateCompletedPrescription(
        Guid? patientId = null,
        Guid? doctorId = null,
        int itemCount = 3)
    {
        return CreateTestPrescriptionWithItems(
            itemCount,
            patientId,
            doctorId,
            PrescriptionStatus.Completed);
    }

    /// <summary>
    /// 创建草稿处方
    /// </summary>
    public static Prescription CreateDraftPrescription(
        Guid? patientId = null,
        Guid? doctorId = null,
        int itemCount = 3)
    {
        return CreateTestPrescriptionWithItems(
            itemCount,
            patientId,
            doctorId,
            PrescriptionStatus.Draft);
    }

    /// <summary>
    /// 创建进行中的处方
    /// </summary>
    public static Prescription CreateInProgressPrescription(
        Guid? patientId = null,
        Guid? doctorId = null,
        int itemCount = 3)
    {
        return CreateTestPrescriptionWithItems(
            itemCount,
            patientId,
            doctorId,
            PrescriptionStatus.InProgress);
    }

    /// <summary>
    /// 创建特定日期范围内的处方
    /// </summary>
    public static List<Prescription> CreateTestPrescriptionsInDateRange(
        DateTime startDate,
        DateTime endDate,
        int count)
    {
        var prescriptions = new List<Prescription>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var randomDays = random.Next(0, (endDate - startDate).Days + 1);
            var prescriptionDate = startDate.AddDays(randomDays);
            
            var prescription = CreateTestPrescription(prescriptionDate: prescriptionDate);
            prescriptions.Add(prescription);
        }
        
        return prescriptions;
    }

    /// <summary>
    /// 创建高价值处方（用于金额测试）
    /// </summary>
    public static Prescription CreateHighValuePrescription(
        decimal minTotalAmount = 1000m)
    {
        var prescription = CreateTestPrescription();
        var items = new List<PrescriptionItem>();
        
        // 创建高价值项目
        for (int i = 0; i < 5; i++)
        {
            var item = PrescriptionItemGenerator.Generate();
            item.PrescriptionId = prescription.Id;
            item.UnitPrice = 50m + i * 20m; // 高单价
            item.Dosage = 10m + i * 5m; // 较大剂量
            item.TotalPrice = item.UnitPrice * item.Dosage;
            items.Add(item);
        }
        
        prescription.Items = items;
        prescription.TotalAmount = items.Sum(item => item.TotalPrice);
        
        // 如果总金额不够，调整第一个项目的单价
        if (prescription.TotalAmount < minTotalAmount)
        {
            var deficit = minTotalAmount - prescription.TotalAmount;
            items.First().UnitPrice += deficit / items.First().Dosage;
            items.First().TotalPrice = items.First().UnitPrice * items.First().Dosage;
            prescription.TotalAmount = items.Sum(item => item.TotalPrice);
        }
        
        return prescription;
    }
}