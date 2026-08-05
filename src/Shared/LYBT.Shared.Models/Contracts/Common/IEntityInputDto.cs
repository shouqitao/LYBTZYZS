namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 实体输入 DTO 基础接口 — 提供实体 ID（更新时必填，创建时为 null）。
/// 供泛型仓储基类在更新时提取实体 ID。
/// </summary>
public interface IEntityInputDto
{
    /// <summary>实体 ID（更新时必填，创建时为 null）</summary>
    Guid? Id { get; }
}
