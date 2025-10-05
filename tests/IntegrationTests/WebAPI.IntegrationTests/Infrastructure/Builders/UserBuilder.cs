using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.WebAPI.IntegrationTests.Infrastructure.Builders;

/// <summary>
/// 用户测试数据构建器
/// </summary>
/// <remarks>
/// 使用 Fluent Builder 模式简化测试用户创建
/// </remarks>
public class UserBuilder
{
    private string _userName = $"test_user_{Guid.NewGuid():N[..8]}";
    private string _password = "Test123!";
    private string _realName = "测试用户";
    private UserRole _role = UserRole.Doctor;
    private CommonStatus _status = CommonStatus.Enabled;
    private string? _phoneNumber;
    private string? _email;

    /// <summary>
    /// 设置用户名
    /// </summary>
    public UserBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }

    /// <summary>
    /// 设置密码
    /// </summary>
    public UserBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    /// <summary>
    /// 设置真实姓名
    /// </summary>
    public UserBuilder WithRealName(string realName)
    {
        _realName = realName;
        return this;
    }

    /// <summary>
    /// 设置角色
    /// </summary>
    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    /// <summary>
    /// 设置为系统管理员
    /// </summary>
    public UserBuilder AsAdmin()
    {
        _role = UserRole.Admin;
        _realName = "系统管理员";
        return this;
    }

    /// <summary>
    /// 设置为医生
    /// </summary>
    public UserBuilder AsDoctor()
    {
        _role = UserRole.Doctor;
        _realName = "测试医生";
        return this;
    }

    /// <summary>
    /// 设置为医生（药师角色已废弃，统一使用医生）
    ///&lt;/summary&gt;
    public UserBuilder AsPharmacist()
    {
        _role = UserRole.Doctor;
        _realName = "测试药师";
        return this;
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    public UserBuilder WithStatus(CommonStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// 设置为禁用状态
    /// </summary>
    public UserBuilder Disabled()
    {
        _status = CommonStatus.Disabled;
        return this;
    }

    /// <summary>
    /// 设置电话号码
    /// </summary>
    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    /// <summary>
    /// 设置邮箱
    /// </summary>
    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// 构建用户实体
    /// </summary>
    public User Build()
    {
        return new User
        {
            UserName = _userName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_password),
            RealName = _realName,
            Role = _role,
            Status = _status,
            PhoneNumber = _phoneNumber,
            Email = _email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 构建并返回密码（用于登录测试）
    /// </summary>
    public (User User, string Password) BuildWithPassword()
    {
        return (Build(), _password);
    }
}
