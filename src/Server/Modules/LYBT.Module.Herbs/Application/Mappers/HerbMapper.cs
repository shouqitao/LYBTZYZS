using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Entities.Herbs;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Herbs.Application.Mappers;

/// <summary>
/// 药材数据映射器。Mapperly 编译时生成（A-18 P1-4 由手写静态类改造）。
/// 纯属性复制方法（ToListDto/ToDetailDto）由 Mapperly 生成；工厂方法（ToEntity）保留手写（行为等价）。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, AutoUserMappings = false)]
public static partial class HerbDtoMapper
{
    /// <summary>
    /// HerbInputDto 转换为 Herb 实体（创建）。
    /// 保留手写：走领域工厂 Herb.Create（校验 + Trim），Mapperly 无法表达。
    /// 不带 [UserMapping] 标记：AutoUserMappings=false 下不被 Mapperly 发现（带额外参数签名不受支持）。
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
    /// Herb 实体转换为 HerbListDto（列表查询）。Mapperly 生成（属性全同名）。
    /// </summary>
    public static partial HerbListDto ToListDto(Herb entity);

    /// <summary>
    /// Herb 实体转换为 HerbDetailDto（详情查询）。Mapperly 生成（属性全同名）。
    /// </summary>
    public static partial HerbDetailDto ToDetailDto(Herb entity);
}
