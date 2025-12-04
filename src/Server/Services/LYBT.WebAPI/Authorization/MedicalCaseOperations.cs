using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace LYBT.WebAPI.Authorization;

/// <summary>
/// 医案授权操作定义
/// 使用 OperationAuthorizationRequirement 实现资源级授权
/// </summary>
public static class MedicalCaseOperations
{
    /// <summary>
    /// 创建医案操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Create =
        new() { Name = nameof(Create) };

    /// <summary>
    /// 读取医案操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Read =
        new() { Name = nameof(Read) };

    /// <summary>
    /// 编辑医案操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Edit =
        new() { Name = nameof(Edit) };

    /// <summary>
    /// 删除医案操作
    /// </summary>
    public static readonly OperationAuthorizationRequirement Delete =
        new() { Name = nameof(Delete) };
}
