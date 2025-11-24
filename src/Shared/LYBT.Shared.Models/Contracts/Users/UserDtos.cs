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
        public string UserName { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public UserRole Role { get; set; } = UserRole.Doctor;

        /// <summary>电话号码</summary>
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [DisplayName("邮箱地址")]
        public string? Email { get; set; }

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>失败登录次数</summary>
        [DisplayName("失败登录次数")]
        public int FailedLoginCount { get; set; }

        /// <summary>账号启用状态 - UltraThink兼容性别名</summary>
        [DisplayName("账号启用状态")]
        public bool IsActive => Status == CommonStatus.Enabled;
    }

    #endregion

    #region 创建和更新DTO

    /// <summary>
    /// 用户输入DTO - 统一创建和更新
    /// Phase 3: 合并UserCreateDto和UserUpdateDto
    /// Issue #1262: 密码改为可选，Server端使用默认值
    /// </summary>
    public class UserInputDto
    {
        /// <summary>用户ID（更新时必填，创建时为null）</summary>
        [DisplayName("用户ID")]
        public Guid? Id { get; set; }

        /// <summary>用户名（创建时必填，更新时不可改）</summary>
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        /// <summary>
        /// 密码（创建时可选，更新时禁止）
        /// Issue #1262: 如果不提供密码，Server端将使用配置的默认密码
        /// </summary>
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("密码")]
        public string? Password { get; set; }

        /// <summary>
        /// 确认密码（创建时可选，更新时禁止）
        /// Issue #1262: 仅当提供密码时需要确认
        /// </summary>
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认密码")]
        public string? ConfirmPassword { get; set; }

        /// <summary>真实姓名（创建时必填，更新时可选）</summary>
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

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

        /// <summary>用户角色（创建时必填，更新时可选）</summary>
        [DisplayName("用户角色")]
        public UserRole? Role { get; set; } = UserRole.Doctor;

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;
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
    /// Issue #1008: 简化为MVP必需字段（移除WuBiCode/StartDate/EndDate）
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

        /// <summary>是否包含已禁用项</summary>
        [DisplayName("包含已禁用")]
        public bool IncludeInactive { get; set; } = false;
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
    /// 修改个人资料DTO (Issue #1887: 简化为MVP必需字段)
    /// UserId从路由参数获取，Email/Avatar/Bio暂不支持修改
    /// </summary>
    public class ChangeProfileDto
    {
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
    }

    /// <summary>
    /// 管理员重置密码请求DTO (Issue #1162)
    /// 修复：重置密码不需要传递新密码，使用配置文件中的默认密码
    /// </summary>
    public class ResetPasswordRequestDto
    {
        /// <summary>新密码（已废弃，为了向后兼容保留此属性，实际不使用）</summary>
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("新密码")]
        [Obsolete("重置密码不再支持传递新密码，将使用配置文件中的默认密码")]
        public string? NewPassword { get; set; } = null;

        /// <summary>是否强制用户下次登录时修改密码</summary>
        [DisplayName("强制修改密码")]
        public bool MustChangeOnNextLogin { get; set; } = true;
    }

    /// <summary>
    /// 管理员重置密码响应DTO (Issue #1162)
    /// </summary>
    public class ResetPasswordResponseDto
    {
        /// <summary>操作是否成功</summary>
        [DisplayName("成功")]
        public bool Success { get; set; }

        /// <summary>临时密码（自动生成时返回）</summary>
        [DisplayName("临时密码")]
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    #endregion
}
