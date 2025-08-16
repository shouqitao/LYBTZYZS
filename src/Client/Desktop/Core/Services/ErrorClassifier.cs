using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Data.SqlClient;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Exceptions;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 错误分类器 - 识别和分类异常
    /// </summary>
    public class ErrorClassifier : IErrorClassifier
    {
        private readonly ILogger<ErrorClassifier>? _logger;
        
        public ErrorClassifier(ILogger<ErrorClassifier>? logger = null)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// 分类异常
        /// </summary>
        public AppException ClassifyException(Exception exception)
        {
            // 如果已经是AppException，直接返回
            if (exception is AppException appEx)
            {
                _logger?.LogDebug("异常已分类: {Category} - {Severity}", appEx.Category, appEx.Severity);
                return appEx;
            }
            
            // 分析异常类型并分类
            var classified = exception switch
            {
                // 网络相关
                HttpRequestException httpEx => ClassifyHttpException(httpEx),
                WebException webEx => ClassifyWebException(webEx),
                SocketException socketEx => ClassifySocketException(socketEx),
                TaskCanceledException when IsTimeout(exception) => new Exceptions.OperationTimeoutException(
                    "操作超时", TimeSpan.FromSeconds(30), "HTTP请求"),
                
                // 数据访问相关
                SqlException sqlEx => ClassifySqlException(sqlEx),
                
                // 文件系统相关（更具体的异常先匹配）
                DirectoryNotFoundException => new AppException(
                    "目录不存在", ErrorCategory.FileSystem, ErrorSeverity.Warning, exception),
                FileNotFoundException => new AppException(
                    "文件不存在", ErrorCategory.FileSystem, ErrorSeverity.Warning, exception),
                IOException ioEx => ClassifyIOException(ioEx),
                UnauthorizedAccessException => new AppException(
                    "文件访问被拒绝", ErrorCategory.FileSystem, ErrorSeverity.Warning, exception),
                
                // 安全相关
                SecurityException secEx => new AuthorizationException(
                    "安全权限不足", secEx.PermissionType?.ToString()),
                
                // 参数验证
                ArgumentNullException argNullEx => new ValidationException(
                    $"参数不能为空: {argNullEx.ParamName}", argNullEx.ParamName),
                ArgumentException argEx => new ValidationException(
                    $"参数无效: {argEx.ParamName}", argEx.ParamName),
                FormatException => new ValidationException(
                    "数据格式不正确"),
                InvalidOperationException => new BusinessException(
                    "操作无效: " + exception.Message),
                
                // 并发相关
                System.Data.DBConcurrencyException => new ConcurrencyException(
                    "数据并发冲突"),
                
                // 配置相关
                System.Configuration.ConfigurationErrorsException => new ConfigurationException(
                    "配置错误: " + exception.Message),
                
                // 默认
                _ => new AppException(
                    exception.Message ?? "未知错误",
                    ErrorCategory.Unknown,
                    DetermineSeverity(exception),
                    exception)
            };
            
            // 设置技术详情
            classified.TechnicalDetails = BuildTechnicalDetails(exception);
            
            _logger?.LogDebug("异常分类完成: {Type} -> {Category} - {Severity}", 
                exception.GetType().Name, classified.Category, classified.Severity);
            
            return classified;
        }
        
        /// <summary>
        /// 分类HTTP异常
        /// </summary>
        private NetworkException ClassifyHttpException(HttpRequestException exception)
        {
            var message = exception.Message;
            int? statusCode = null;
            
            // 尝试从内部异常获取状态码
            if (exception.InnerException is WebException webEx && 
                webEx.Response is HttpWebResponse response)
            {
                statusCode = (int)response.StatusCode;
            }
            
            return new NetworkException(message, null, statusCode)
            {
                Severity = DetermineNetworkSeverity(statusCode)
            };
        }
        
        /// <summary>
        /// 分类Web异常
        /// </summary>
        private NetworkException ClassifyWebException(WebException exception)
        {
            var statusCode = exception.Response is HttpWebResponse response 
                ? (int?)response.StatusCode 
                : null;
                
            return new NetworkException(
                exception.Message,
                exception.Response?.ResponseUri?.ToString(),
                statusCode)
            {
                Severity = DetermineNetworkSeverity(statusCode)
            };
        }
        
        /// <summary>
        /// 分类Socket异常
        /// </summary>
        private NetworkException ClassifySocketException(SocketException exception)
        {
            var message = exception.SocketErrorCode switch
            {
                SocketError.HostNotFound => "无法找到服务器",
                SocketError.ConnectionRefused => "连接被拒绝",
                SocketError.TimedOut => "连接超时",
                SocketError.NetworkUnreachable => "网络不可达",
                _ => $"网络错误: {exception.SocketErrorCode}"
            };
            
            return new NetworkException(message, null, (int)exception.SocketErrorCode)
            {
                Severity = ErrorSeverity.Error
            };
        }
        
        /// <summary>
        /// 分类SQL异常
        /// </summary>
        private DataAccessException ClassifySqlException(SqlException exception)
        {
            var message = exception.Number switch
            {
                2627 => "数据重复，违反唯一约束",
                547 => "外键约束冲突",
                2601 => "违反唯一索引",
                -2 => "连接超时",
                18456 => "登录失败",
                _ => exception.Message
            };
            
            return new DataAccessException(message, null, null)
            {
                ErrorCode = $"SQL_{exception.Number}",
                Severity = exception.Number == -2 ? ErrorSeverity.Warning : ErrorSeverity.Error
            };
        }
        
        /// <summary>
        /// 分类IO异常
        /// </summary>
        private AppException ClassifyIOException(IOException exception)
        {
            if (exception.Message.Contains("正在被另一个进程使用"))
            {
                return new AppException(
                    "文件被占用",
                    ErrorCategory.FileSystem,
                    ErrorSeverity.Warning,
                    exception)
                {
                    UserFriendlyMessage = "文件正在被使用，请稍后重试"
                };
            }
            
            if (exception.Message.Contains("磁盘空间不足"))
            {
                return new AppException(
                    "磁盘空间不足",
                    ErrorCategory.FileSystem,
                    ErrorSeverity.Critical,
                    exception);
            }
            
            return new AppException(
                "文件操作失败",
                ErrorCategory.FileSystem,
                ErrorSeverity.Error,
                exception);
        }
        
        /// <summary>
        /// 判断是否为超时异常
        /// </summary>
        private bool IsTimeout(Exception exception)
        {
            return exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("超时", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 确定网络错误严重程度
        /// </summary>
        private ErrorSeverity DetermineNetworkSeverity(int? statusCode)
        {
            if (!statusCode.HasValue)
                return ErrorSeverity.Error;
                
            return statusCode.Value switch
            {
                >= 500 => ErrorSeverity.Critical,  // 服务器错误
                429 => ErrorSeverity.Warning,      // 请求过多
                401 or 403 => ErrorSeverity.Warning, // 认证/授权
                404 => ErrorSeverity.Info,         // 资源不存在
                _ => ErrorSeverity.Error
            };
        }
        
        /// <summary>
        /// 确定异常严重程度
        /// </summary>
        private ErrorSeverity DetermineSeverity(Exception exception)
        {
            // 基于异常类型确定严重程度
            return exception switch
            {
                OutOfMemoryException => ErrorSeverity.Fatal,
                StackOverflowException => ErrorSeverity.Fatal,
                AccessViolationException => ErrorSeverity.Fatal,
                SystemException => ErrorSeverity.Critical,
                _ => ErrorSeverity.Error
            };
        }
        
        /// <summary>
        /// 构建技术详情
        /// </summary>
        private string BuildTechnicalDetails(Exception exception)
        {
            var details = $"异常类型: {exception.GetType().FullName}\n";
            details += $"消息: {exception.Message}\n";
            
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                details += $"堆栈跟踪:\n{exception.StackTrace}\n";
            }
            
            if (exception.InnerException != null)
            {
                details += $"\n内部异常:\n{BuildTechnicalDetails(exception.InnerException)}";
            }
            
            return details;
        }
    }
    
    /// <summary>
    /// 错误分类器接口
    /// </summary>
    public interface IErrorClassifier
    {
        AppException ClassifyException(Exception exception);
    }
}