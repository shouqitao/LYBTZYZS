namespace LYBT.Entities.Common;

/// <summary>
/// 软删除接口
/// 实现此接口的实体将使用软删除而非物理删除
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// 软删除标记
    /// true: 已删除, false: 未删除
    /// </summary>
    bool IsDeleted { get; set; }
}
