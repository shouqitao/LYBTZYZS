namespace LYBT.Desktop.Core.Resources
{

    /// <summary>
    /// 错误消息资源
    /// </summary>
    public static class ErrorMessages
    {

        #region 网络错误消息

        public static class Network
        {
            public const string ConnectionFailed = "网络连接失败，请检查网络设置";
            public const string Timeout = "网络请求超时，请稍后重试";
            public const string ServerUnavailable = "服务器暂时不可用，请稍后重试";
            public const string BadGateway = "网关错误，请稍后重试";
            public const string TooManyRequests = "请求过于频繁，请稍后重试";
            public const string InternalServerError = "服务器内部错误，请稍后重试";
            public const string NotFound = "请求的资源不存在";
            public const string BadRequest = "请求参数错误，请检查输入";
        }

        #endregion 网络错误消息

        #region 认证错误消息

        public static class Authentication
        {
            public const string LoginRequired = "请先登录后再执行此操作";
            public const string TokenExpired = "登录已过期，请重新登录";
            public const string InvalidCredentials = "用户名或密码错误";
            public const string InsufficientPermissions = "权限不足，无法执行此操作";
            public const string AccountLocked = "账户已被锁定，请联系管理员";
            public const string Unauthorized = "未授权访问，请重新登录";
            public const string ForbiddenAccess = "禁止访问此资源";
        }

        #endregion 认证错误消息

        #region 验证错误消息

        public static class Validation
        {
            public const string RequiredFieldEmpty = "必填字段不能为空";
            public const string InvalidFormat = "数据格式不正确";
            public const string InvalidRange = "数值超出允许范围";
            public const string InvalidLength = "输入长度不符合要求";
            public const string DuplicateValue = "数据已存在，不能重复";
            public const string InvalidEmail = "邮箱格式不正确";
            public const string InvalidPhone = "电话号码格式不正确";
            public const string InvalidDate = "日期格式不正确";
            public const string InvalidNumber = "数字格式不正确";
        }

        #endregion 验证错误消息

        #region 业务错误消息

        public static class Business
        {
            public const string DataNotFound = "未找到相关数据";
            public const string OperationNotAllowed = "当前状态不允许此操作";
            public const string BusinessRuleViolation = "违反业务规则";
            public const string DataConflict = "数据冲突，请刷新后重试";
            public const string ResourceInUse = "资源正在使用中，无法删除";
            public const string QuotaExceeded = "已超出配额限制";
            public const string ServiceNotAvailable = "服务暂不可用";
        }

        #endregion 业务错误消息

        #region 系统错误消息

        public static class System
        {
            public const string UnknownError = "发生未知错误";
            public const string InternalError = "系统内部错误";
            public const string OutOfMemory = "内存不足，请关闭其他程序后重试";
            public const string StackOverflow = "程序错误：堆栈溢出";
            public const string FileNotFound = "文件未找到";
            public const string DirectoryNotFound = "目录未找到";
            public const string AccessDenied = "访问被拒绝";
            public const string OperationCanceled = "操作已取消";
            public const string OperationTimeout = "操作超时";
        }

        #endregion 系统错误消息

        #region 用户操作错误消息

        public static class UserOperation
        {
            public const string InvalidInput = "输入无效，请检查后重试";
            public const string OperationFailed = "操作失败";
            public const string SaveFailed = "保存失败";
            public const string DeleteFailed = "删除失败";
            public const string LoadFailed = "加载失败";
            public const string UpdateFailed = "更新失败";
            public const string CreateFailed = "创建失败";
        }

        #endregion 用户操作错误消息

        #region 建议操作消息

        public static class SuggestedActions
        {
            public const string CheckNetwork = "检查网络连接";
            public const string RetryLater = "稍后重试";
            public const string ContactAdmin = "联系管理员";
            public const string ContactSupport = "联系技术支持";
            public const string RestartApp = "重启应用程序";
            public const string ReLogin = "重新登录";
            public const string CheckInput = "检查输入数据";
            public const string RefreshPage = "刷新页面";
            public const string CheckPermissions = "检查权限设置";
            public const string UpdateApp = "更新应用程序";
            public const string FreeMemory = "释放内存";
            public const string CheckConfiguration = "检查配置设置";
        }

        #endregion 建议操作消息

        #region 错误消息获取方法

        private static readonly Dictionary<string, string> _messageTemplates = new()
        {
            // 网络错误模板
            ["HTTP_400"] = Network.BadRequest,
            ["HTTP_401"] = Authentication.Unauthorized,
            ["HTTP_403"] = Authentication.ForbiddenAccess,
            ["HTTP_404"] = Network.NotFound,
            ["HTTP_408"] = Network.Timeout,
            ["HTTP_429"] = Network.TooManyRequests,
            ["HTTP_500"] = Network.InternalServerError,
            ["HTTP_502"] = Network.BadGateway,
            ["HTTP_503"] = Network.ServerUnavailable,
            ["HTTP_504"] = Network.Timeout,

            // 业务错误模板
            ["BUSINESS_DATA_NOT_FOUND"] = Business.DataNotFound,
            ["BUSINESS_OPERATION_NOT_ALLOWED"] = Business.OperationNotAllowed,
            ["BUSINESS_RULE_VIOLATION"] = Business.BusinessRuleViolation,
            ["BUSINESS_DATA_CONFLICT"] = Business.DataConflict,
            ["BUSINESS_RESOURCE_IN_USE"] = Business.ResourceInUse,

            // 验证错误模板
            ["VALIDATION_REQUIRED"] = Validation.RequiredFieldEmpty,
            ["VALIDATION_FORMAT"] = Validation.InvalidFormat,
            ["VALIDATION_RANGE"] = Validation.InvalidRange,
            ["VALIDATION_LENGTH"] = Validation.InvalidLength,
            ["VALIDATION_DUPLICATE"] = Validation.DuplicateValue,

            // 系统错误模板
            ["SYSTEM_OUT_OF_MEMORY"] = System.OutOfMemory,
            ["SYSTEM_STACK_OVERFLOW"] = System.StackOverflow,
            ["SYSTEM_FILE_NOT_FOUND"] = System.FileNotFound,
            ["SYSTEM_ACCESS_DENIED"] = System.AccessDenied,
            ["SYSTEM_OPERATION_CANCELED"] = System.OperationCanceled,
        };

        /// <summary>
        /// 根据错误代码获取消息
        /// </summary>
        public static string GetMessage(string errorCode, string? defaultMessage = null)
        {
            return _messageTemplates.TryGetValue(errorCode, out var message) ? message : (defaultMessage ?? System.UnknownError);
        }

        /// <summary>
        /// 格式化错误消息
        /// </summary>
        public static string FormatMessage(string template, params object[] args)
        {
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// 获取HTTP状态码对应的错误消息
        /// </summary>
        public static string GetHttpErrorMessage(int statusCode)
        {
            return GetMessage($"HTTP_{statusCode}", Network.ConnectionFailed);
        }

        #endregion 错误消息获取方法
    }
}
