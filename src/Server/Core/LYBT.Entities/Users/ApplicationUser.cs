using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Entities.Users;

/// <summary>
/// 统一用户实体 - 合并原 Identity ApplicationUser 与业务 User 模型
/// 继承 IdentityUser<Guid> 提供 Identity 集成，同时携带业务字段（角色、状态、审计等）
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDeletable
{
    /// <summary>真实姓名</summary>
    [StringLength(50)]
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>用户角色（业务角色，与 Identity Roles 并存便于跨模块查询）</summary>
    [DisplayName("角色")]
    public UserRole Role { get; set; } = UserRole.Doctor;

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
    public DateTime? LastLoginAt { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    [StringLength(500)]
    public string? Remark { get; set; }

    // ==== 审计字段（IAuditableEntity）====

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
}
