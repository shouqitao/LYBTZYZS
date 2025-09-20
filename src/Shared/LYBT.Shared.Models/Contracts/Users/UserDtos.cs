using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users
{
    #region 基础DTO定义

    /// <summary>
    /// 用户信息DTO - UltraThink v2.0简化版
    /// 与User实体对齐，删除时间字段和不存在字段
    /// </summary>
    public class UserDto : StatusDto
    {

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

        /// <summary>用户显示名(兼容性属性，使用Username字段)</summary>
        [DisplayName("用户显示名")]
        [JsonPropertyName("userDisplayName")]
        public string UserDisplayName => RealName ?? Username;
    }

    #endregion

    #region 输入基础DTO

    /// <summary>
    /// 用户输入基础DTO - 提取创建和更新的共同字段
    /// </summary>
    public abstract class UserInputBaseDto
    {
        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>手机号码</summary>
        [Phone(ErrorMessage = "电话号码格式不正确")]
        [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
        [DisplayName("邮箱地址")]
        public string? Email { get; set; }

        /// <summary>用户角色</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public UserRole Role { get; set; } = UserRole.Doctor;

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    }

    #endregion

    #region 创建和更新DTO

    /// <summary>
    /// 用户创建DTO - 继承输入基础DTO
    /// </summary>
    public class UserCreateDto : UserInputBaseDto
    {
        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>确认密码</summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户更新DTO - 继承输入基础DTO并实现ID接口
    /// </summary>
    public class UserUpdateDto : UserInputBaseDto, IIdentifiable<Guid>
    {
        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>真实姓名（可选更新）</summary>
        [DisplayName("真实姓名")]
        public new string? RealName { get; set; }

        /// <summary>用户角色（可选更新）</summary>
        [DisplayName("用户角色")]
        public new UserRole? Role { get; set; }
    }

    /// <summary>
    /// 用户变更DTO - UltraThink架构优化：统一创建和更新操作（已废弃，使用UserCreateDto或UserUpdateDto）
    /// </summary>
    [Obsolete("请使用UserCreateDto或UserUpdateDto替代")]
    public class UserMutationDto : BaseDto
    {

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

    #endregion

    #region 查询DTO

    /// <summary>
    /// 用户查询DTO - 基础查询条件
    /// </summary>
    public class UserQueryDto : PagedQueryBaseDto
    {
        /// <summary>用户名关键词</summary>
        [DisplayName("用户名")]
        public string? Username { get; set; }

        /// <summary>真实姓名关键词</summary>
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        /// <summary>角色筛选</summary>
        [DisplayName("用户角色")]
        public UserRole? Role { get; set; }

        /// <summary>状态筛选</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }

        /// <summary>关键词搜索（同时搜索用户名和真实姓名）</summary>
        [DisplayName("关键词")]
        public new string? Keyword { get; set; }
    }

    /// <summary>
    /// 用户搜索DTO - 高级搜索条件
    /// </summary>
    public class UserSearchDto : UserQueryDto
    {
        /// <summary>邮箱关键词</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>电话关键词</summary>
        [DisplayName("电话")]
        public string? PhoneNumber { get; set; }

        /// <summary>按拼音码搜索</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>按五笔码搜索</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>创建日期范围-开始日期</summary>
        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        /// <summary>创建日期范围-结束日期</summary>
        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        /// <summary>是否包含已禁用项</summary>
        [DisplayName("包含已禁用")]
        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// 用户分页查询DTO - 兼容旧代码（已废弃，使用UserSearchDto替代）
    /// </summary>
    [Obsolete("请使用UserSearchDto替代")]
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

        /// <summary>按拼音码搜索</summary>
        [DisplayName("拼音码")]
        public new string? PinYinCode { get; set; }

        /// <summary>按五笔码搜索</summary>
        [DisplayName("五笔码")]
        public new string? WuBiCode { get; set; }
    }

    #endregion

    #region 操作DTO

    /// <summary>
    /// 修改密码DTO
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>原密码</summary>
        [Required(ErrorMessage = "原密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("原密码")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>新密码</summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("新密码")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>确认新密码</summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认新密码")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 重置密码DTO
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

    /// <summary>
    /// 修改个人资料DTO
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

    #endregion
}
