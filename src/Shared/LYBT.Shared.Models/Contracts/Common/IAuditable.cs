namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 审计接口 - 提供创建、更新时间追踪和创建者信息
/// </summary>
public interface IAuditable
{
    /// <summary>创建时间</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>创建者ID</summary>
    Guid? CreatedBy { get; set; }
}
