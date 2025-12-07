using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace LYBT.WebAPI.Authorization;

/// <summary>
/// 经验方授权操作定义
/// optimize-api-permissions: 使用 OperationAuthorizationRequirement 实现资源级授权
/// </summary>
public static class FormulaOperations
{
    /// <summary>
    /// 读取经验方操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Read =
        new() { Name = nameof(Read) };

    /// <summary>
    /// 更新经验方操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Update =
        new() { Name = nameof(Update) };

    /// <summary>
    /// 删除经验方操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Delete =
        new() { Name = nameof(Delete) };
}
