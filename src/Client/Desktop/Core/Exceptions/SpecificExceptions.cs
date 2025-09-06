using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Exceptions {

    /// <summary>
    /// 网络异常
    /// </summary>
    [Serializable]
    public class NetworkException : AppException {
        public int? HttpStatusCode { get; set; }
        public string Endpoint { get; set; } = string.Empty;

        public NetworkException(string message, string? endpoint = null, int? statusCode = null)
            : base(message, ErrorCategory.Network, ErrorSeverity.Error) {
            Endpoint = endpoint ?? string.Empty;
            HttpStatusCode = statusCode;
            ErrorCode = $"NET_{statusCode ?? 0}";
            UserFriendlyMessage = statusCode switch {
                404 => "请求的资源不存在",
                500 => "服务器内部错误，请稍后重试",
                503 => "服务暂时不可用，请稍后重试",
                _ => "网络连接失败，请检查网络设置"
            };
        }

        public NetworkException(string message, Exception innerException)
            : base(message, ErrorCategory.Network, ErrorSeverity.Error, innerException) {
        }
    }

    /// <summary>
    /// 认证异常
    /// </summary>
    [Serializable]
    public class AuthenticationException : AppException {
        public string Username { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;

        public AuthenticationException(string message, string? username = null)
            : base(message, ErrorCategory.Authentication, ErrorSeverity.Warning) {
            Username = username ?? string.Empty;
            FailureReason = message;
            ErrorCode = "AUTH_001";
            UserFriendlyMessage = "登录失败，请检查用户名和密码";
        }

        public AuthenticationException(string message, Exception innerException)
            : base(message, ErrorCategory.Authentication, ErrorSeverity.Warning, innerException) {
        }
    }

    /// <summary>
    /// 授权异常
    /// </summary>
    [Serializable]
    public class AuthorizationException : AppException {
        public string RequiredPermission { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;

        public AuthorizationException(string message, string? requiredPermission = null)
            : base(message, ErrorCategory.Authorization, ErrorSeverity.Warning) {
            RequiredPermission = requiredPermission ?? string.Empty;
            ErrorCode = "AUTHZ_001";
            UserFriendlyMessage = "您没有权限执行此操作";
        }
    }

    /// <summary>
    /// 验证异常
    /// </summary>
    [Serializable]
    public class ValidationException : AppException {
        public string FieldName { get; set; } = string.Empty;
        public object? InvalidValue { get; set; }
        public string ValidationRule { get; set; } = string.Empty;

        public ValidationException(string message, string? fieldName = null)
            : base(message, ErrorCategory.Validation, ErrorSeverity.Info) {
            FieldName = fieldName ?? string.Empty;
            InvalidValue = null;
            ValidationRule = "Required";
            ErrorCode = "VAL_001";
            UserFriendlyMessage = $"输入的{fieldName ?? "数据"}不正确";
            IsRetryable = false;
        }
    }

    /// <summary>
    /// 业务逻辑异常
    /// </summary>
    [Serializable]
    public class BusinessException : AppException {
        public string BusinessRule { get; set; } = string.Empty;
        public object Context { get; set; } = new object();

        public BusinessException(string message, string? businessRule = null)
            : base(message, ErrorCategory.Business, ErrorSeverity.Warning) {
            BusinessRule = businessRule ?? string.Empty;
            ErrorCode = "BUS_001";
            IsRetryable = false;
        }
    }

    /// <summary>
    /// 数据访问异常
    /// </summary>
    [Serializable]
    public class DataAccessException : AppException {
        public string EntityType { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;

        public DataAccessException(string message, string? entityType = null, string? operation = null)
            : base(message, ErrorCategory.DataAccess, ErrorSeverity.Error) {
            EntityType = entityType ?? string.Empty;
            Operation = operation ?? string.Empty;
            ErrorCode = "DATA_001";
            UserFriendlyMessage = "数据操作失败，请稍后重试";
        }

        public DataAccessException(string message, Exception innerException)
            : base(message, ErrorCategory.DataAccess, ErrorSeverity.Error, innerException) {
        }
    }

    /// <summary>
    /// 配置异常
    /// </summary>
    [Serializable]
    public class ConfigurationException : AppException {
        public string ConfigurationKey { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;

        public ConfigurationException(string message, string? configKey = null)
            : base(message, ErrorCategory.Configuration, ErrorSeverity.Critical) {
            ConfigurationKey = configKey ?? string.Empty;
            ErrorCode = "CFG_001";
            UserFriendlyMessage = "系统配置错误，请联系管理员";
            IsRetryable = false;
        }
    }

    /// <summary>
    /// 操作超时异常
    /// </summary>
    [Serializable]
    public class OperationTimeoutException : AppException {
        public TimeSpan Timeout { get; set; }
        public string Operation { get; set; } = string.Empty;

        public OperationTimeoutException(string message, TimeSpan timeout, string? operation = null)
            : base(message, ErrorCategory.Timeout, ErrorSeverity.Warning) {
            Timeout = timeout;
            Operation = operation ?? string.Empty;
            ErrorCode = "TIME_001";
            UserFriendlyMessage = $"操作超时（{timeout.TotalSeconds}秒），请重试";
        }
    }

    /// <summary>
    /// 并发冲突异常
    /// </summary>
    [Serializable]
    public class ConcurrencyException : AppException {
        public string EntityType { get; set; } = string.Empty;
        public object EntityId { get; set; } = new object();
        public string ConflictingUser { get; set; } = string.Empty;

        public ConcurrencyException(string message, string? entityType = null, object? entityId = null)
            : base(message, ErrorCategory.Concurrency, ErrorSeverity.Warning) {
            EntityType = entityType ?? string.Empty;
            EntityId = entityId ?? new object();
            ErrorCode = "CONC_001";
            UserFriendlyMessage = "数据已被其他用户修改，请刷新后重试";
        }
    }

    /// <summary>
    /// 资源未找到异常
    /// </summary>
    [Serializable]
    public class ResourceNotFoundException : AppException {
        public string ResourceType { get; set; } = string.Empty;
        public object ResourceId { get; set; } = new object();

        public ResourceNotFoundException(string message, string? resourceType = null, object? resourceId = null)
            : base(message, ErrorCategory.ResourceNotFound, ErrorSeverity.Warning) {
            ResourceType = resourceType ?? string.Empty;
            ResourceId = resourceId ?? new object();
            ErrorCode = "RES_404";
            UserFriendlyMessage = $"请求的{resourceType ?? "资源"}不存在";
            IsRetryable = false;
        }
    }
}
