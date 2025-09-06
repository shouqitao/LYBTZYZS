using System.Net;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 异常工厂 - UltraThink统一异常体系
/// 提供便捷的异常创建方法
/// </summary>
public static class ExceptionFactory {

    #region Business Exceptions

    /// <summary>
    /// 创建业务异常
    /// </summary>
    public static BusinessException Business(string message, string? businessRule = null)
        => new(message, businessRule ?? "UnknownRule");

    /// <summary>
    /// 创建业务异常（带错误码）
    /// </summary>
    public static BusinessException Business(string message, string errorCode, string businessRule)
        => new(message, errorCode, businessRule);

    #endregion Business Exceptions

    #region Validation Exceptions

    /// <summary>
    /// 创建验证异常
    /// </summary>
    public static ValidationException Validation(string message) => new(message);

    /// <summary>
    /// 创建字段验证异常
    /// </summary>
    public static ValidationException Validation(string fieldName, string errorMessage)
        => new(fieldName, errorMessage);

    /// <summary>
    /// 创建多字段验证异常
    /// </summary>
    public static ValidationException Validation(Dictionary<string, string[]> errors)
        => new("数据验证失败", errors);

    #endregion Validation Exceptions

    #region Not Found Exceptions

    /// <summary>
    /// 创建资源不存在异常
    /// </summary>
    public static NotFoundException NotFound(string resourceType, Guid resourceId)
        => new(resourceType, resourceId);

    /// <summary>
    /// 创建资源不存在异常
    /// </summary>
    public static NotFoundException NotFound(string message) => new(message);

    #endregion Not Found Exceptions

    #region API Exceptions

    /// <summary>
    /// 创建API异常
    /// </summary>
    public static ApiException Api(HttpStatusCode statusCode, string? responseContent = null)
        => new(statusCode, responseContent);

    /// <summary>
    /// 创建API异常（带请求信息）
    /// </summary>
    public static ApiException Api(HttpStatusCode statusCode, string requestUrl, string httpMethod, string? responseContent = null)
        => new(statusCode, requestUrl, httpMethod, responseContent);

    /// <summary>
    /// 创建未授权异常
    /// </summary>
    public static ApiException Unauthorized() => ApiException.Unauthorized();

    /// <summary>
    /// 创建禁止访问异常
    /// </summary>
    public static ApiException Forbidden() => ApiException.Forbidden();

    /// <summary>
    /// 创建服务不可用异常
    /// </summary>
    public static ApiException ServiceUnavailable() => ApiException.ServiceUnavailable();

    /// <summary>
    /// 创建请求超时异常
    /// </summary>
    public static ApiException Timeout() => ApiException.Timeout();

    #endregion API Exceptions

    #region Application Exceptions

    /// <summary>
    /// 创建应用程序异常
    /// </summary>
    public static AppException App(string message, string? errorCode = null)
        => new(message, errorCode);

    /// <summary>
    /// 创建应用程序异常（带内部异常）
    /// </summary>
    public static AppException App(string message, Exception innerException, string? errorCode = null)
        => new(message, innerException, errorCode);

    #endregion Application Exceptions

    #region 常用业务场景

    /// <summary>
    /// 用户相关异常
    /// </summary>
    public static class User {

        public static NotFoundException NotFound(Guid userId) => NotFoundException.User(userId);

        public static BusinessException AlreadyExists(string username) => Business($"用户名 {username} 已存在", "USER_ALREADY_EXISTS");

        public static BusinessException InvalidCredentials() => Business("用户名或密码错误", "INVALID_CREDENTIALS");

        public static BusinessException AccountLocked() => Business("账户已被锁定", "ACCOUNT_LOCKED");
    }

    /// <summary>
    /// 患者相关异常
    /// </summary>
    public static class Patient {

        public static NotFoundException NotFound(Guid patientId) => NotFoundException.Patient(patientId);

        public static BusinessException AlreadyExists(string name, string phone) => Business($"患者 {name} (电话: {phone}) 已存在", "PATIENT_ALREADY_EXISTS");
    }

    /// <summary>
    /// 药材相关异常
    /// </summary>
    public static class Herb {

        public static NotFoundException NotFound(Guid herbId) => NotFoundException.Herb(herbId);

        public static BusinessException InsufficientStock(string herbName, int required, int available) => Business($"药材 {herbName} 库存不足，需要 {required}，可用 {available}", "INSUFFICIENT_STOCK");
    }

    /// <summary>
    /// 处方相关异常
    /// </summary>
    public static class Prescription {

        public static NotFoundException NotFound(Guid prescriptionId) => NotFoundException.Prescription(prescriptionId);

        public static BusinessException AlreadyProcessed() => Business("处方已处理，无法修改", "PRESCRIPTION_PROCESSED");
    }

    #endregion 常用业务场景
}
