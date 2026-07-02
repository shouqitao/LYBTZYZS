using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Primitives;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Domain;

/// <summary>
/// 用户聚合根。封装用户生命周期的业务规则和状态转换。
/// 继承IdentityUser以支持ASP.NET Core Identity。
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IAggregateRoot
{
    /// <summary>真实姓名</summary>
    [StringLength(50)]
    public string RealName { get; private set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    public string? PinYinCode { get; private set; }

    /// <summary>用户角色</summary>
    public UserRole Role { get; private set; } = UserRole.Doctor;

    /// <summary>系统管理员标识</summary>
    public bool IsSysAdmin { get; private set; }

    /// <summary>用户状态</summary>
    public CommonStatus Status { get; private set; } = CommonStatus.Enabled;

    /// <summary>下次登录须改密</summary>
    public bool MustChangeOnNextLogin { get; private set; }

    /// <summary>最后登录时间 (UTC)</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Remark { get; private set; }

    /// <summary>创建时间 (UTC)</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>更新时间 (UTC)</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>创建者ID</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>更新者ID</summary>
    public Guid? UpdatedBy { get; private set; }

    /// <summary>软删除标记</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>乐观并发控制</summary>
    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    private ApplicationUser() { }

    /// <summary>
    /// 创建新用户。
    /// </summary>
    public static ApplicationUser Create(
        string userName,
        string realName,
        UserRole role,
        string? phoneNumber = null,
        string? email = null,
        string? remark = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));
        if (userName.Length < 3 || userName.Length > 32)
            throw new ArgumentException("用户名长度必须在3-32个字符之间", nameof(userName));
        if (string.IsNullOrWhiteSpace(realName))
            throw new ArgumentException("真实姓名不能为空", nameof(realName));

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            RealName = realName.Trim(),
            Role = role,
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim(),
            Remark = remark?.Trim(),
            Status = CommonStatus.Enabled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新用户基本信息。
    /// </summary>
    public void UpdateProfile(string realName, string? phoneNumber, string? email, string? remark, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(realName))
            throw new ArgumentException("真实姓名不能为空", nameof(realName));

        RealName = realName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Email = email?.Trim();
        Remark = remark?.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改用户状态（启用/禁用）。sysadmin不可被禁用。
    /// </summary>
    public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
    {
        if (IsSysAdmin && newStatus == CommonStatus.Disabled)
            throw new InvalidOperationException("系统管理员不能被禁用");

        Status = newStatus;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改用户角色。sysadmin角色不可更改。
    /// </summary>
    public void ChangeRole(UserRole newRole, Guid updatedBy)
    {
        if (IsSysAdmin)
            throw new InvalidOperationException("系统管理员角色不可更改");

        Role = newRole;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录登录时间。
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记下次登录须改密。
    /// </summary>
    public void RequirePasswordChange()
    {
        MustChangeOnNextLogin = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 清除须改密标记。
    /// </summary>
    public void ClearPasswordChangeRequirement()
    {
        MustChangeOnNextLogin = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除用户。sysadmin不可被删除。
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        if (IsSysAdmin)
            throw new InvalidOperationException("系统管理员不能被删除");

        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复已软删除的用户。
    /// </summary>
    public void Restore(Guid restoredBy)
    {
        IsDeleted = false;
        UpdatedBy = restoredBy;
        UpdatedAt = DateTime.UtcNow;
    }
}


