using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// 测试数据工厂，提供标准测试数据创建方法
/// </summary>
public static class TestDataFactory
{
    private static int _patientCounter = 0;
    private static int _userCounter = 0;
    private static int _medicalCaseCounter = 0;

    /// <summary>
    /// 创建测试患者
    /// </summary>
    public static Patient CreatePatient(
        string? name = null,
        Gender? gender = null,
        string? phoneNumber = null,
        DateTime? birthDate = null,
        string? idNumber = null,
        string? address = null)
    {
        _patientCounter++;
        var patientName = name ?? $"测试患者{_patientCounter}";

        return new Patient
        {
            Id = Guid.NewGuid(),
            Name = patientName,
            PinYinCode = $"CSHZ{_patientCounter}",
            Gender = gender ?? Gender.Male,
            PhoneNumber = phoneNumber ?? $"1380013{8000 + _patientCounter:D4}",
            BirthDate = birthDate ?? DateTime.Now.AddYears(-30),
            IdNumber = idNumber ?? $"11010119900101{_patientCounter:D4}",
            Address = address ?? $"测试地址{_patientCounter}",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试用户（医生）
    /// </summary>
    public static User CreateUser(
        string? userName = null,
        string? realName = null,
        UserRole? role = null,
        string? passwordHash = null)
    {
        _userCounter++;
        var username = userName ?? $"testuser{_userCounter}";

        return new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            RealName = realName ?? $"测试用户{_userCounter}",
            PinYinCode = $"CSYH{_userCounter}",
            Role = role ?? UserRole.Doctor,
            PasswordHash = passwordHash ?? "$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", // BCrypt hash placeholder
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试医案
    /// </summary>
    public static MedicalCase CreateMedicalCase(
        Guid? patientId = null,
        string? patientName = null,
        Guid? userId = null,
        string? doctorName = null,
        MedicalCaseStatus? status = null)
    {
        _medicalCaseCounter++;
        var now = DateTime.Now;

        return new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId ?? Guid.NewGuid(),
            PatientName = patientName ?? $"测试患者{_medicalCaseCounter}",
            UserId = userId ?? Guid.NewGuid(),
            DoctorName = doctorName ?? $"测试医生{_medicalCaseCounter}",
            CaseNumber = $"MC{now:yyyyMMdd}{_medicalCaseCounter:D3}",
            CaseStatus = status ?? MedicalCaseStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// 创建测试诊断
    /// </summary>
    public static Consultation CreateConsultation(
        Guid? medicalCaseId = null,
        string? presentIllness = null,
        string? tongueDiagnosis = null,
        string? pulseDiagnosis = null,
        string? tcmDiagnosis = null)
    {
        return new Consultation
        {
            Id = medicalCaseId ?? Guid.NewGuid(),
            PresentIllness = presentIllness ?? "患者自述头痛、发热",
            TongueDiagnosis = tongueDiagnosis ?? "舌淡红，苔薄白",
            PulseDiagnosis = pulseDiagnosis ?? "脉浮数",
            TcmDiagnosis = tcmDiagnosis ?? "风热感冒",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试处方
    /// </summary>
    public static Prescription CreatePrescription(
        Guid? medicalCaseId = null,
        int? dosageCount = null,
        string? usage = null,
        string? advice = null)
    {
        return new Prescription
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = medicalCaseId ?? Guid.NewGuid(),
            PrescriptionNumber = $"RX{DateTime.Now:yyyyMMdd}{Guid.NewGuid().ToString("N")[..4]}",
            DosageCount = dosageCount ?? 7,
            Discount = 1.0m,
            Usage = usage ?? "水煎服，每日一剂",
            Advice = advice ?? "忌辛辣油腻",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试处方药材项
    /// </summary>
    public static PrescriptionItem CreatePrescriptionItem(
        Guid? prescriptionId = null,
        Guid? herbId = null,
        string? herbName = null,
        int? dosage = null,
        decimal? unitPrice = null)
    {
        return new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId ?? Guid.NewGuid(),
            HerbId = herbId ?? Guid.NewGuid(),
            HerbName = herbName ?? "测试药材",
            Dosage = dosage ?? 10,
            Unit = "g",
            UnitPrice = unitPrice ?? 1.5m,
            Usage = "常规煎服"
        };
    }

    /// <summary>
    /// 异步保存患者到数据库
    /// </summary>
    public static async Task<Patient> SavePatientAsync(LocalDbContext context, Patient? patient = null)
    {
        patient ??= CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient;
    }

    /// <summary>
    /// 异步保存用户到数据库
    /// </summary>
    public static async Task<User> SaveUserAsync(LocalDbContext context, User? user = null)
    {
        user ??= CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// 异步保存医案到数据库（包含诊断和处方）
    /// </summary>
    public static async Task<MedicalCase> SaveMedicalCaseAsync(
        LocalDbContext context,
        MedicalCase? medicalCase = null,
        Consultation? consultation = null,
        Prescription? prescription = null)
    {
        medicalCase ??= CreateMedicalCase();

        // 确保患者存在
        var patient = await context.Patients.FindAsync(medicalCase.PatientId);
        if (patient == null)
        {
            patient = CreatePatient(id: medicalCase.PatientId, name: medicalCase.PatientName);
            context.Patients.Add(patient);
        }

        // 确保医生存在
        var doctor = await context.Users.FindAsync(medicalCase.UserId);
        if (doctor == null)
        {
            doctor = CreateUser(id: medicalCase.UserId, realName: medicalCase.DoctorName);
            context.Users.Add(doctor);
        }

        context.MedicalCases.Add(medicalCase);

        // 添加诊断
        if (consultation != null)
        {
            consultation.Id = medicalCase.Id;
            context.Consultations.Add(consultation);
        }

        // 添加处方
        if (prescription != null)
        {
            prescription.MedicalCaseId = medicalCase.Id;
            context.Prescriptions.Add(prescription);
        }

        await context.SaveChangesAsync();
        return medicalCase;
    }

    /// <summary>
    /// 重置计数器（用于测试隔离）
    /// </summary>
    public static void ResetCounters()
    {
        _patientCounter = 0;
        _userCounter = 0;
        _medicalCaseCounter = 0;
    }

    // 私有辅助方法
    private static Patient CreatePatient(Guid id, string name)
    {
        return new Patient
        {
            Id = id,
            Name = name,
            PinYinCode = "CSHZ",
            Gender = Gender.Male,
            PhoneNumber = "13800138000",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private static User CreateUser(Guid id, string realName)
    {
        return new User
        {
            Id = id,
            UserName = $"user{id:N}",
            RealName = realName,
            Role = UserRole.Doctor,
            PasswordHash = "$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
