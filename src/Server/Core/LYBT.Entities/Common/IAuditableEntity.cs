namespace LYBT.Entities.Common;

/// <summary>
/// 审计实体接口
/// 定义实体的创建和更新审计字段
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// 创建时间 (UTC)
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    Guid? CreatedBy { get; set; }

    /// <summary>
    /// 更新时间 (UTC)
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    Guid? UpdatedBy { get; set; }
}
