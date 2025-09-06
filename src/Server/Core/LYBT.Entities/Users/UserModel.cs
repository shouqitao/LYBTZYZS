using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Users {

    /// <summary>
    /// 用户实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseUser和UserModel，包含医生功能
    /// 删除五笔码字段，保留拼音码用于快速搜索
    /// </summary>
    [Table("Users")]
    public class User {

        /// <summary>用户唯一标识</summary>
        [Key]
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名（统一命名）</summary>
        [Required]
        [StringLength(50)]
        [Column("UserName")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

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
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [StringLength(100)]
        [DisplayName("邮箱")]
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

        // ==== 医生专属字段 ====

        /// <summary>专长（医生用户填写，普通用户为空）</summary>
        [DisplayName("专长")]
        [StringLength(200)]
        public string? Specialty { get; set; }

        /// <summary>挂号费（医生用户填写）</summary>
        [DisplayName("挂号费")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RegistrationFee { get; set; }

        /// <summary>执业证书号（医生用户填写）</summary>
        [DisplayName("执业证书号")]
        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        /// <summary>简介（医生用户填写）</summary>
        [DisplayName("简介")]
        [StringLength(1000)]
        public string? Introduction { get; set; }

        // ==== 基础时间字段 ====

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>最后更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        // ==== 扩展字段 ====

        /// <summary>备注</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }
    }
}
