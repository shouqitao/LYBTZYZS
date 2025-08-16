using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户信息DTO - 继承完整基础DTO + 编码接口
    /// 用于用户信息的展示和传输（不包含敏感信息）
    /// </summary>
    public class UserDto : FullBaseDto, ICodeable
    {
        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public string Role { get; set; } = "User";

        /// <summary>电话号码</summary>
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>头像URL</summary>
        [DisplayName("头像")]
        public string? Avatar { get; set; }

        /// <summary>是否在线</summary>
        [DisplayName("是否在线")]
        public bool IsOnline { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>最后登录IP</summary>
        [DisplayName("最后登录IP")]
        public string? LastLoginIp { get; set; }

        /// <summary>账号启用状态 - UltraThink兼容性别名</summary>
        [DisplayName("账号启用状态")]
        public bool IsActive => Status == CommonStatus.Enabled;
    }

    /// <summary>
    /// 用户详情DTO - 继承基础DTO
    /// 用于用户资料查看与编辑（不包含密码）
    /// </summary>
    public class UserDetailDto : BaseDto
    {
        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(20, ErrorMessage = "真实姓名长度不能超过20个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色（单选）</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public string Role { get; set; } = "User";

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
    /// 用户创建DTO - 继承创建基类
    /// 用于创建新用户账户的请求模型
    /// </summary>
    public class UserCreateDto : CreateDtoBase
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
    /// 用户更新DTO - 继承更新基类
    /// 用于更新用户信息的请求模型
    /// </summary>
    public class UserUpdateDto : UpdateDtoBase
    {
        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public string Role { get; set; } = "User";

        /// <summary>电话号码</summary>
        [Phone(ErrorMessage = "电话号码格式不正确")]
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// 用户统计DTO
    /// 用于用户统计信息的展示
    /// </summary>
    public class UserStatisticsDto
    {
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

        /// <summary>护士数量</summary>
        [DisplayName("护士数量")]
        public int NurseCount { get; set; }

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