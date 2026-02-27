using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Attributes;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Users
{

    /// <summary>
    /// 用户实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseUser和UserModel，包含医生功能
    /// 删除五笔码字段，保留拼音码用于快速搜索
    /// 继承BaseEntity实现审计字段自动化
    /// </summary>
    [Table("Users")]
    public class User : BaseEntity
    {

        // Id字段继承自BaseEntity

        /// <summary>用户名（统一命名）</summary>
        [Required]
        [StringLength(50)]
        [Column("UserName")]
        [DisplayName("用户名")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>拼音码（用于快速搜索）</summary>
        [StringLength(50)]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>电话号码</summary>
        [StringLength(20)]
        [DisplayName("电话号码")]
        [SensitiveData(SensitiveDataType.ContactInfo, MaskingMode = MaskingMode.Partial)]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [StringLength(100)]
        [DisplayName("邮箱")]
        [SensitiveData(SensitiveDataType.ContactInfo, MaskingMode = MaskingMode.Partial)]
        public string? Email { get; set; }

        /// <summary>用户角色</summary>
        [DisplayName("角色")]
        public UserRole Role { get; set; } = UserRole.Doctor;

        /// <summary>用户状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>密码哈希（敏感信息，仅后端使用）</summary>
        [Required]
        [StringLength(256)]
        [DisplayName("密码哈希")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>失败登录次数（安全状态，仅后端使用）</summary>
        [DisplayName("失败登录次数")]
        public int FailedLoginCount { get; set; } = 0;

        /// <summary>锁定结束时间（安全状态，仅后端使用）</summary>
        [DisplayName("锁定结束时间")]
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// T5-P2-31: 下次登录时必须修改密码
        /// 管理员重置密码后设置此标记
        /// </summary>
        [DisplayName("下次登录须改密")]
        public bool MustChangeOnNextLogin { get; set; } = false;

        // ==== 基础时间字段 ====
        // 审计字段（CreatedAt、UpdatedAt、CreatedBy、UpdatedBy）继承自BaseEntity

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        // ==== 扩展字段 ====

        /// <summary>备注</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }

        // RowVersion、IsDeleted等字段继承自BaseEntity
    }
}
