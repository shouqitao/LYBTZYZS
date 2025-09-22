using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 诊断输出控制服务 - 统一管理应用程序的调试输出
    /// 提供集中的诊断日志控制，支持按级别和分类过滤
    /// </summary>
    public static class DiagnosticService
    {
        private static bool _isEnabled = true;
        private static DiagnosticLevel _minimumLevel = DiagnosticLevel.Debug;

        /// <summary>
        /// 诊断输出级别
        /// </summary>
        public enum DiagnosticLevel
        {
            Trace = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Critical = 5,
            None = 99
        }

        /// <summary>
        /// 获取或设置是否启用诊断输出
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// 获取或设置最低输出级别
        /// </summary>
        public static DiagnosticLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        /// <summary>
        /// 输出跟踪级别信息
        /// </summary>
        [Conditional("DEBUG")]
        public static void Trace(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog(DiagnosticLevel.Trace, message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 输出调试级别信息
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog(DiagnosticLevel.Debug, message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 输出信息级别消息
        /// </summary>
        [Conditional("DEBUG")]
        public static void Info(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog(DiagnosticLevel.Info, message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 输出警告级别消息
        /// </summary>
        [Conditional("DEBUG")]
        public static void Warning(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog(DiagnosticLevel.Warning, message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 输出错误级别消息
        /// </summary>
        [Conditional("DEBUG")]
        public static void Error(string message, Exception? exception = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fullMessage = exception != null
                ? $"{message}\nException: {SanitizeException(exception)}"
                : message;

            WriteLog(DiagnosticLevel.Error, fullMessage, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 输出严重错误级别消息
        /// </summary>
        [Conditional("DEBUG")]
        public static void Critical(string message, Exception? exception = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fullMessage = exception != null
                ? $"{message}\nException: {SanitizeException(exception)}"
                : message;

            WriteLog(DiagnosticLevel.Critical, fullMessage, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 条件性输出（仅在条件为真时输出）
        /// </summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!condition)
            {
                WriteLog(DiagnosticLevel.Error, $"Assertion Failed: {message}", memberName, sourceFilePath, sourceLineNumber);
            }
        }

        /// <summary>
        /// 性能测量开始
        /// </summary>
        public static IDisposable MeasurePerformance(string operationName,
            [CallerMemberName] string memberName = "")
        {
#if DEBUG
            return new PerformanceMeasurement(operationName, memberName);
#else
            // 在发布版本中返回空实现
            return new NullPerformanceMeasurement();
#endif
        }

        private static void WriteLog(DiagnosticLevel level, string message,
            string memberName, string sourceFilePath, int sourceLineNumber)
        {
            if (!_isEnabled || level < _minimumLevel)
            {
                return;
            }

            // 从文件路径中提取文件名（安全处理）
            var fileName = System.IO.Path.GetFileName(sourceFilePath);

            // 构建格式化的输出消息
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelIcon = GetLevelIcon(level);
            var formattedMessage = $"[{timestamp}] {levelIcon} [{fileName}:{sourceLineNumber}] {memberName}: {message}";

            // 输出到调试窗口
            System.Diagnostics.Debug.WriteLine(formattedMessage);
        }

        private static string GetLevelIcon(DiagnosticLevel level)
        {
            return level switch
            {
                DiagnosticLevel.Trace => "🔍",
                DiagnosticLevel.Debug => "🐛",
                DiagnosticLevel.Info => "ℹ️",
                DiagnosticLevel.Warning => "⚠️",
                DiagnosticLevel.Error => "❌",
                DiagnosticLevel.Critical => "🔥",
                _ => "📝"
            };
        }

        private static string SanitizeException(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            // 脱敏异常信息，移除可能的敏感路径和数据
            var message = exception.Message;

            // 移除完整路径
            message = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"[A-Z]:\\[^:\r\n]*\\([^\\:\r\n]+)",
                "$1");

            // 移除可能的敏感信息
            message = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"(password|token|key|secret)[\s]*[:=][\s]*[^\s\r\n]+",
                "$1=[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return $"{exception.GetType().Name}: {message}";
        }

        /// <summary>
        /// 性能测量辅助类
        /// </summary>
        private class PerformanceMeasurement : IDisposable
        {
            private readonly string _operationName;
            private readonly string _memberName;
            private readonly Stopwatch _stopwatch;

            public PerformanceMeasurement(string operationName, string memberName)
            {
                _operationName = operationName;
                _memberName = memberName;
                _stopwatch = Stopwatch.StartNew();

                DiagnosticService.Debug($"Performance: Starting {operationName}");
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                DiagnosticService.Debug($"Performance: {_operationName} completed in {_stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// 空的性能测量实现（用于发布版本）
        /// </summary>
        private class NullPerformanceMeasurement : IDisposable
        {
            public void Dispose()
            {
                // 空实现，在发布版本中不做任何事
            }
        }

        /// <summary>
        /// 配置诊断服务
        /// </summary>
        public static void Configure(bool enabled = true, DiagnosticLevel minimumLevel = DiagnosticLevel.Debug)
        {
            _isEnabled = enabled;
            _minimumLevel = minimumLevel;

#if DEBUG
            Info($"Diagnostic Service configured: Enabled={enabled}, MinimumLevel={minimumLevel}");
#endif
        }

        /// <summary>
        /// 在发布版本中完全禁用诊断输出
        /// </summary>
        static DiagnosticService()
        {
#if !DEBUG
            // 在发布版本中默认禁用诊断输出
            _isEnabled = false;
            _minimumLevel = DiagnosticLevel.None;
#else
            // 在调试版本中输出初始化信息
            Debug("Diagnostic Service initialized (DEBUG mode)");
#endif
        }
    }
}
