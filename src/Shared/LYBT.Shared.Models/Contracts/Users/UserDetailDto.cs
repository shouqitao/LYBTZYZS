using System.ComponentModel;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users;

/// <summary>
/// 用户详情DTO - 扁平化设计，用于详情视图
/// </summary>
public class UserDetailDto
{
    /// <summary>用户ID</summary>
    [DisplayName("用户ID")]
    public Guid Id { get; set; }

    /// <summary>用户名</summary>
    [DisplayName("用户名")]
    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>真实姓名</summary>
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>用户角色</summary>
    [DisplayName("用户角色")]
    public UserRole Role { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>是否启用 - 根据Status计算得出</summary>
    [DisplayName("是否启用")]
    public bool IsEnabled => Status == CommonStatus.Enabled;

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

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    public string? Remark { get; set; }
}
