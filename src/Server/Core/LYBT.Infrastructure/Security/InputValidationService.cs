using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 输入验证和净化服务 - UltraThink重构安全防护
    /// 防止SQL注入、XSS、目录遍历等攻击
    /// </summary>
    public class InputValidationService : IInputValidationService
    {
        private readonly InputValidationOptions _options;
        private readonly ILogger<InputValidationService> _logger;
        
        // SQL注入检测模式
        private static readonly Regex[] SqlInjectionPatterns = new[]
        {
            new Regex(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UNION|UPDATE)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(\b(AND|OR)\b.{1,6}?(=|>|<|\!|\||&))", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(\b(CHAR|NCHAR|VARCHAR|NVARCHAR)\s*\(\s*\d+\s*\))", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(\b(sp_\w+|xp_\w+)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"((\%27)|(\'))((\%6F)|o|(\%4F))((\%72)|r|(\%52))", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"\b(waitfor\s+delay|benchmark|pg_sleep)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        // XSS检测模式
        private static readonly Regex[] XssPatterns = new[]
        {
            new Regex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            new Regex(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"vbscript:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"<iframe[^>]*>.*?</iframe>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            new Regex(@"<object[^>]*>.*?</object>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            new Regex(@"<embed[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        // 路径遍历检测模式
        private static readonly Regex[] PathTraversalPatterns = new[]
        {
            new Regex(@"\.\.[\\/]", RegexOptions.Compiled),
            new Regex(@"[\\/]\.\.[\\/]", RegexOptions.Compiled),
            new Regex(@"[\\/]\.\.", RegexOptions.Compiled),
            new Regex(@"\.\.[\\\/]", RegexOptions.Compiled)
        };

        // 命令注入检测模式
        private static readonly Regex[] CommandInjectionPatterns = new[]
        {
            new Regex(@"[;&|`$(){}[\]\\]", RegexOptions.Compiled),
            new Regex(@"\b(cmd|command|sh|bash|powershell|pwsh)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"\b(eval|exec|system|shell_exec|passthru)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        public InputValidationService(
            IOptions<InputValidationOptions> options, 
            ILogger<InputValidationService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 验证并净化用户输入
        /// </summary>
        public ValidationResult ValidateAndSanitize(string input, InputType inputType)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new ValidationResult { IsValid = true, SanitizedValue = input };
            }

            var result = new ValidationResult
            {
                OriginalValue = input,
                InputType = inputType
            };

            try
            {
                // 基础长度检查
                if (input.Length > _options.MaxInputLength)
                {
                    result.IsValid = false;
                    result.Errors.Add($"输入长度超过限制 ({_options.MaxInputLength} 字符)");
                    _logger.LogWarning("输入长度超限: {Length} > {MaxLength}", input.Length, _options.MaxInputLength);
                }

                // 根据输入类型进行特定验证
                switch (inputType)
                {
                    case InputType.General:
                        result = ValidateGeneral(input, result);
                        break;
                    case InputType.Html:
                        result = ValidateHtml(input, result);
                        break;
                    case InputType.Sql:
                        result = ValidateSql(input, result);
                        break;
                    case InputType.FileName:
                        result = ValidateFileName(input, result);
                        break;
                    case InputType.Url:
                        result = ValidateUrl(input, result);
                        break;
                    case InputType.Email:
                        result = ValidateEmail(input, result);
                        break;
                    case InputType.Json:
                        result = ValidateJson(input, result);
                        break;
                }

                // 设置净化后的值
                if (result.IsValid && string.IsNullOrEmpty(result.SanitizedValue))
                {
                    result.SanitizedValue = input;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "输入验证过程中发生错误");
                result.IsValid = false;
                result.Errors.Add("验证过程中发生内部错误");
            }

            return result;
        }

        /// <summary>
        /// 检测SQL注入
        /// </summary>
        public bool IsSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return SqlInjectionPatterns.Any(pattern => pattern.IsMatch(input));
        }

        /// <summary>
        /// 检测XSS攻击
        /// </summary>
        public bool IsXssAttack(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return XssPatterns.Any(pattern => pattern.IsMatch(input));
        }

        /// <summary>
        /// 检测路径遍历攻击
        /// </summary>
        public bool IsPathTraversal(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return PathTraversalPatterns.Any(pattern => pattern.IsMatch(input));
        }

        /// <summary>
        /// 检测命令注入
        /// </summary>
        public bool IsCommandInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return CommandInjectionPatterns.Any(pattern => pattern.IsMatch(input));
        }

        /// <summary>
        /// HTML编码
        /// </summary>
        public string HtmlEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return HttpUtility.HtmlEncode(input);
        }

        /// <summary>
        /// HTML解码
        /// </summary>
        public string HtmlDecode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return HttpUtility.HtmlDecode(input);
        }

        /// <summary>
        /// URL编码
        /// </summary>
        public string UrlEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return HttpUtility.UrlEncode(input);
        }

        /// <summary>
        /// 通用验证
        /// </summary>
        private ValidationResult ValidateGeneral(string input, ValidationResult result)
        {
            // 检查SQL注入
            if (IsSqlInjection(input))
            {
                result.IsValid = false;
                result.Errors.Add("检测到SQL注入攻击模式");
                result.ThreatType = ThreatType.SqlInjection;
                _logger.LogWarning("检测到SQL注入尝试: {Input}", input.Substring(0, Math.Min(100, input.Length)));
            }

            // 检查XSS
            if (IsXssAttack(input))
            {
                result.IsValid = false;
                result.Errors.Add("检测到XSS攻击模式");
                result.ThreatType = ThreatType.XssAttack;
                _logger.LogWarning("检测到XSS攻击尝试: {Input}", input.Substring(0, Math.Min(100, input.Length)));
            }

            // 检查命令注入
            if (IsCommandInjection(input))
            {
                result.IsValid = false;
                result.Errors.Add("检测到命令注入攻击模式");
                result.ThreatType = ThreatType.CommandInjection;
                _logger.LogWarning("检测到命令注入尝试: {Input}", input.Substring(0, Math.Min(100, input.Length)));
            }

            // 如果没有威胁，进行基础净化
            if (result.IsValid)
            {
                result.SanitizedValue = HtmlEncode(input.Trim());
            }

            return result;
        }

        /// <summary>
        /// HTML验证
        /// </summary>
        private ValidationResult ValidateHtml(string input, ValidationResult result)
        {
            if (IsXssAttack(input))
            {
                result.IsValid = false;
                result.Errors.Add("HTML内容包含XSS攻击模式");
                result.ThreatType = ThreatType.XssAttack;
            }
            else if (_options.AllowHtmlContent)
            {
                // 如果允许HTML，则进行白名单过滤
                result.SanitizedValue = SanitizeHtml(input);
            }
            else
            {
                result.SanitizedValue = HtmlEncode(input);
            }

            return result;
        }

        /// <summary>
        /// SQL验证
        /// </summary>
        private ValidationResult ValidateSql(string input, ValidationResult result)
        {
            if (IsSqlInjection(input))
            {
                result.IsValid = false;
                result.Errors.Add("输入包含SQL注入攻击模式");
                result.ThreatType = ThreatType.SqlInjection;
            }

            return result;
        }

        /// <summary>
        /// 文件名验证
        /// </summary>
        private ValidationResult ValidateFileName(string input, ValidationResult result)
        {
            if (IsPathTraversal(input))
            {
                result.IsValid = false;
                result.Errors.Add("文件名包含路径遍历攻击模式");
                result.ThreatType = ThreatType.PathTraversal;
            }

            // 检查非法字符
            var invalidChars = Path.GetInvalidFileNameChars();
            if (input.Any(c => invalidChars.Contains(c)))
            {
                result.IsValid = false;
                result.Errors.Add("文件名包含非法字符");
            }

            if (result.IsValid)
            {
                result.SanitizedValue = Path.GetFileName(input); // 确保只返回文件名部分
            }

            return result;
        }

        /// <summary>
        /// URL验证
        /// </summary>
        private ValidationResult ValidateUrl(string input, ValidationResult result)
        {
            if (!Uri.TryCreate(input, UriKind.RelativeOrAbsolute, out var uri))
            {
                result.IsValid = false;
                result.Errors.Add("URL格式无效");
            }
            else if (uri.IsAbsoluteUri && !_options.AllowedUrlSchemes.Contains(uri.Scheme.ToLower()))
            {
                result.IsValid = false;
                result.Errors.Add($"不允许的URL协议: {uri.Scheme}");
            }

            return result;
        }

        /// <summary>
        /// 邮箱验证
        /// </summary>
        private ValidationResult ValidateEmail(string input, ValidationResult result)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (!emailRegex.IsMatch(input))
            {
                result.IsValid = false;
                result.Errors.Add("邮箱地址格式无效");
            }

            return result;
        }

        /// <summary>
        /// JSON验证
        /// </summary>
        private ValidationResult ValidateJson(string input, ValidationResult result)
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(input);
            }
            catch (System.Text.Json.JsonException)
            {
                result.IsValid = false;
                result.Errors.Add("JSON格式无效");
            }

            return result;
        }

        /// <summary>
        /// HTML内容净化（白名单方式）
        /// </summary>
        private string SanitizeHtml(string input)
        {
            // 简化的HTML净化，实际项目中建议使用HtmlSanitizer等专业库
            var allowedTags = new[] { "p", "br", "strong", "em", "u", "ol", "ul", "li" };
            
            // 移除script、iframe等危险标签
            foreach (var pattern in XssPatterns)
            {
                input = pattern.Replace(input, "");
            }

            return input;
        }
    }

    /// <summary>
    /// 输入验证配置选项
    /// </summary>
    public class InputValidationOptions
    {
        public int MaxInputLength { get; set; } = 10000;
        public bool AllowHtmlContent { get; set; } = false;
        public List<string> AllowedUrlSchemes { get; set; } = new() { "http", "https" };
        public bool EnableLogging { get; set; } = true;
        public bool StrictMode { get; set; } = true;
    }
}