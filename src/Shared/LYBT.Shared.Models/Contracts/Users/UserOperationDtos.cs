using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users
{

    /// <summary>
    /// 用户分页查询DTO - 继承完整查询基类 + 编码接口
    /// 用于用户管理的分页查询和筛选
    /// </summary>
    public class UserPagedQueryDto : ExtendedQueryDto, ICodeable
    {

        /// <summary>用户名关键词</summary>
        [DisplayName("用户名")]
        public string? Username { get; set; }

        /// <summary>真实姓名关键词</summary>
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        /// <summary>角色筛选</summary>
        [DisplayName("用户角色")]
        public string? Role { get; set; }

        /// <summary>邮箱关键词</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>电话关键词</summary>
        [DisplayName("电话")]
        public string? PhoneNumber { get; set; }

        /// <summary>部门关键词</summary>
        [DisplayName("部门")]
        public string? Department { get; set; }

        /// <summary>职位关键词</summary>
        [DisplayName("职位")]
        public string? Position { get; set; }

        /// <summary>最后登录日期范围-开始日期</summary>
        [DisplayName("登录开始日期")]
        public DateTime? LoginStartDate { get; set; }

        /// <summary>最后登录日期范围-结束日期</summary>
        [DisplayName("登录结束日期")]
        public DateTime? LoginEndDate { get; set; }

        /// <summary>按拼音码搜索</summary>
        [DisplayName("拼音码")]
        public new string? PinYinCode { get; set; }

        /// <summary>按五笔码搜索</summary>
        [DisplayName("五笔码")]
        public new string? WuBiCode { get; set; }
    }

    /// <summary>
    /// 用户查询DTO - 前后端共享API契约
    /// 用于用户信息的基础查询和筛选
    /// </summary>
    public class UserQueryDto
    {

        /// <summary>用户名关键词</summary>
        [DisplayName("用户名")]
        public string? Username { get; set; }

        /// <summary>真实姓名关键词</summary>
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        /// <summary>角色筛选</summary>
        [DisplayName("用户角色")]
        public string? Role { get; set; }

        /// <summary>状态筛选</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }

        /// <summary>是否在线</summary>
        [DisplayName("是否在线")]
        public bool? IsOnline { get; set; }

        /// <summary>创建日期范围-开始日期</summary>
        [DisplayName("创建开始日期")]
        public DateTime? CreateStartDate { get; set; }

        /// <summary>创建日期范围-结束日期</summary>
        [DisplayName("创建结束日期")]
        public DateTime? CreateEndDate { get; set; }

        /// <summary>关键词搜索（同时搜索用户名和真实姓名）</summary>
        [DisplayName("关键词")]
        public string? Keyword { get; set; }
    }

    /// <summary>
    /// 用户修改密码 DTO
    /// </summary>
    public class ChangePasswordDto
    {

        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 原密码
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 6)]
        [DisplayName("原密码")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 6)]
        [DisplayName("新密码")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户修改个人资料DTO - 前后端共享API契约
    /// 用于用户修改个人基本信息的请求模型
    /// </summary>
    public class ChangeProfileDto
    {

        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>电话号码</summary>
        [Phone(ErrorMessage = "电话号码格式不正确")]
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>头像URL</summary>
        [StringLength(500, ErrorMessage = "头像URL长度不能超过500个字符")]
        [DisplayName("头像")]
        public string? Avatar { get; set; }

        /// <summary>个人简介</summary>
        [StringLength(1000, ErrorMessage = "个人简介长度不能超过1000个字符")]
        [DisplayName("个人简介")]
        public string? Bio { get; set; }
    }

    /// <summary>
    /// 用户重置密码DTO - 前后端共享API契约
    /// 用于管理员重置用户密码的请求模型
    /// </summary>
    public class ResetPasswordDto
    {

        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>新密码</summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("新密码")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>确认密码</summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>重置原因</summary>
        [StringLength(500, ErrorMessage = "重置原因长度不能超过500个字符")]
        [DisplayName("重置原因")]
        public string? Reason { get; set; }

        /// <summary>是否强制用户下次登录时修改密码</summary>
        [DisplayName("强制修改密码")]
        public bool ForceChangePassword { get; set; } = true;
    }
}
