namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 日志记录结构（可用于数据库存储扩展）
    /// </summary>
    public class LogEntry {
        public string Module { get; set; }
        public string Action { get; set; }
        public string Operator { get; set; }
        public DateTime Time { get; set; }
        public string Content { get; set; }
        public string IpAddress { get; set; }
        public string Type { get; set; }
    }
}