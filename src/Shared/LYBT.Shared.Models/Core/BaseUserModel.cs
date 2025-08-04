using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 用户基础模型 - 前后端共享核心字段
    /// 包含所有通用的用户信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseUserModel {

        /// <summary>用户唯一标识</summary>
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名（统一命名）</summary>
        [DisplayName("用户名")]
        [System.ComponentModel.DataAnnotations.Schema.Column("UserName")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public UserRole Role { get; set; } = UserRole.DiagnosingDoctor;

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        [System.ComponentModel.DataAnnotations.Schema.Column("CreatedTime")]
        public DateTime CreateTime { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>邮箱</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>电话号码</summary>
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>部门/科室</summary>
        [DisplayName("部门")]
        [StringLength(100)]
        public string? Department { get; set; }

        /// <summary>职位</summary>
        [DisplayName("职位")]
        [StringLength(100)]
        public string? Position { get; set; }

        /// <summary>
        /// 是否有管理员权限（计算属性）
        /// </summary>
        [DisplayName("是否管理员")]
        public bool IsAdmin => Role == UserRole.Admin;

        /// <summary>
        /// 是否有医生权限（计算属性）
        /// </summary>
        [DisplayName("是否医生")]
        public bool IsDoctor => Role == UserRole.DiagnosingDoctor;

        /// <summary>
        /// 获取角色显示名称（计算属性）
        /// </summary>
        [DisplayName("角色名称")]
        public string RoleDisplayName => Role.GetDescription();

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>备注信息</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }
    }
}