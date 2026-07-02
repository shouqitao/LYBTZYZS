using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Module.Herbs.Domain;

namespace LYBT.Module.Herbs.Application.Mappers;

/// <summary>
/// 药材数据映射器。静态类，用于 Domain 实体与 DTO 之间的转换。
/// </summary>
public static class HerbDtoMapper
{
    /// <summary>
    /// HerbInputDto 转换为 Herb 实体（创建）。
    /// </summary>
    public static Herb ToEntity(HerbInputDto dto, Guid? createdBy = null) => Herb.Create(
        dto.Name,
        dto.Unit,
        dto.Price,
        dto.PinYinCode,
        dto.Category,
        dto.Properties,
        dto.Origin,
        dto.Spec,
        dto.CostPrice,
        dto.Effect,
        dto.Usage,
        dto.Remark,
        createdBy);

    /// <summary>
    /// Herb 实体转换为 HerbListDto（列表查询）。
    /// </summary>
    public static HerbListDto ToListDto(Herb entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        PinYinCode = entity.PinYinCode,
        Category = entity.Category,
        Origin = entity.Origin,
        Spec = entity.Spec,
        Unit = entity.Unit,
        Price = entity.Price,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };

    /// <summary>
    /// Herb 实体转换为 HerbDetailDto（详情查询）。
    /// </summary>
    public static HerbDetailDto ToDetailDto(Herb entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        PinYinCode = entity.PinYinCode,
        Category = entity.Category,
        Properties = entity.Properties,
        Origin = entity.Origin,
        Spec = entity.Spec,
        Unit = entity.Unit,
        Price = entity.Price,
        CostPrice = entity.CostPrice,
        Effect = entity.Effect,
        Usage = entity.Usage,
        Remark = entity.Remark,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedBy = entity.CreatedBy
    };
}


