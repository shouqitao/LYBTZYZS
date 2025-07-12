using LYBT.Common.Enums.Logs;

namespace LYBT.Common.Helpers {

    /// <summary>
    /// 日志工具类（简化示例）
    /// </summary>
    public static class LogHelper {

        public static void Write(LogType type, string content) {
            Console.WriteLine($"[{type}] - {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {content}");
            // 实际项目中可写入文件、数据库或外部日志系统
        }
    }
}