using LYBT.Shared.Utilities.Constants;

namespace LYBT.Desktop.Core.Constants
{

    /// <summary>
    /// API端点常量 - 与WebAPI路由保持一致
    /// </summary>
    /// <remarks>
    /// 已迁移到 LYBT.Shared.Utilities.Constants.ApiRoutes
    /// 保留此类用于向后兼容，新代码请使用 ApiRoutes
    /// </remarks>
    [System.Obsolete("请使用 LYBT.Shared.Utilities.Constants.ApiRoutes 代替")]
    public static class ApiEndpoints
    {

        // 核心业务模块API端点 - 保持原始值用于向后兼容
        public const string Auth = "api/v1/auth";
        public const string Users = "api/v1/users";
        public const string Patients = "api/v1/patients";
        public const string Herbs = "api/v1/herbs";
        public const string Formulas = "api/v1/formula";
        public const string Consultation = "api/v1/consultation";
        public const string MedicalCase = "api/v1/medicalcase";
        public const string Prescriptions = "api/v1/prescriptions";
    }
}
