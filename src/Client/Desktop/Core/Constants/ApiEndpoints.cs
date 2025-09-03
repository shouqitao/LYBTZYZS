using LYBT.Shared.Models.Contracts.Common;
namespace LYBT.Desktop.Core.Constants
{
    /// <summary>
    /// API端点常量 - 与WebAPI路由保持一致
    /// </summary>
    public static class ApiEndpoints
    {
        // 核心业务模块API端点
        public const string Auth = "api/v1/Auth";
        public const string Users = "api/v1/Users";
        public const string Patients = "api/v1/Patients";
        public const string Herbs = "api/v1/Herbs";
        public const string Formulas = "api/v1/Formulas";
        public const string Consultation = "api/v1/Consultation";
        public const string MedicalCase = "api/v1/MedicalCase";
        public const string Prescriptions = "api/v1/Prescriptions";
    }
}