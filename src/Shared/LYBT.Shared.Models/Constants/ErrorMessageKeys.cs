namespace LYBT.Shared.Models.Constants
{
    /// <summary>
    /// 异常消息键值常量 - 国际化准备
    /// 集中管理所有异常消息，为将来的国际化支持做准备
    /// </summary>
    public static class ErrorMessageKeys
    {
        #region 通用应用异常消息

        /// <summary>应用程序异常</summary>
        public const string APP_EXCEPTION = "应用程序异常";

        /// <summary>业务处理失败</summary>
        public const string BUSINESS_FAILURE = "业务处理失败";

        /// <summary>数据验证失败</summary>
        public const string VALIDATION_FAILURE = "数据验证失败";

        /// <summary>请求的资源不存在</summary>
        public const string RESOURCE_NOT_FOUND = "请求的资源不存在";

        /// <summary>API调用失败</summary>
        public const string API_CALL_FAILED = "API调用失败";

        #endregion

        #region 认证和授权相关

        /// <summary>用户名或密码错误</summary>
        public const string INVALID_CREDENTIALS = "用户名或密码错误";

        /// <summary>账户已被锁定</summary>
        public const string ACCOUNT_LOCKED = "账户已被锁定";

        /// <summary>身份验证失败，请重新登录</summary>
        public const string AUTHENTICATION_FAILED = "身份验证失败，请重新登录";

        /// <summary>没有权限访问此资源</summary>
        public const string ACCESS_FORBIDDEN = "没有权限访问此资源";

        /// <summary>服务暂时不可用，请稍后重试</summary>
        public const string SERVICE_UNAVAILABLE = "服务暂时不可用，请稍后重试";

        /// <summary>请求超时，请稍后重试</summary>
        public const string REQUEST_TIMEOUT = "请求超时，请稍后重试";

        #endregion

        #region 用户相关异常

        /// <summary>用户不存在</summary>
        public const string USER_NOT_FOUND = "用户不存在";

        /// <summary>用户名 {0} 已存在</summary>
        public const string USER_ALREADY_EXISTS = "用户名 {0} 已存在";

        #endregion

        #region 患者相关异常

        /// <summary>患者不存在</summary>
        public const string PATIENT_NOT_FOUND = "患者不存在";

        /// <summary>患者 {0} (电话: {1}) 已存在</summary>
        public const string PATIENT_ALREADY_EXISTS = "患者 {0} (电话: {1}) 已存在";

        #endregion

        #region 药材相关异常

        /// <summary>药材不存在</summary>
        public const string HERB_NOT_FOUND = "药材不存在";

        /// <summary>药材 {0} 库存不足，需要 {1}，可用 {2}</summary>
        public const string HERB_INSUFFICIENT_STOCK = "药材 {0} 库存不足，需要 {1}，可用 {2}";

        #endregion

        #region 处方相关异常

        /// <summary>处方不存在</summary>
        public const string PRESCRIPTION_NOT_FOUND = "处方不存在";

        /// <summary>处方已处理，无法修改</summary>
        public const string PRESCRIPTION_ALREADY_PROCESSED = "处方已处理，无法修改";

        #endregion

        #region 医案相关异常

        /// <summary>医案不存在</summary>
        public const string MEDICAL_CASE_NOT_FOUND = "医案不存在";

        #endregion

        #region 诊断相关异常

        /// <summary>诊断不存在</summary>
        public const string CONSULTATION_NOT_FOUND = "诊断不存在";

        #endregion

        #region 验方相关异常

        /// <summary>验方不存在</summary>
        public const string FORMULA_NOT_FOUND = "验方不存在";

        #endregion

        #region 字段验证异常消息

        /// <summary>字段 {0} 验证失败: {1}</summary>
        public const string FIELD_VALIDATION_FAILED = "字段 {0} 验证失败: {1}";

        /// <summary>字段 {0} 验证失败</summary>
        public const string FIELD_VALIDATION_ERROR = "字段 {0} 验证失败";

        #endregion

        #region 业务规则异常

        /// <summary>{0} (ID: {1}) 不存在</summary>
        public const string RESOURCE_WITH_ID_NOT_FOUND = "{0} (ID: {1}) 不存在";

        /// <summary>API调用失败: {0}</summary>
        public const string API_CALL_FAILED_WITH_STATUS = "API调用失败: {0}";

        /// <summary>API调用失败: {0} {1} 返回 {2}</summary>
        public const string API_CALL_FAILED_WITH_DETAILS = "API调用失败: {0} {1} 返回 {2}";

        #endregion
    }
}