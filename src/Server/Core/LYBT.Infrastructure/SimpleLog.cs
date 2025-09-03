namespace LYBT.Infrastructure
{
    /// <summary>
    /// 极简日志模型 - UltraThink重构：删除冗余，保留核心
    /// </summary>
    public class SimpleLog
    {
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? UserId { get; set; }
    }
}