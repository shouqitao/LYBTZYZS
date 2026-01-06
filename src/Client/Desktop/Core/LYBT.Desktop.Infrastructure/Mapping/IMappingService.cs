// -----------------------------------------------------------------------
// <copyright file="IMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace LYBT.Desktop.Infrastructure.Mapping;

/// <summary>
/// 通用映射服务接口，提供DTO与Item之间的映射能力。
/// </summary>
/// <typeparam name="TDto">DTO类型（API传输对象）。</typeparam>
/// <typeparam name="TItem">Item类型（XAML绑定对象）。</typeparam>
/// <remarks>
/// 设计说明：
/// - DTO: 用于API数据传输，为纯POCO类。
/// - Item: 继承BindableBase，用于XAML双向绑定。
/// - 映射逻辑由Mapperly源生成器在编译时生成。
/// </remarks>
public interface IMappingService<TDto, TItem>
    where TDto : class
    where TItem : class
{
    /// <summary>
    /// 将DTO转换为Item。
    /// </summary>
    /// <param name="dto">源DTO对象。</param>
    /// <returns>转换后的Item对象。</returns>
    TItem ToItem(TDto dto);

    /// <summary>
    /// 将Item转换为DTO。
    /// </summary>
    /// <param name="item">源Item对象。</param>
    /// <returns>转换后的DTO对象。</returns>
    TDto ToDto(TItem item);

    /// <summary>
    /// 将DTO集合转换为Item集合。
    /// </summary>
    /// <param name="dtos">源DTO集合。</param>
    /// <returns>转换后的Item集合。</returns>
    IEnumerable<TItem> ToItems(IEnumerable<TDto> dtos);

    /// <summary>
    /// 将DTO集合转换并填充到ObservableCollection。
    /// </summary>
    /// <param name="dtos">源DTO集合。</param>
    /// <param name="target">目标ObservableCollection，会先清空再填充。</param>
    void ToItemsInto(IEnumerable<TDto> dtos, ObservableCollection<TItem> target);

    /// <summary>
    /// 将Item集合转换为DTO集合。
    /// </summary>
    /// <param name="items">源Item集合。</param>
    /// <returns>转换后的DTO集合。</returns>
    IEnumerable<TDto> ToDtos(IEnumerable<TItem> items);
}

/// <summary>
/// 支持InputDto的映射服务接口扩展。
/// </summary>
/// <typeparam name="TDto">DetailDTO类型（API返回的详情对象）。</typeparam>
/// <typeparam name="TInputDto">InputDTO类型（API创建/更新的输入对象）。</typeparam>
/// <typeparam name="TItem">Item类型（XAML绑定对象）。</typeparam>
/// <remarks>
/// 三种DTO类型说明：
/// - DetailDto: API返回的完整详情，包含审计字段等。
/// - InputDto: API创建/更新时的输入，不含只读字段。
/// - Item: 客户端绑定模型，包含UI状态字段。
/// </remarks>
public interface IMappingService<TDto, TInputDto, TItem> : IMappingService<TDto, TItem>
    where TDto : class
    where TInputDto : class
    where TItem : class
{
    /// <summary>
    /// 将Item转换为InputDto（用于创建/更新API调用）。
    /// </summary>
    /// <param name="item">源Item对象。</param>
    /// <returns>转换后的InputDto对象。</returns>
    TInputDto ToInputDto(TItem item);

    /// <summary>
    /// 将Item集合转换为InputDto集合。
    /// </summary>
    /// <param name="items">源Item集合。</param>
    /// <returns>转换后的InputDto集合。</returns>
    IEnumerable<TInputDto> ToInputDtos(IEnumerable<TItem> items);
}
