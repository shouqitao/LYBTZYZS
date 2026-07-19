using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.SharedKernel.Primitives;

namespace LYBT.Module.Formulas.Domain;

/// <summary>
/// 验方聚合根。封装验方生命周期的业务规则和状态转换。
/// </summary>
public class Formula : Entity, IAggregateRoot
{
    /// <summary>验方名称</summary>
    [StringLength(200)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>功用</summary>
    [StringLength(500)]
    public string? Effect { get; private set; }

    /// <summary>主治</summary>
    [StringLength(1000)]
    public string? Indication { get; private set; }

    /// <summary>用法</summary>
    [StringLength(500)]
    public string? Usage { get; private set; }

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Remark { get; private set; }

    /// <summary>性味归经</summary>
    [StringLength(300)]
    public string? Property { get; private set; }

    /// <summary>验方状态</summary>
    public CommonStatus Status { get; private set; } = CommonStatus.Enabled;

    /// <summary>是否共享</summary>
    public bool IsShared { get; private set; }

    /// <summary>验证状态</summary>
    public FormulaValidationStatus ValidationStatus { get; private set; } = FormulaValidationStatus.Draft;

    /// <summary>方剂分类</summary>
    [StringLength(50)]
    public string? Category { get; private set; }

    /// <summary>方剂类型（经典方/经验方）</summary>
    public FormulaType FormulaType { get; private set; } = FormulaType.Experience;

    /// <summary>创建用户ID</summary>
    public Guid? UserId { get; private set; }

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

    /// <summary>药材组成</summary>
    public ICollection<FormulaHerbItem> Herbs { get; private set; } = new List<FormulaHerbItem>();

    /// <summary>药材数量</summary>
    public int HerbCount => Herbs.Count;

    private Formula() { }

    /// <summary>
    /// 创建新验方。
    /// </summary>
    public static Formula Create(
        string name,
        string? effect = null,
        string? indication = null,
        string? usage = null,
        string? remark = null,
        string? property = null,
        string? category = null,
        FormulaType formulaType = FormulaType.Experience,
        bool isShared = false,
        Guid? userId = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("验方名称不能为空", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("验方名称长度不能超过200个字符", nameof(name));

        return new Formula
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Effect = effect?.Trim(),
            Indication = indication?.Trim(),
            Usage = usage?.Trim(),
            Remark = remark?.Trim(),
            Property = property?.Trim(),
            Category = category?.Trim(),
            FormulaType = formulaType,
            IsShared = isShared,
            UserId = userId,
            Status = CommonStatus.Enabled,
            ValidationStatus = FormulaValidationStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新验方基本信息。
    /// </summary>
    public void UpdateProfile(
        string name,
        string? effect,
        string? indication,
        string? usage,
        string? remark,
        string? property,
        string? category,
        bool isShared,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("验方名称不能为空", nameof(name));

        Name = name.Trim();
        Effect = effect?.Trim();
        Indication = indication?.Trim();
        Usage = usage?.Trim();
        Remark = remark?.Trim();
        Property = property?.Trim();
        Category = category?.Trim();
        IsShared = isShared;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加药材到验方。
    /// </summary>
    public void AddHerb(FormulaHerbItem herb)
    {
        if (herb == null)
            throw new ArgumentNullException(nameof(herb));

        Herbs.Add(herb);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 从验方移除药材。
    /// </summary>
    public void RemoveHerb(Guid herbItemId)
    {
        var herb = Herbs.FirstOrDefault(h => h.Id == herbItemId);
        if (herb != null)
        {
            Herbs.Remove(herb);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 验证验方（标记所有药材已验证）。
    /// </summary>
    public void Validate()
    {
        ValidationStatus = FormulaValidationStatus.Validated;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记为共享。
    /// </summary>
    public void MarkShared(bool shared, Guid updatedBy)
    {
        IsShared = shared;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改验方状态（启用/禁用）。
    /// </summary>
    public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
    {
        Status = newStatus;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除验方。
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复已软删除的验方。
    /// </summary>
    public void Restore(Guid restoredBy)
    {
        IsDeleted = false;
        UpdatedBy = restoredBy;
        UpdatedAt = DateTime.UtcNow;
    }
}


