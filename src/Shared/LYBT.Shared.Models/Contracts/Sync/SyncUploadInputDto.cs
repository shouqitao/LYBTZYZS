using System.Text.Json;

namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步上传请求 DTO
/// </summary>
public class SyncUploadInputDto
{
    /// <summary>
    /// 实体类型 (Herb/Patient/Formula)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体数据列表（JSON 格式）
    /// </summary>
    public List<JsonElement> Entities { get; set; } = new();

    /// <summary>
    /// 是否覆盖服务器端冲突数据
    /// </summary>
    public bool OverwriteConflicts { get; set; } = false;
}
