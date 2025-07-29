using System.ComponentModel;

namespace LYBT.UI.PrismWpf.Models
{
    /// <summary>
    /// API配置
    /// </summary>
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:5001";
        public int Timeout { get; set; } = 30;
    }

    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppSettings
    {
        public string AppName { get; set; } = "LYBT中医诊所管理系统";
        public string Version { get; set; } = "1.0.0";
        public string Theme { get; set; } = "Light";
    }

    /// <summary>
    /// 应用程序配置根类
    /// </summary>
    public class AppConfiguration
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public AppSettings AppSettings { get; set; } = new();
    }

    /// <summary>
    /// 用户信息扩展（用于UI绑定）
    /// </summary>
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public bool IsSelected { get; set; } // 用于批量操作选择
    }

    /// <summary>
    /// 患者信息
    /// </summary>
    public class PatientInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int Age { get; set; }
        public string IdCard { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public string EmergencyContact { get; set; } = string.Empty;
        public string AllergyHistory { get; set; } = string.Empty;
        public string MedicalHistory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 医生信息
    /// </summary>
    public class DoctorInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedTime { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 药材信息
    /// </summary>
    public class HerbInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PinyinCode { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int AlertStock { get; set; }
        public DateTime ExpireDate { get; set; }
        public bool IsActive { get; set; }
        public string BatchNo { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;
    }

    /// <summary>
    /// 经验方模板信息
    /// </summary>
    public class FormulaTemplateInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;
        public string Indications { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public bool IsActive { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 日志信息
    /// </summary>
    public class LogInfo
    {
        public Guid Id { get; set; }
        public DateTime LogTime { get; set; }
        public string LogType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Function { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string Parameters { get; set; } = string.Empty;
    }

    /// <summary>
    /// 角色选项
    /// </summary>
    public class RoleOption
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}