// -----------------------------------------------------------------------
// <copyright file="MappingServiceBase.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace LYBT.Desktop.Infrastructure.Mapping;

/// <summary>
/// 映射服务基类，提供集合映射的默认实现。
/// </summary>
/// <typeparam name="TDto">DTO类型。</typeparam>
/// <typeparam name="TItem">Item类型。</typeparam>
/// <remarks>
/// 派生类只需实现单对象映射方法（ToItem/ToDto），
/// 集合映射方法由基类提供默认实现。
/// </remarks>
public abstract class MappingServiceBase<TDto, TItem> : IMappingService<TDto, TItem>
    where TDto : class
    where TItem : class
{
    /// <inheritdoc />
    public abstract TItem ToItem(TDto dto);

    /// <inheritdoc />
    public abstract TDto ToDto(TItem item);

    /// <inheritdoc />
    public virtual IEnumerable<TItem> ToItems(IEnumerable<TDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        return dtos.Select(ToItem);
    }

    /// <inheritdoc />
    public virtual void ToItemsInto(IEnumerable<TDto> dtos, ObservableCollection<TItem> target)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        ArgumentNullException.ThrowIfNull(target);

        target.Clear();
        foreach (var dto in dtos)
        {
            target.Add(ToItem(dto));
        }
    }

    /// <inheritdoc />
    public virtual IEnumerable<TDto> ToDtos(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Select(ToDto);
    }
}

/// <summary>
/// 支持InputDto的映射服务基类。
/// </summary>
/// <typeparam name="TDto">DetailDTO类型。</typeparam>
/// <typeparam name="TInputDto">InputDTO类型。</typeparam>
/// <typeparam name="TItem">Item类型。</typeparam>
public abstract class MappingServiceBase<TDto, TInputDto, TItem>
    : MappingServiceBase<TDto, TItem>, IMappingService<TDto, TInputDto, TItem>
    where TDto : class
    where TInputDto : class
    where TItem : class
{
    /// <inheritdoc />
    public abstract TInputDto ToInputDto(TItem item);

    /// <inheritdoc />
    public virtual IEnumerable<TInputDto> ToInputDtos(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Select(ToInputDto);
    }
}
