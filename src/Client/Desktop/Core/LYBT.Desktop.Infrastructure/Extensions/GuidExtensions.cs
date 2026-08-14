using System;

namespace LYBT.Desktop.Infrastructure.Extensions;

/// <summary>
/// Guid 扩展方法
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// 空 Guid 转为 null（创建语义：null=新建，有值=更新）。
    /// mapper-chain-audit M3——统一 3+ 处 `Id == Guid.Empty ? null : Id` 模式。
    /// </summary>
    public static Guid? OrNullIfEmpty(this Guid id) => id == Guid.Empty ? null : id;

    /// <summary>
    /// 空 Guid（含 null）转为 null。
    /// </summary>
    public static Guid? OrNullIfEmpty(this Guid? id) => id == null || id == Guid.Empty ? null : id;
}
