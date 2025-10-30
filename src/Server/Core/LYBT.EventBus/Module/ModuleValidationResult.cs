namespace LYBT.EventBus.Module;

/// <summary>
/// 模块验证结果
/// </summary>
public class ModuleValidationResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// 警告信息
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="isValid">是否验证成功</param>
    /// <param name="errors">错误信息</param>
    /// <param name="warnings">警告信息</param>
    public ModuleValidationResult(bool isValid, IEnumerable<string>? errors = null, IEnumerable<string>? warnings = null)
    {
        IsValid = isValid;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        Warnings = warnings?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    /// <param name="warnings">警告信息</param>
    /// <returns>验证结果</returns>
    public static ModuleValidationResult Success(IEnumerable<string>? warnings = null)
    {
        return new ModuleValidationResult(true, null, warnings);
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="errors">错误信息</param>
    /// <param name="warnings">警告信息</param>
    /// <returns>验证结果</returns>
    public static ModuleValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null)
    {
        return new ModuleValidationResult(false, errors, warnings);
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="error">错误信息</param>
    /// <param name="warnings">警告信息</param>
    /// <returns>验证结果</returns>
    public static ModuleValidationResult Failure(string error, IEnumerable<string>? warnings = null)
    {
        return new ModuleValidationResult(false, new[] { error }, warnings);
    }

    /// <summary>
    /// 合并多个验证结果
    /// </summary>
    /// <param name="results">验证结果集合</param>
    /// <returns>合并后的验证结果</returns>
    public static ModuleValidationResult Combine(params ModuleValidationResult[] results)
    {
        if (results == null || results.Length == 0)
            return Success();

        var allErrors = new List<string>();
        var allWarnings = new List<string>();
        var isValid = true;

        foreach (var result in results)
        {
            if (!result.IsValid)
                isValid = false;

            allErrors.AddRange(result.Errors);
            allWarnings.AddRange(result.Warnings);
        }

        return new ModuleValidationResult(isValid, allErrors, allWarnings);
    }

    /// <summary>
    /// 添加错误信息
    /// </summary>
    /// <param name="error">错误信息</param>
    /// <returns>新的验证结果</returns>
    public ModuleValidationResult AddError(string error)
    {
        var newErrors = new List<string>(Errors) { error };
        return new ModuleValidationResult(false, newErrors, Warnings);
    }

    /// <summary>
    /// 添加警告信息
    /// </summary>
    /// <param name="warning">警告信息</param>
    /// <returns>新的验证结果</returns>
    public ModuleValidationResult AddWarning(string warning)
    {
        var newWarnings = new List<string>(Warnings) { warning };
        return new ModuleValidationResult(IsValid, Errors, newWarnings);
    }

    /// <summary>
    /// 获取所有问题的摘要
    /// </summary>
    /// <returns>问题摘要</returns>
    public string GetSummary()
    {
        if (IsValid && !Warnings.Any())
            return "验证通过，无问题";

        var summary = new List<string>();

        if (Errors.Any())
        {
            summary.Add($"错误 ({Errors.Count} 个):");
            summary.AddRange(Errors.Select(e => $"  - {e}"));
        }

        if (Warnings.Any())
        {
            summary.Add($"警告 ({Warnings.Count} 个):");
            summary.AddRange(Warnings.Select(w => $"  - {w}"));
        }

        return string.Join(Environment.NewLine, summary);
    }

    /// <summary>
    /// 检查是否有问题
    /// </summary>
    /// <returns>是否有问题</returns>
    public bool HasIssues()
    {
        return Errors.Any() || Warnings.Any();
    }

    /// <summary>
    /// 检查是否有错误
    /// </summary>
    /// <returns>是否有错误</returns>
    public bool HasErrors()
    {
        return Errors.Any();
    }

    /// <summary>
    /// 检查是否有警告
    /// </summary>
    /// <returns>是否有警告</returns>
    public bool HasWarnings()
    {
        return Warnings.Any();
    }

    /// <summary>
    /// 抛出验证异常（如果验证失败）
    /// </summary>
    /// <exception cref="ModuleValidationException">验证失败异常</exception>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new ModuleValidationException("模块验证失败", this);
        }
    }
}

/// <summary>
/// 模块验证异常
/// </summary>
public class ModuleValidationException : Exception
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public ModuleValidationResult ValidationResult { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="validationResult">验证结果</param>
    public ModuleValidationException(string message, ModuleValidationResult validationResult)
        : base(message)
    {
        ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="validationResult">验证结果</param>
    /// <param name="innerException">内部异常</param>
    public ModuleValidationException(string message, ModuleValidationResult validationResult, Exception innerException)
        : base(message, innerException)
    {
        ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    }

    /// <summary>
    /// 获取详细的异常消息
    /// </summary>
    /// <returns>详细消息</returns>
    public string GetDetailedMessage()
    {
        return $"{Message}{Environment.NewLine}{ValidationResult.GetSummary()}";
    }
}
