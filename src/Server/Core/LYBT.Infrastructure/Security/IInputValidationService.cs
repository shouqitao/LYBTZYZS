namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 输入验证服务接口
    /// </summary>
    public interface IInputValidationService
    {
        /// <summary>
        /// 验证并净化用户输入
        /// </summary>
        ValidationResult ValidateAndSanitize(string input, InputType inputType);

        /// <summary>
        /// 检测SQL注入
        /// </summary>
        bool IsSqlInjection(string input);

        /// <summary>
        /// 检测XSS攻击
        /// </summary>
        bool IsXssAttack(string input);

        /// <summary>
        /// 检测路径遍历攻击
        /// </summary>
        bool IsPathTraversal(string input);

        /// <summary>
        /// 检测命令注入
        /// </summary>
        bool IsCommandInjection(string input);

        /// <summary>
        /// HTML编码
        /// </summary>
        string HtmlEncode(string input);

        /// <summary>
        /// HTML解码
        /// </summary>
        string HtmlDecode(string input);

        /// <summary>
        /// URL编码
        /// </summary>
        string UrlEncode(string input);
    }

    /// <summary>
    /// 输入类型
    /// </summary>
    public enum InputType
    {
        General = 1,
        Html = 2,
        Sql = 3,
        FileName = 4,
        Url = 5,
        Email = 6,
        Json = 7
    }

    /// <summary>
    /// 威胁类型
    /// </summary>
    public enum ThreatType
    {
        None = 0,
        SqlInjection = 1,
        XssAttack = 2,
        PathTraversal = 3,
        CommandInjection = 4,
        MalformedInput = 5
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public string? OriginalValue { get; set; }
        public string? SanitizedValue { get; set; }
        public InputType InputType { get; set; }
        public ThreatType ThreatType { get; set; } = ThreatType.None;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}