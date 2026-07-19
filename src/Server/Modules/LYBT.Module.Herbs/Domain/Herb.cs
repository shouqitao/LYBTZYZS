using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.SharedKernel.Primitives;

namespace LYBT.Module.Herbs.Domain;

/// <summary>
/// 中药材聚合根。封装药材生命周期的业务规则和状态转换。
/// </summary>
public class Herb : Entity, IAggregateRoot
{
    /// <summary>药材名称</summary>
    [StringLength(100)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    public string? PinYinCode { get; private set; }

    /// <summary>分类（如：补血药、补气药）</summary>
    [StringLength(50)]
    public string? Category { get; private set; }

    /// <summary>性味（如：甘、温）</summary>
    [StringLength(100)]
    public string? Properties { get; private set; }

    /// <summary>产地</summary>
    [StringLength(100)]
    public string? Origin { get; private set; }

    /// <summary>规格</summary>
    [StringLength(100)]
    public string? Spec { get; private set; }

    /// <summary>单位（如：克、两、钱）</summary>
    [StringLength(20)]
    public string Unit { get; private set; } = "克";

    /// <summary>单价（元/单位）</summary>
    public decimal Price { get; private set; }

    /// <summary>成本价（元/单位）</summary>
    public decimal? CostPrice { get; private set; }

    /// <summary>功效说明</summary>
    [StringLength(500)]
    public string? Effect { get; private set; }

    /// <summary>用法用量</summary>
    [StringLength(500)]
    public string? Usage { get; private set; }

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Remark { get; private set; }

    /// <summary>药材状态</summary>
    public CommonStatus Status { get; private set; } = CommonStatus.Enabled;

    /// <summary>软删除标记</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>创建时间 (UTC)</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>更新时间 (UTC)</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>创建者ID</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>更新者ID</summary>
    public Guid? UpdatedBy { get; private set; }

    /// <summary>乐观并发控制</summary>
    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    private Herb() { }

    /// <summary>
    /// 创建新药材。
    /// </summary>
    public static Herb Create(
        string name,
        string unit,
        decimal price,
        string? pinYinCode = null,
        string? category = null,
        string? properties = null,
        string? origin = null,
        string? spec = null,
        decimal? costPrice = null,
        string? effect = null,
        string? usage = null,
        string? remark = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("药材名称不能为空", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("药材名称长度不能超过100个字符", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("单位不能为空", nameof(unit));
        if (price < 0)
            throw new ArgumentException("单价不能为负数", nameof(price));

        return new Herb
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Unit = unit.Trim(),
            Price = price,
            PinYinCode = pinYinCode?.Trim(),
            Category = category?.Trim(),
            Properties = properties?.Trim(),
            Origin = origin?.Trim(),
            Spec = spec?.Trim(),
            CostPrice = costPrice,
            Effect = effect?.Trim(),
            Usage = usage?.Trim(),
            Remark = remark?.Trim(),
            Status = CommonStatus.Enabled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新药材基本信息。
    /// </summary>
    public void UpdateProfile(
        string name,
        string unit,
        decimal price,
        string? pinYinCode,
        string? category,
        string? properties,
        string? origin,
        string? spec,
        decimal? costPrice,
        string? effect,
        string? usage,
        string? remark,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("药材名称不能为空", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("单位不能为空", nameof(unit));
        if (price < 0)
            throw new ArgumentException("单价不能为负数", nameof(price));

        Name = name.Trim();
        Unit = unit.Trim();
        Price = price;
        PinYinCode = pinYinCode?.Trim();
        Category = category?.Trim();
        Properties = properties?.Trim();
        Origin = origin?.Trim();
        Spec = spec?.Trim();
        CostPrice = costPrice;
        Effect = effect?.Trim();
        Usage = usage?.Trim();
        Remark = remark?.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新药材价格。
    /// </summary>
    public void UpdatePrice(decimal newPrice, decimal? newCostPrice, Guid updatedBy)
    {
        if (newPrice < 0)
            throw new ArgumentException("单价不能为负数", nameof(newPrice));

        Price = newPrice;
        CostPrice = newCostPrice;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改药材状态（启用/禁用）。
    /// </summary>
    public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
    {
        Status = newStatus;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除药材。
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复已软删除的药材。
    /// </summary>
    public void Restore(Guid restoredBy)
    {
        IsDeleted = false;
        UpdatedBy = restoredBy;
        UpdatedAt = DateTime.UtcNow;
    }
}


