using Bogus;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Tests.Base;

/// <summary>
/// 医疗案例测试数据生成器
/// </summary>
public static class MedicalCaseTestDataGenerator
{
    /// <summary>
    /// 医疗案例数据生成器
    /// </summary>
    public static Faker<LYBT.Entities.MedicalCase.MedicalCase> MedicalCaseGenerator => 
        new Faker<LYBT.Entities.MedicalCase.MedicalCase>("zh_CN")
            .RuleFor(mc => mc.Id, f => Guid.NewGuid())
            .RuleFor(mc => mc.PatientId, f => Guid.NewGuid())
            .RuleFor(mc => mc.DoctorId, f => Guid.NewGuid())
            .RuleFor(mc => mc.ConsultationDate, f => f.Date.Recent(30))
            .RuleFor(mc => mc.Status, f => f.PickRandom<MedicalCaseStatus>())
            .RuleFor(mc => mc.ChiefComplaint, f => f.Lorem.Sentence(5, 10))
            .RuleFor(mc => mc.PresentIllnessHistory, f => f.Lorem.Paragraph(3))
            .RuleFor(mc => mc.PastMedicalHistory, f => f.Lorem.Paragraph(2))
            .RuleFor(mc => mc.FamilyHistory, f => f.Lorem.Sentence())
            .RuleFor(mc => mc.PersonalHistory, f => f.Lorem.Sentence())
            .RuleFor(mc => mc.PhysicalExamination, f => f.Lorem.Paragraph())
            .RuleFor(mc => mc.AuxiliaryExamination, f => f.Lorem.Sentence())
            .RuleFor(mc => mc.TcmDiagnosis, f => f.Lorem.Words(3).ToString())
            .RuleFor(mc => mc.WesternDiagnosis, f => f.Lorem.Words(4).ToString())
            .RuleFor(mc => mc.TreatmentPrinciple, f => f.Lorem.Sentence())
            .RuleFor(mc => mc.Notes, f => f.Lorem.Sentence())
            .RuleFor(mc => mc.CreateTime, f => f.Date.Recent(30))
            .RuleFor(mc => mc.UpdateTime, f => f.Date.Recent(5))
            .FinishWith((f, mc) =>
            {
                // 创建关联的诊断记录
                if (mc.Consultation == null)
                {
                    mc.Consultation = CreateConsultationForMedicalCase(mc.Id, mc.DoctorId, mc.PatientId);
                }
                
                // 确保更新时间不早于创建时间
                if (mc.UpdateTime < mc.CreateTime)
                {
                    mc.UpdateTime = mc.CreateTime.AddHours(1);
                }
            });

    /// <summary>
    /// 诊断记录生成器
    /// </summary>
    public static Faker<ConsultationModel> ConsultationGenerator =>
        new Faker<ConsultationModel>("zh_CN")
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.MedicalCaseId, f => Guid.NewGuid())
            .RuleFor(c => c.DoctorId, f => Guid.NewGuid())
            .RuleFor(c => c.PatientId, f => Guid.NewGuid())
            .RuleFor(c => c.ConsultationDate, f => f.Date.Recent(30))
            .RuleFor(c => c.Inspection, f => f.Lorem.Paragraph()) // 望诊
            .RuleFor(c => c.Auscultation, f => f.Lorem.Paragraph()) // 闻诊
            .RuleFor(c => c.Inquiry, f => f.Lorem.Paragraph()) // 问诊
            .RuleFor(c => c.Palpation, f => f.Lorem.Paragraph()) // 切诊
            .RuleFor(c => c.TongueExamination, f => f.Lorem.Sentence()) // 舌诊
            .RuleFor(c => c.PulseExamination, f => f.Lorem.Sentence()) // 脉诊
            .RuleFor(c => c.SyndromeClassification, f => f.Lorem.Words(3).ToString()) // 证候分型
            .RuleFor(c => c.TreatmentMethod, f => f.Lorem.Words(4).ToString()) // 治法
            .RuleFor(c => c.TcmDiagnosis, f => f.Lorem.Words(3).ToString())
            .RuleFor(c => c.WesternDiagnosis, f => f.Lorem.Words(4).ToString())
            .RuleFor(c => c.Notes, f => f.Lorem.Sentence())
            .RuleFor(c => c.CreateTime, f => f.Date.Recent(30))
            .RuleFor(c => c.UpdateTime, f => f.Date.Recent(5));

    /// <summary>
    /// 创建测试医疗案例
    /// </summary>
    public static LYBT.Entities.MedicalCase.MedicalCase CreateTestMedicalCase(
        Guid? patientId = null,
        Guid? doctorId = null,
        MedicalCaseStatus status = MedicalCaseStatus.InProgress,
        DateTime? consultationDate = null)
    {
        var medicalCase = MedicalCaseGenerator.Generate();
        
        if (patientId.HasValue)
            medicalCase.PatientId = patientId.Value;
            
        if (doctorId.HasValue)
            medicalCase.DoctorId = doctorId.Value;
            
        medicalCase.Status = status;
        
        if (consultationDate.HasValue)
        {
            medicalCase.ConsultationDate = consultationDate.Value;
            medicalCase.CreateTime = consultationDate.Value.AddHours(-1);
        }

        // 重新创建关联的诊断记录
        medicalCase.Consultation = CreateConsultationForMedicalCase(
            medicalCase.Id, medicalCase.DoctorId, medicalCase.PatientId, medicalCase.ConsultationDate);
            
        return medicalCase;
    }

    /// <summary>
    /// 批量创建测试医疗案例
    /// </summary>
    public static List<LYBT.Entities.MedicalCase.MedicalCase> CreateTestMedicalCases(
        int count,
        MedicalCaseStatus? status = null)
    {
        var generator = MedicalCaseGenerator;
        
        if (status.HasValue)
        {
            generator = generator.RuleFor(mc => mc.Status, status.Value);
        }
        
        return generator.Generate(count);
    }

    /// <summary>
    /// 为特定患者创建医疗案例
    /// </summary>
    public static List<LYBT.Entities.MedicalCase.MedicalCase> CreateTestMedicalCasesForPatient(
        Guid patientId, 
        int count)
    {
        var medicalCases = new List<LYBT.Entities.MedicalCase.MedicalCase>();
        
        for (int i = 0; i < count; i++)
        {
            var medicalCase = CreateTestMedicalCase(
                patientId: patientId, 
                consultationDate: DateTime.Today.AddDays(-i * 7)); // 每周一次
            medicalCases.Add(medicalCase);
        }
        
        return medicalCases;
    }

    /// <summary>
    /// 为特定医生创建医疗案例
    /// </summary>
    public static List<LYBT.Entities.MedicalCase.MedicalCase> CreateTestMedicalCasesForDoctor(
        Guid doctorId,
        int count)
    {
        var medicalCases = new List<LYBT.Entities.MedicalCase.MedicalCase>();
        
        for (int i = 0; i < count; i++)
        {
            var medicalCase = CreateTestMedicalCase(
                doctorId: doctorId,
                consultationDate: DateTime.Today.AddDays(-i * 3)); // 每3天一次
            medicalCases.Add(medicalCase);
        }
        
        return medicalCases;
    }

    /// <summary>
    /// 创建已完成的医疗案例
    /// </summary>
    public static LYBT.Entities.MedicalCase.MedicalCase CreateCompletedMedicalCase(
        Guid? patientId = null,
        Guid? doctorId = null)
    {
        return CreateTestMedicalCase(
            patientId: patientId,
            doctorId: doctorId,
            status: MedicalCaseStatus.Completed,
            consultationDate: DateTime.Today.AddDays(-7));
    }

    /// <summary>
    /// 创建进行中的医疗案例
    /// </summary>
    public static LYBT.Entities.MedicalCase.MedicalCase CreateInProgressMedicalCase(
        Guid? patientId = null,
        Guid? doctorId = null)
    {
        return CreateTestMedicalCase(
            patientId: patientId,
            doctorId: doctorId,
            status: MedicalCaseStatus.InProgress,
            consultationDate: DateTime.Today);
    }

    /// <summary>
    /// 为医疗案例创建对应的诊断记录
    /// </summary>
    private static ConsultationModel CreateConsultationForMedicalCase(
        Guid medicalCaseId,
        Guid doctorId,
        Guid patientId,
        DateTime? consultationDate = null)
    {
        var consultation = ConsultationGenerator.Generate();
        consultation.MedicalCaseId = medicalCaseId;
        consultation.DoctorId = doctorId;
        consultation.PatientId = patientId;
        consultation.ConsultationDate = consultationDate ?? DateTime.Today;
        
        return consultation;
    }

    /// <summary>
    /// 创建特定日期范围内的医疗案例
    /// </summary>
    public static List<LYBT.Entities.MedicalCase.MedicalCase> CreateTestMedicalCasesInDateRange(
        DateTime startDate,
        DateTime endDate,
        int count)
    {
        var medicalCases = new List<LYBT.Entities.MedicalCase.MedicalCase>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var randomDays = random.Next(0, (endDate - startDate).Days + 1);
            var consultationDate = startDate.AddDays(randomDays);
            
            var medicalCase = CreateTestMedicalCase(consultationDate: consultationDate);
            medicalCases.Add(medicalCase);
        }
        
        return medicalCases;
    }
}