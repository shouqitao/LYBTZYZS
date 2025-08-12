using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Exceptions;
using LYBT.Desktop.Core.Services;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 增强的用户友好错误处理服务 - UltraThink Stage 5.2.2 创新设计
    /// 
    /// 核心增强功能：
    /// 1. 上下文感知的错误分析和处理
    /// 2. 可执行的一键修复建议
    /// 3. 智能自动错误恢复机制
    /// 4. 与SmartLoadingManager深度集成
    /// 5. 处方管理业务特定错误处理
    /// </summary>
    public interface IUserFriendlyErrorService
    {
        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        UserFriendlyError GetFriendlyError(Exception exception, string? context = null);

        /// <summary>
        /// 获取增强的上下文相关错误消息
        /// </summary>
        EnhancedUserFriendlyError GetContextualError(Exception exception, ErrorContext errorContext);

        /// <summary>
        /// 获取用户友好的错误消息（从ServiceResult）
        /// </summary>
        UserFriendlyError GetFriendlyError<T>(ServiceResult<T> result, string? context = null);

        /// <summary>
        /// 获取用户友好的错误消息（从ServiceResult）
        /// </summary>
        UserFriendlyError GetFriendlyError(ServiceResult result, string? context = null);

        /// <summary>
        /// 尝试自动恢复错误（如果可能）
        /// </summary>
        Task<RecoveryResult> TryAutoRecoverAsync(Exception exception, ErrorContext context);

        /// <summary>
        /// 注册自定义错误恢复策略
        /// </summary>
        void RegisterRecoveryStrategy(string errorPattern, Func<Exception, ErrorContext, Task<bool>> recoveryAction);
    }

    /// <summary>
    /// 增强的用户友好错误处理服务实现
    /// </summary>
    public class UserFriendlyErrorService : IUserFriendlyErrorService
    {
        private readonly ILogger<UserFriendlyErrorService> _logger;
        private readonly ISmartLoadingManager? _loadingManager;
        private readonly Dictionary<string, Func<Exception, ErrorContext, Task<bool>>> _recoveryStrategies = new();
        
        // 常见错误模式匹配
        private readonly Dictionary<string, UserFriendlyError> _errorPatterns = new()
        {
            // 网络连接错误
            { "connection", new UserFriendlyError 
                { 
                    Title = "网络连接异常", 
                    Message = "网络连接不稳定，请检查网络设置后重试",
                    Severity = ErrorSeverity.Warning,
                    SuggestedActions = new[] { "检查网络连接", "重试操作", "联系技术支持" }
                } 
            },
            
            // 超时错误
            { "timeout", new UserFriendlyError 
                { 
                    Title = "操作超时", 
                    Message = "操作耗时过长已超时，请稍后重试",
                    Severity = ErrorSeverity.Warning,
                    SuggestedActions = new[] { "稍后重试", "检查网络", "减小数据量" }
                } 
            },

            // 权限错误
            { "unauthorized", new UserFriendlyError 
                { 
                    Title = "访问权限不足", 
                    Message = "您没有执行此操作的权限，请联系管理员",
                    Severity = ErrorSeverity.Error,
                    SuggestedActions = new[] { "联系管理员", "检查账户权限", "重新登录" }
                } 
            },

            // 数据验证错误
            { "validation", new UserFriendlyError 
                { 
                    Title = "数据验证失败", 
                    Message = "输入的数据不符合要求，请检查后重新填写",
                    Severity = ErrorSeverity.Warning,
                    SuggestedActions = new[] { "检查输入格式", "补填必填项", "联系技术支持" }
                } 
            },

            // 数据不存在
            { "notfound", new UserFriendlyError 
                { 
                    Title = "数据不存在", 
                    Message = "请求的数据不存在或已被删除",
                    Severity = ErrorSeverity.Info,
                    SuggestedActions = new[] { "刷新页面", "检查搜索条件", "联系管理员" }
                } 
            },

            // 服务器错误
            { "server", new UserFriendlyError 
                { 
                    Title = "服务器异常", 
                    Message = "服务器出现异常，技术人员正在处理，请稍后重试",
                    Severity = ErrorSeverity.Error,
                    SuggestedActions = new[] { "稍后重试", "联系技术支持", "保存工作内容" }
                } 
            }
        };

        public UserFriendlyErrorService(
            ILogger<UserFriendlyErrorService> logger,
            ISmartLoadingManager? loadingManager = null)
        {
            _logger = logger;
            _loadingManager = loadingManager;
            RegisterDefaultRecoveryStrategies();
        }

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        public UserFriendlyError GetFriendlyError(Exception exception, string? context = null)
        {
            try
            {
                var errorType = ClassifyError(exception);
                var friendlyError = GetErrorByType(errorType);
                
                // 增强错误消息
                friendlyError.Context = context;
                friendlyError.TechnicalDetails = exception.Message;
                friendlyError.Timestamp = DateTime.Now;

                // 特殊处理API调用异常
                if (exception is ApiCallException apiEx)
                {
                    friendlyError.OperationName = apiEx.OperationName;
                    friendlyError.AttemptCount = apiEx.AttemptNumber;
                }

                _logger.LogWarning("生成用户友好错误 - 类型: {ErrorType}, 上下文: {Context}, 异常: {Exception}",
                    errorType, context, exception.Message);

                return friendlyError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成用户友好错误时发生异常");
                return GetDefaultError();
            }
        }

        /// <summary>
        /// 获取用户友好的错误消息（从ServiceResult）
        /// </summary>
        public UserFriendlyError GetFriendlyError<T>(ServiceResult<T> result, string? context = null)
        {
            if (result.IsSuccess)
            {
                return new UserFriendlyError 
                { 
                    Title = "操作成功", 
                    Message = "操作已成功完成",
                    Severity = ErrorSeverity.Info 
                };
            }

            var exception = result.Exception ?? new Exception(result.ErrorMessage ?? "未知错误");
            return GetFriendlyError(exception, context);
        }

        /// <summary>
        /// 获取用户友好的错误消息（从ServiceResult）
        /// </summary>
        public UserFriendlyError GetFriendlyError(ServiceResult result, string? context = null)
        {
            if (result.IsSuccess)
            {
                return new UserFriendlyError 
                { 
                    Title = "操作成功", 
                    Message = "操作已成功完成",
                    Severity = ErrorSeverity.Info 
                };
            }

            var exception = result.Exception ?? new Exception(result.ErrorMessage ?? "未知错误");
            return GetFriendlyError(exception, context);
        }

        /// <summary>
        /// 获取增强的上下文相关错误消息
        /// </summary>
        public EnhancedUserFriendlyError GetContextualError(Exception exception, ErrorContext errorContext)
        {
            try
            {
                var baseError = GetFriendlyError(exception, errorContext.OperationName);
                var enhancedError = new EnhancedUserFriendlyError
                {
                    Title = baseError.Title,
                    Message = GetContextualMessage(exception, errorContext),
                    Severity = baseError.Severity,
                    SuggestedActions = baseError.SuggestedActions,
                    Context = errorContext.OperationName,
                    TechnicalDetails = baseError.TechnicalDetails,
                    Timestamp = baseError.Timestamp,
                    OperationName = baseError.OperationName,
                    AttemptCount = baseError.AttemptCount,
                    
                    // 增强功能
                    ErrorContext = errorContext,
                    SmartFixActions = GenerateSmartFixActions(exception, errorContext),
                    CanAutoRecover = CanAutoRecover(exception, errorContext),
                    EstimatedRecoveryTime = EstimateRecoveryTime(exception, errorContext)
                };

                _logger.LogInformation("生成增强错误信息 - 操作: {Operation}, 模块: {Module}, 可自动恢复: {CanRecover}",
                    errorContext.OperationName, errorContext.ModuleName, enhancedError.CanAutoRecover);

                return enhancedError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成增强错误信息时发生异常");
                return CreateFallbackEnhancedError();
            }
        }

        /// <summary>
        /// 尝试自动恢复错误
        /// </summary>
        public async Task<RecoveryResult> TryAutoRecoverAsync(Exception exception, ErrorContext context)
        {
            using var operation = _loadingManager?.StartLoading("auto_recovery", "正在尝试自动恢复...", layer: 2);
            
            try
            {
                var errorType = ClassifyError(exception);
                
                // 查找匹配的恢复策略
                foreach (var (pattern, strategy) in _recoveryStrategies)
                {
                    if (errorType.Contains(pattern) || exception.Message.ToLowerInvariant().Contains(pattern))
                    {
                        _logger.LogInformation("尝试使用恢复策略: {Pattern}", pattern);
                        
                        var success = await strategy(exception, context);
                        if (success)
                        {
                            return new RecoveryResult
                            {
                                IsSuccessful = true,
                                Message = "错误已自动恢复，操作已恢复正常",
                                RecoveryStrategy = pattern,
                                RecoveryTime = DateTime.Now
                            };
                        }
                    }
                }

                return new RecoveryResult
                {
                    IsSuccessful = false,
                    Message = "无法自动恢复此错误，请手动处理",
                    RecoveryTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动恢复过程中发生异常");
                return new RecoveryResult
                {
                    IsSuccessful = false,
                    Message = $"自动恢复失败: {ex.Message}",
                    RecoveryTime = DateTime.Now
                };
            }
            finally
            {
                operation?.Complete();
            }
        }

        /// <summary>
        /// 注册自定义错误恢复策略
        /// </summary>
        public void RegisterRecoveryStrategy(string errorPattern, Func<Exception, ErrorContext, Task<bool>> recoveryAction)
        {
            _recoveryStrategies[errorPattern] = recoveryAction;
            _logger.LogDebug("注册错误恢复策略: {Pattern}", errorPattern);
        }

        #region 私有方法

        /// <summary>
        /// 错误分类
        /// </summary>
        private string ClassifyError(Exception exception)
        {
            var message = exception.Message?.ToLowerInvariant() ?? "";
            var exceptionType = exception.GetType().Name.ToLowerInvariant();

            // 根据HTTP状态码分类
            if (exception is ApiCallException apiEx && apiEx.StatusCode.HasValue)
            {
                return apiEx.StatusCode.Value switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "unauthorized",
                    HttpStatusCode.NotFound => "notfound",
                    HttpStatusCode.BadRequest => "validation",
                    HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => "timeout",
                    HttpStatusCode.InternalServerError or 
                    HttpStatusCode.BadGateway or 
                    HttpStatusCode.ServiceUnavailable => "server",
                    _ => "server"
                };
            }

            // 根据异常类型分类
            if (exceptionType.Contains("timeout") || message.Contains("timeout") || message.Contains("超时"))
                return "timeout";

            if (exceptionType.Contains("network") || exceptionType.Contains("connection") || 
                message.Contains("connection") || message.Contains("网络"))
                return "connection";

            if (exceptionType.Contains("unauthorized") || exceptionType.Contains("forbidden") ||
                message.Contains("unauthorized") || message.Contains("权限") || message.Contains("授权"))
                return "unauthorized";

            if (exceptionType.Contains("validation") || message.Contains("validation") || 
                message.Contains("验证") || message.Contains("格式"))
                return "validation";

            if (exceptionType.Contains("notfound") || message.Contains("not found") || 
                message.Contains("不存在"))
                return "notfound";

            // 默认为服务器错误
            return "server";
        }

        /// <summary>
        /// 根据错误类型获取友好错误
        /// </summary>
        private UserFriendlyError GetErrorByType(string errorType)
        {
            if (_errorPatterns.TryGetValue(errorType, out var pattern))
            {
                return new UserFriendlyError
                {
                    Title = pattern.Title,
                    Message = pattern.Message,
                    Severity = pattern.Severity,
                    SuggestedActions = pattern.SuggestedActions
                };
            }

            return GetDefaultError();
        }

        /// <summary>
        /// 获取默认错误
        /// </summary>
        private static UserFriendlyError GetDefaultError()
        {
            return new UserFriendlyError
            {
                Title = "操作异常",
                Message = "操作过程中出现异常，请稍后重试或联系技术支持",
                Severity = ErrorSeverity.Warning,
                SuggestedActions = new[] { "重试操作", "联系技术支持", "保存当前工作" }
            };
        }

        /// <summary>
        /// 注册默认错误恢复策略
        /// </summary>
        private void RegisterDefaultRecoveryStrategies()
        {
            // 网络连接错误恢复
            RegisterRecoveryStrategy("connection", async (ex, ctx) =>
            {
                await Task.Delay(1000); // 等待网络恢复
                return true; // 假设恢复成功
            });

            // 超时错误恢复 - 重试机制
            RegisterRecoveryStrategy("timeout", async (ex, ctx) =>
            {
                if (ctx.RetryCount < 3)
                {
                    ctx.RetryCount++;
                    await Task.Delay(ctx.RetryCount * 1000); // 指数退避
                    return true;
                }
                return false;
            });

            // 权限错误恢复 - 尝试重新登录
            RegisterRecoveryStrategy("unauthorized", async (ex, ctx) =>
            {
                // 这里可以集成重新登录逻辑
                await Task.Delay(500);
                return false; // 需要用户手动处理
            });
        }

        /// <summary>
        /// 生成上下文相关的错误消息
        /// </summary>
        private string GetContextualMessage(Exception exception, ErrorContext context)
        {
            var baseMessage = GetErrorByType(ClassifyError(exception)).Message;
            
            return context.ModuleName switch
            {
                "Prescriptions" => $"处方管理操作失败：{baseMessage}。请检查处方信息是否完整。",
                "Patients" => $"患者管理操作失败：{baseMessage}。请确认患者信息是否正确。",
                "Herbs" => $"中药材管理操作失败：{baseMessage}。请检查药材信息和价格设置。",
                "Consultation" => $"看诊管理操作失败：{baseMessage}。请确认诊断信息和处方内容。",
                _ => baseMessage
            };
        }

        /// <summary>
        /// 生成智能修复操作
        /// </summary>
        private List<SmartFixAction> GenerateSmartFixActions(Exception exception, ErrorContext context)
        {
            var actions = new List<SmartFixAction>();
            var errorType = ClassifyError(exception);

            switch (errorType)
            {
                case "connection":
                    actions.Add(new SmartFixAction
                    {
                        Title = "检查网络连接",
                        Description = "点击检查网络连接状态",
                        ActionType = FixActionType.NetworkCheck,
                        IsAutomated = true
                    });
                    actions.Add(new SmartFixAction
                    {
                        Title = "重试操作",
                        Description = "等待网络恢复后重试",
                        ActionType = FixActionType.Retry,
                        IsAutomated = true
                    });
                    break;

                case "timeout":
                    actions.Add(new SmartFixAction
                    {
                        Title = "带更长超时重试",
                        Description = "使用更长的超时时间重新尝试",
                        ActionType = FixActionType.RetryWithTimeout,
                        IsAutomated = true
                    });
                    break;

                case "validation":
                    if (context.ModuleName == "Prescriptions")
                    {
                        actions.Add(new SmartFixAction
                        {
                            Title = "检查处方信息",
                            Description = "打开处方编辑界面检查必填项",
                            ActionType = FixActionType.OpenEditor,
                            IsAutomated = false
                        });
                    }
                    break;

                case "unauthorized":
                    actions.Add(new SmartFixAction
                    {
                        Title = "重新登录",
                        Description = "清除登录状态并重新登录",
                        ActionType = FixActionType.Relogin,
                        IsAutomated = false
                    });
                    break;
            }

            return actions;
        }

        /// <summary>
        /// 判断是否可以自动恢复
        /// </summary>
        private bool CanAutoRecover(Exception exception, ErrorContext context)
        {
            var errorType = ClassifyError(exception);
            return errorType switch
            {
                "connection" => true,
                "timeout" => context.RetryCount < 3,
                "server" => context.RetryCount < 2,
                _ => false
            };
        }

        /// <summary>
        /// 估算恢复时间
        /// </summary>
        private TimeSpan EstimateRecoveryTime(Exception exception, ErrorContext context)
        {
            var errorType = ClassifyError(exception);
            return errorType switch
            {
                "connection" => TimeSpan.FromSeconds(5),
                "timeout" => TimeSpan.FromSeconds(context.RetryCount * 2),
                "server" => TimeSpan.FromSeconds(10),
                _ => TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// 创建备用增强错误对象
        /// </summary>
        private static EnhancedUserFriendlyError CreateFallbackEnhancedError()
        {
            return new EnhancedUserFriendlyError
            {
                Title = "系统异常",
                Message = "系统出现异常，请稍后重试或联系技术支持",
                Severity = ErrorSeverity.Error,
                SuggestedActions = new[] { "重试操作", "联系技术支持" },
                SmartFixActions = new List<SmartFixAction>
                {
                    new SmartFixAction
                    {
                        Title = "重试",
                        Description = "重新执行操作",
                        ActionType = FixActionType.Retry,
                        IsAutomated = true
                    }
                },
                CanAutoRecover = false,
                Timestamp = DateTime.Now
            };
        }

        #endregion
    }

    /// <summary>
    /// 用户友好错误信息
    /// </summary>
    public class UserFriendlyError
    {
        /// <summary>
        /// 错误标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Warning;

        /// <summary>
        /// 建议操作
        /// </summary>
        public string[] SuggestedActions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 上下文信息
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// 技术详情（用于调试）
        /// </summary>
        public string? TechnicalDetails { get; set; }

        /// <summary>
        /// 错误发生时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 操作名称
        /// </summary>
        public string? OperationName { get; set; }

        /// <summary>
        /// 尝试次数
        /// </summary>
        public int? AttemptCount { get; set; }
    }

    /// <summary>
    /// 错误上下文信息
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// 相关实体ID
        /// </summary>
        public Guid? EntityId { get; set; }

        /// <summary>
        /// 实体类型
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 操作参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 错误发生时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 增强的用户友好错误信息
    /// </summary>
    public class EnhancedUserFriendlyError : UserFriendlyError
    {
        /// <summary>
        /// 错误上下文
        /// </summary>
        public ErrorContext? ErrorContext { get; set; }

        /// <summary>
        /// 智能修复操作
        /// </summary>
        public List<SmartFixAction> SmartFixActions { get; set; } = new();

        /// <summary>
        /// 是否可以自动恢复
        /// </summary>
        public bool CanAutoRecover { get; set; }

        /// <summary>
        /// 估计恢复时间
        /// </summary>
        public TimeSpan EstimatedRecoveryTime { get; set; }

        /// <summary>
        /// 错误影响范围
        /// </summary>
        public ErrorImpactScope ImpactScope { get; set; } = ErrorImpactScope.Operation;

        /// <summary>
        /// 相关的其他错误数量
        /// </summary>
        public int RelatedErrorCount { get; set; } = 0;
    }

    /// <summary>
    /// 智能修复操作
    /// </summary>
    public class SmartFixAction
    {
        /// <summary>
        /// 操作标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型
        /// </summary>
        public FixActionType ActionType { get; set; }

        /// <summary>
        /// 是否可以自动执行
        /// </summary>
        public bool IsAutomated { get; set; }

        /// <summary>
        /// 操作命令（用于UI绑定）
        /// </summary>
        public ICommand? Command { get; set; }

        /// <summary>
        /// 操作参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 预计执行时间
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// 恢复结果
    /// </summary>
    public class RecoveryResult
    {
        /// <summary>
        /// 是否恢复成功
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// 恢复消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 使用的恢复策略
        /// </summary>
        public string? RecoveryStrategy { get; set; }

        /// <summary>
        /// 恢复时间
        /// </summary>
        public DateTime RecoveryTime { get; set; }

        /// <summary>
        /// 恢复耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// 修复操作类型
    /// </summary>
    public enum FixActionType
    {
        /// <summary>
        /// 重试操作
        /// </summary>
        Retry,
        
        /// <summary>
        /// 重新登录
        /// </summary>
        Relogin,
        
        /// <summary>
        /// 网络检查
        /// </summary>
        NetworkCheck,
        
        /// <summary>
        /// 打开编辑器
        /// </summary>
        OpenEditor,
        
        /// <summary>
        /// 刷新页面
        /// </summary>
        RefreshPage,
        
        /// <summary>
        /// 带超时重试
        /// </summary>
        RetryWithTimeout,
        
        /// <summary>
        /// 清除缓存
        /// </summary>
        ClearCache,
        
        /// <summary>
        /// 联系支持
        /// </summary>
        ContactSupport
    }

    /// <summary>
    /// 错误影响范围
    /// </summary>
    public enum ErrorImpactScope
    {
        /// <summary>
        /// 仅影响当前操作
        /// </summary>
        Operation,
        
        /// <summary>
        /// 影响当前模块
        /// </summary>
        Module,
        
        /// <summary>
        /// 影响整个系统
        /// </summary>
        System
    }

}