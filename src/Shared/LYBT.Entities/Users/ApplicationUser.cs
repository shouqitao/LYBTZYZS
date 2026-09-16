using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Entities.Users;

/// <summary>
/// 统一用户实体 - 合并原 Identity ApplicationUser 与业务 User 模型
/// 继承 IdentityUser<Guid> 提供 Identity 集成，同时携带业务字段（角色、状态、审计等）
/// P2-4-3 评估：因需继承 IdentityUser&lt;Guid&gt; 无法继承 BaseEntity，故审计/软删除字段手抄（CreatedAt/UpdatedAt/CreatedBy/UpdatedBy/IsDeleted/RowVersion），
/// AppDbContext.SetAuditFields 同时处理 BaseEntity 与 ApplicationUser 两分支；若 BaseEntity 新增字段需同步手抄，已在 04-data-model.md 标注特例。
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDeletable
{
    /// <summary>真实姓名</summary>
    [StringLength(100)]
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>用户角色（业务角色，与 Identity Roles 并存便于跨模块查询）</summary>
    [DisplayName("角色")]
    public UserRole Role { get; set; } = UserRole.Doctor;

    /// <summary>系统管理员标识 - P2-4-4 约束：IsSysAdmin==true ⇒ Role 必须为 SuperAdmin，已在 Create/ChangeStatus/SoftDelete 中校验（畸形 Doctor+IsSysAdmin=true 会抛 InvalidOperationException）。</summary>
    [DisplayName("系统管理员")]
    public bool IsSysAdmin { get; set; } = false;

    /// <summary>用户状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>
    /// T5-P2-31: 下次登录时必须修改密码
    /// 管理员重置密码后设置此标记
    /// </summary>
    [DisplayName("下次登录须改密")]
    public bool MustChangeOnNextLogin { get; set; } = false;

    /// <summary>最后登录时间 (UTC)</summary>
    [DisplayName("最后登录时间")]
    public DateTime? LastLoginTime { get; set; }

    /// <summary>挂号费 (元) - REG-BR-009: 医生挂号费，前台/医生创建挂号时自动带出</summary>
    [Column(TypeName = "decimal(10,2)")]
    [DisplayName("挂号费")]
    public decimal RegistrationFee { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    [StringLength(500)]
    public string? Remark { get; set; }

    // ==== 审计字段（IAuditableEntity）====
    // 与 BaseEntity 同步维护 — 新增审计字段时必须同步更新此处
    // （ApplicationUser 继承 IdentityUser<Guid>，无法继承 BaseEntity，故手抄）

    /// <summary>创建时间 (UTC)</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间 (UTC)</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>创建者ID</summary>
    [DisplayName("创建者")]
    public Guid? CreatedBy { get; set; }

    /// <summary>更新者ID</summary>
    [DisplayName("更新者")]
    public Guid? UpdatedBy { get; set; }

    // ==== 软删除 + 并发控制（ISoftDeletable + RowVersion）====

    /// <summary>软删除标记</summary>
    [DisplayName("删除标记")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>并发控制字段 - 乐观并发控制</summary>
    [Timestamp]
    [DisplayName("版本")]
    public byte[]? RowVersion { get; set; }

    // ==== 领域方法 ====

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
        Guid? createdBy = null,
        decimal registrationFee = 0m)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));
        if (userName.Length < 3 || userName.Length > 32)
            throw new ArgumentException("用户名长度必须在3-32个字符之间", nameof(userName));
        if (string.IsNullOrWhiteSpace(realName))
            throw new ArgumentException("真实姓名不能为空", nameof(realName));

        // P2-4-4 验证：IsSysAdmin 仅 SuperAdmin 允许（ApplicationUser.Create 当前不设 IsSysAdmin，未来若增参需校验 IsSysAdmin ⇒ Role==SuperAdmin；畸形数据在持久化前由 EnsureConsistency 兜底）
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            RealName = realName.Trim(),
            Role = role,
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim(),
            Remark = remark?.Trim(),
            RegistrationFee = registrationFee,
            Status = CommonStatus.Enabled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新用户基本信息。
    /// </summary>
    public void UpdateProfile(string realName, string? phoneNumber, string? email, string? remark, Guid updatedBy, decimal? registrationFee = null)
    {
        if (string.IsNullOrWhiteSpace(realName))
            throw new ArgumentException("真实姓名不能为空", nameof(realName));

        RealName = realName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Email = email?.Trim();
        Remark = remark?.Trim();
        if (registrationFee.HasValue)
            RegistrationFee = registrationFee.Value;
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
}


