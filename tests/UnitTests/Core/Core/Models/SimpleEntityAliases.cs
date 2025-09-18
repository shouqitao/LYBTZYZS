/// <summary>
/// P3-Fix 简化实体别名 - 直接解决Core测试项目编译错误
/// 避免复杂的UltraThink测试基础设施依赖
/// </summary>

// P3-Fix: 全局类型别名，解决测试项目编译错误
global using UserModel = LYBT.Entities.Users.User;
global using PatientModel = LYBT.Entities.Patients.Patient;
global using HerbModel = LYBT.Entities.Herbs.Herb;
global using FormulaModel = LYBT.Entities.Formula.Formula;
global using PrescriptionModel = LYBT.Entities.Prescriptions.Prescription;
global using MedicalCaseModel = LYBT.Entities.MedicalCase.MedicalCase;
global using ConsultationModel = LYBT.Entities.Consultation.Consultation;
global using AuthModel = LYBT.Entities.Auth.AuthSession;
global using PrescriptionItemModel = LYBT.Entities.Prescriptions.PrescriptionItem;

// P3-Fix: DTO类型别名临时定义，解决测试项目编译错误
using System;

namespace LYBT.Tests.Core.TempDtos
{
    public class UserCreateDto { public string Username { get; set; } = ""; public string RealName { get; set; } = ""; }
    public class UserUpdateDto { public string Username { get; set; } = ""; public string RealName { get; set; } = ""; }
    public class HerbCreateDto { public string Name { get; set; } = ""; public decimal Price { get; set; } }
    public class HerbPriceUpdateDto { public Guid Id { get; set; } public decimal Price { get; set; } }
    public class HerbImportDto { public string Name { get; set; } = ""; public decimal Price { get; set; } public string Unit { get; set; } = ""; }
}