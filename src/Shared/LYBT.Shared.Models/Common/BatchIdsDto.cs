namespace LYBT.Shared.Models.Common;

/// <summary>
/// 批量操作ID列表DTO
/// 用于批量启用、禁用等操作的通用数据传输对象
/// </summary>
public class BatchIdsDto
{
    /// <summary>
    /// ID列表
    /// </summary>
    public List<Guid> Ids { get; set; } = [];
}