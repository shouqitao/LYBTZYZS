using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users {

    /// <summary>
    /// 用户信息DTO - UltraThink v2.0简化版
    /// 与User实体对齐，删除时间字段和不存在字段
    /// </summary>
    public class UserDto : StatusDto {

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public string Role { get; set; } = "Doctor";

        /// <summary>电话号码</summary>
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [DisplayName("邮箱地址")]
        public string? Email { get; set; }

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>账号启用状态 - UltraThink兼容性别名</summary>
        [DisplayName("账号启用状态")]
        public bool IsActive => Status == CommonStatus.Enabled;

        /// <summary>用户名(兼容性别名)</summary>
        [DisplayName("用户名")]
        [JsonPropertyName("userDisplayName")]
        public string UserName => RealName ?? Username;
    }

    /// <summary>
    /// 用户详情DTO - 继承基础DTO
    /// 用于用户资料查看与编辑（不包含密码）
    /// </summary>
    public class UserDetailDto : BaseDto {

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(20, ErrorMessage = "真实姓名长度不能超过20个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色（单选）</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public string Role { get; set; } = "Doctor";

        /// <summary>账号启用状态（true=启用，false=禁用，必填）</summary>
        [Required(ErrorMessage = "账号启用状态不能为空")]
        [DisplayName("账号启用状态")]
        public bool IsActive { get; set; }

        /// <summary>联系电话</summary>
        [Phone(ErrorMessage = "联系电话格式不正确")]
        [DisplayName("联系电话")]
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// 用户变更DTO - UltraThink架构优化：统一创建和更新操作
    /// 消除95%的代码重复，密码字段可选（创建时必须，更新时可选）
    /// </summary>
    public class UserMutationDto : BaseDto {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码 - 创建时必须，更新时可选（null=不更新密码）</summary>
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("密码")]
        public string? Password { get; set; }

        /// <summary>确认密码 - 仅当提供密码时需要</summary>
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认密码")]
        public string? ConfirmPassword { get; set; }

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public string Role { get; set; } = "Doctor";

        /// <summary>电话号码</summary>
        [Phone(ErrorMessage = "电话号码格式不正确")]
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
        [DisplayName("邮箱地址")]
        public string? Email { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>操作类型标识 - 用于区分创建或更新操作</summary>
        [DisplayName("操作类型")]
        public bool IsCreateOperation { get; set; }
    }

    /// <summary>
    /// 用户统计DTO
    /// 用于用户统计信息的展示
    /// </summary>
    public class UserStatisticsDto {

        /// <summary>用户总数</summary>
        [DisplayName("用户总数")]
        public int TotalCount { get; set; }

        /// <summary>活跃用户数</summary>
        [DisplayName("活跃用户数")]
        public int ActiveCount { get; set; }

        /// <summary>非活跃用户数</summary>
        [DisplayName("非活跃用户数")]
        public int InactiveCount { get; set; }

        /// <summary>医生数量</summary>
        [DisplayName("医生数量")]
        public int DoctorCount { get; set; }

        /// <summary>收银员数量</summary>
        [DisplayName("收银员数量")]
        public int CashierCount { get; set; }

        /// <summary>理疗师数量</summary>
        [DisplayName("理疗师数量")]
        public int TherapistCount { get; set; }

        /// <summary>管理员数量</summary>
        [DisplayName("管理员数量")]
        public int AdminCount { get; set; }

        /// <summary>药师数量</summary>
        [DisplayName("药师数量")]
        public int PharmacistCount { get; set; }

        /// <summary>前台数量</summary>
        [DisplayName("前台数量")]
        public int ReceptionistCount { get; set; }
    }
}
