using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Users {

    /// <summary>
    /// 简化用户实体类，避免字段映射问题
    /// </summary>
    [Table("Users")]
    public class SimpleUserModel {

        /// <summary>用户唯一标识</summary>
        [Key]
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("用户名")]
        [Column("UserName")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>密码哈希</summary>
        [Required]
        [DisplayName("密码哈希")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public UserRole Role { get; set; } = UserRole.DiagnosingDoctor;

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Column("CreatedTime")]
        public DateTime CreateTime { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>邮箱</summary>
        [StringLength(100)]
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>电话号码</summary>
        [StringLength(20)]
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>拼音码（可选）</summary>
        [StringLength(100)]
        [DisplayName("拼音码")]
        public string? PinyinCode { get; set; }

        /// <summary>五笔码（可选）</summary>
        [StringLength(100)]
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>失败登录次数</summary>
        [DisplayName("失败登录次数")]
        public int FailedLoginCount { get; set; } = 0;

        /// <summary>锁定结束时间</summary>
        [DisplayName("锁定结束时间")]
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// 是否有管理员权限（计算属性）
        /// </summary>
        [NotMapped]
        [DisplayName("是否管理员")]
        public bool IsAdmin => Role == UserRole.Admin;

        /// <summary>
        /// 是否有医生权限（计算属性）
        /// </summary>
        [NotMapped]
        [DisplayName("是否医生")]
        public bool IsDoctor => Role == UserRole.DiagnosingDoctor;
    }
}