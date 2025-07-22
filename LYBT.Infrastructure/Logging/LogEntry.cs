namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 日志记录结构（可用于数据库存储扩展）
    /// </summary>
    public class LogEntry {
/// <summary>
/// Module 属性。
/// </summary>
        public string Module { get; set; }
/// <summary>
/// Action 属性。
/// </summary>
        public string Action { get; set; }
/// <summary>
/// Operator 属性。
/// </summary>
        public string Operator { get; set; }
/// <summary>
/// Time 属性。
/// </summary>
        public DateTime Time { get; set; }
/// <summary>
/// Content 属性。
/// </summary>
        public string Content { get; set; }
/// <summary>
/// IpAddress 属性。
/// </summary>
        public string IpAddress { get; set; }
/// <summary>
/// Type 属性。
/// </summary>
        public string Type { get; set; }
    }
}
