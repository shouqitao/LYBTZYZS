namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 业务规则验证结果
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 验证是否成功
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 验证规则名称
        /// </summary>
        public string? RuleName { get; set; }

        /// <summary>
        /// 验证详情
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new();

        /// <summary>
        /// 验证时间
        /// </summary>
        public DateTime ValidationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static ValidationResult Failure(string message, string? ruleName = null)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = message,
                RuleName = ruleName
            };
        }

        /// <summary>
        /// 创建带详情的成功结果
        /// </summary>
        public static ValidationResult Success(string ruleName, params (string Key, object Value)[] details)
        {
            var result = Success();
            result.RuleName = ruleName;
            foreach (var (key, value) in details)
            {
                result.Details[key] = value;
            }
            return result;
        }

        /// <summary>
        /// 创建带详情的失败结果
        /// </summary>
        public static ValidationResult Failure(string message, string ruleName, params (string Key, object Value)[] details)
        {
            var result = Failure(message, ruleName);
            foreach (var (key, value) in details)
            {
                result.Details[key] = value;
            }
            return result;
        }

        /// <summary>
        /// 添加详情
        /// </summary>
        public ValidationResult WithDetail(string key, object value)
        {
            Details[key] = value;
            return this;
        }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
            {
                return $"Validation succeeded (Rule: {RuleName})";
            }
            return $"Validation failed: {ErrorMessage} (Rule: {RuleName})";
        }
    }
}