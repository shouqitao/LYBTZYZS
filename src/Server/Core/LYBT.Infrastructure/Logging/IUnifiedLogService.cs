/// <summary>
/// P3-Fix 统一日志服务接口 - 解决测试项目编译错误
/// 最小化接口定义，仅用于编译通过
/// </summary>

namespace LYBT.Infrastructure.Logging
{
    /// <summary>
    /// 统一日志服务接口
    /// </summary>
    public interface IUnifiedLogService
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        void LogInformation(string message, params object[] args);
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        void LogWarning(string message, params object[] args);
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        void LogError(string message, params object[] args);
        
        /// <summary>
        /// 记录异常日志
        /// </summary>
        void LogError(Exception exception, string message, params object[] args);
        
        /// <summary>
        /// 记录调试日志
        /// </summary>
        void LogDebug(string message, params object[] args);
    }
}