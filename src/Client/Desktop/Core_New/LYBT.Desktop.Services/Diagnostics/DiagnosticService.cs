using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LYBT.Desktop.Services.Diagnostics
{
    /// <summary>
    /// 简化的诊断输出控制服务 - 遵循"适度设计、拒绝过度工程"原则
    /// 提供核心的调试输出功能，避免过度复杂的格式化和脱敏
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
                ? $"{message} - Exception: {exception.GetType().Name}: {exception.Message}"
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
                ? $"{message} - Exception: {exception.GetType().Name}: {exception.Message}"
                : message;

            WriteLog(DiagnosticLevel.Critical, fullMessage, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 简化的性能测量
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

        /// <summary>
        /// 配置诊断服务
        /// </summary>
        public static void Configure(bool enabled = true, DiagnosticLevel minimumLevel = DiagnosticLevel.Debug)
        {
            _isEnabled = enabled;
            _minimumLevel = minimumLevel;

#if DEBUG
            Info($"诊断服务已配置: 启用={enabled}, 最低级别={minimumLevel}");
#endif
        }

        private static void WriteLog(DiagnosticLevel level, string message,
            string memberName, string sourceFilePath, int sourceLineNumber)
        {
            if (!_isEnabled || level < _minimumLevel)
            {
                return;
            }

            // 简化的输出格式
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelName = level.ToString();
            var formattedMessage = $"[{timestamp}] {levelName} [{fileName}:{sourceLineNumber}] {memberName}: {message}";

            // 输出到调试窗口
            System.Diagnostics.Debug.WriteLine(formattedMessage);
        }

        /// <summary>
        /// 性能测量辅助类
        /// </summary>
        private class PerformanceMeasurement : IDisposable
        {
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;

            public PerformanceMeasurement(string operationName, string memberName)
            {
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
                Debug($"性能测量开始: {operationName}");
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                Debug($"性能测量完成: {_operationName} 耗时 {_stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// 空的性能测量实现（用于发布版本）
        /// </summary>
        private class NullPerformanceMeasurement : IDisposable
        {
            public void Dispose()
            {
                // 空实现
            }
        }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static DiagnosticService()
        {
#if !DEBUG
            // 在发布版本中默认禁用诊断输出
            _isEnabled = false;
            _minimumLevel = DiagnosticLevel.None;
#else
            // 在调试版本中输出初始化信息
            Debug("诊断服务已初始化 (DEBUG模式)");
#endif
        }
    }
}
