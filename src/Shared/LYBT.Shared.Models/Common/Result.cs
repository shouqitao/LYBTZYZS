namespace LYBT.Shared.Models.Common;

/// <summary>
/// 统一返回值模式 - 封装成功/失败状态和错误信息
/// Phase 1 Task 1.5: 为Service层提供统一的返回值模式，替代直接抛出异常
/// </summary>
/// <typeparam name="T">返回数据类型</typeparam>
/// <remarks>
/// 设计原则：
/// - 成功场景：使用Success静态方法创建，返回数据存储在Data属性
/// - 失败场景：使用Failure静态方法创建，错误信息存储在ErrorMessage或Errors属性
/// - 避免直接new Result对象，统一使用静态工厂方法
///
/// 使用示例：
/// <code>
/// // 成功场景
/// var result = Result&lt;UserDto&gt;.Success(userDto);
/// if (result.IsSuccess)
/// {
///     var data = result.Data; // 获取返回数据
/// }
///
/// // 失败场景 - 单个错误
/// var result = Result&lt;UserDto&gt;.Failure("用户名已存在");
/// if (!result.IsSuccess)
/// {
///     var error = result.ErrorMessage; // 获取错误信息
/// }
///
/// // 失败场景 - 多个错误（FluentValidation）
/// var validationResult = await validator.ValidateAsync(input);
/// if (!validationResult.IsValid)
/// {
///     var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
///     return Result&lt;UserDto&gt;.Failure(errors);
/// }
/// </code>
/// </remarks>
public class Result<T>
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 返回数据（成功时有值）
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误信息（失败时有值，单个错误或多个错误的拼接）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误列表（失败时有值，用于返回多个验证错误）
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="data">返回的数据对象</param>
    /// <returns>成功的Result对象</returns>
    public static Result<T> Success(T data)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果（单个错误信息）
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>失败的Result对象</returns>
    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// 创建失败结果（多个错误信息）
    /// </summary>
    /// <param name="errors">错误信息列表（如FluentValidation验证错误）</param>
    /// <returns>失败的Result对象</returns>
    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Errors = errors,
            ErrorMessage = string.Join("; ", errors)
        };
    }
}
