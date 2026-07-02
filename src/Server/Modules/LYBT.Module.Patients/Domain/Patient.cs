using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Primitives;

namespace LYBT.Module.Patients.Domain;

/// <summary>
/// 患者聚合根。封装患者生命周期的业务规则和状态转换。
/// </summary>
public class Patient : Entity, IAggregateRoot
{
    /// <summary>患者姓名</summary>
    [StringLength(100)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    public string? PinYinCode { get; private set; }

    /// <summary>性别</summary>
    public Gender Gender { get; private set; } = Gender.Unknown;

    /// <summary>出生日期</summary>
    public DateTime? BirthDate { get; private set; }

    /// <summary>证件号码</summary>
    [StringLength(50)]
    public string? IdNumber { get; private set; }

    /// <summary>手机号码</summary>
    [StringLength(20)]
    public string? PhoneNumber { get; private set; }

    /// <summary>患者状态</summary>
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

    /// <summary>年龄（计算属性）</summary>
    [NotMapped]
    public int? Age
    {
        get
        {
            if (!BirthDate.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }

    private Patient() { }

    /// <summary>
    /// 创建新患者。
    /// </summary>
    public static Patient Create(
        string name,
        Gender gender,
        DateTime? birthDate = null,
        string? phoneNumber = null,
        string? idNumber = null,
        string? pinYinCode = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("患者姓名不能为空", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("患者姓名长度不能超过100个字符", nameof(name));

        return new Patient
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Gender = gender,
            BirthDate = birthDate,
            PhoneNumber = phoneNumber?.Trim(),
            IdNumber = idNumber?.Trim(),
            PinYinCode = pinYinCode?.Trim(),
            Status = CommonStatus.Enabled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新患者基本信息。
    /// </summary>
    public void UpdateProfile(
        string name,
        Gender gender,
        DateTime? birthDate,
        string? phoneNumber,
        string? idNumber,
        string? pinYinCode,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("患者姓名不能为空", nameof(name));

        Name = name.Trim();
        Gender = gender;
        BirthDate = birthDate;
        PhoneNumber = phoneNumber?.Trim();
        IdNumber = idNumber?.Trim();
        PinYinCode = pinYinCode?.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改患者状态（启用/禁用）。
    /// </summary>
    public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
    {
        Status = newStatus;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除患者。
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复已软删除的患者。
    /// </summary>
    public void Restore(Guid restoredBy)
    {
        IsDeleted = false;
        UpdatedBy = restoredBy;
        UpdatedAt = DateTime.UtcNow;
    }
}


