namespace LYBT.Shared.Utilities.Constants
{
    /// <summary>
    /// API路由常量定义 - 统一管理所有API端点路由
    /// </summary>
    /// <remarks>
    /// <para>架构标准: 所有API路由必须在此处统一定义，禁止硬编码</para>
    /// <para>命名规范: RESTful风格，小写路径，版本化管理</para>
    /// <para>使用场景: 服务端Controller路由、测试用例、文档生成</para>
    /// <para>注意事项: Refit属性需要字符串字面量，故API接口仍需硬编码但必须与此处保持一致</para>
    /// </remarks>
    public static class ApiRoutes
    {
        /// <summary>
        /// API版本前缀
        /// </summary>
        public const string ApiVersion = "api/v1";

        /// <summary>
        /// 身份认证模块路由
        /// </summary>
        public static class Auth
        {
            public const string Base = $"{ApiVersion}/auth";
            public const string Login = $"{Base}/login";
            public const string Logout = $"{Base}/logout";
            public const string CurrentUser = $"{Base}/current-user";
            public const string RefreshToken = $"{Base}/refresh-token";
            public const string ChangePassword = $"{Base}/change-password";
            public const string ResetPassword = $"{Base}/reset-password";
            public const string ValidateToken = $"{Base}/validate-token";
        }

        /// <summary>
        /// 用户管理模块路由
        /// </summary>
        public static class Users
        {
            public const string Base = $"{ApiVersion}/users";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string CheckUsername = $"{Base}/check-username";
            public const string GetRoles = $"{Base}/roles";
        }

        /// <summary>
        /// 患者档案模块路由
        /// </summary>
        public static class Patients
        {
            public const string Base = $"{ApiVersion}/patients";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string GetMedicalHistory = $"{Base}/{{id}}/medical-history";
            public const string Statistics = $"{Base}/statistics";
        }

        /// <summary>
        /// 医案管理模块路由
        /// </summary>
        public static class MedicalCase
        {
            public const string Base = $"{ApiVersion}/medicalcase";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string GetByPatient = $"{Base}/patient/{{patientId}}";
            public const string Export = $"{Base}/{{id}}/export";
        }

        /// <summary>
        /// 诊疗咨询模块路由
        /// </summary>
        public static class Consultation
        {
            public const string Base = $"{ApiVersion}/consultation";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string GetByMedicalCase = $"{Base}/medicalcase/{{medicalCaseId}}";
            public const string SaveDiagnosis = $"{Base}/{{id}}/diagnosis";
        }

        /// <summary>
        /// 处方管理模块路由
        /// </summary>
        public static class Prescriptions
        {
            public const string Base = $"{ApiVersion}/prescriptions";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string GetByConsultation = $"{Base}/consultation/{{consultationId}}";
            public const string Validate = $"{Base}/{{id}}/validate";
            public const string Print = $"{Base}/{{id}}/print";
        }

        /// <summary>
        /// 中药材管理模块路由
        /// </summary>
        public static class Herbs
        {
            public const string Base = $"{ApiVersion}/herbs";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string GetCategories = $"{Base}/categories";
            public const string GetByCategory = $"{Base}/category/{{category}}";
        }

        /// <summary>
        /// 验方管理模块路由
        /// </summary>
        public static class Formula
        {
            public const string Base = $"{ApiVersion}/formula";
            public const string GetById = $"{Base}/{{id}}";
            public const string Search = $"{Base}/search";
            public const string Create = Base;
            public const string Update = $"{Base}/{{id}}";
            public const string Delete = $"{Base}/{{id}}";
            public const string GetTemplates = $"{Base}/templates";
            public const string ApplyTemplate = $"{Base}/{{id}}/apply";
        }

        /// <summary>
        /// 健康检查路由
        /// </summary>
        public static class Health
        {
            public const string Base = $"{ApiVersion}/health";
            public const string Check = Base;
            public const string Ready = $"{Base}/ready";
            public const string Live = $"{Base}/live";
        }
    }
}