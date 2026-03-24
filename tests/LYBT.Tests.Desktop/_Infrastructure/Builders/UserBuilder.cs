using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

/// <summary>
/// 用户数据构建器 - 简化版
/// 使用 Fluent API 模式创建测试用的用户数据
/// </summary>
public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _userName = "testuser";
    private string _realName = "测试用户";
    private string? _email;
    private string? _phoneNumber;
    private CommonStatus _status = CommonStatus.Enabled;
    private UserRole _role = UserRole.Doctor;
    private DateTime _createdAt = DateTime.UtcNow.AddDays(-30);
    private DateTime? _lastLoginTime;
    private string? _remark;

    public static UserBuilder Create() => new();

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }

    public UserBuilder WithRealName(string realName)
    {
        _realName = realName;
        return this;
    }

    public UserBuilder WithEmail(string? email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPhoneNumber(string? phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public UserBuilder WithStatus(CommonStatus status)
    {
        _status = status;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder WithLastLoginTime(DateTime lastLoginTime)
    {
        _lastLoginTime = lastLoginTime;
        return this;
    }

    public UserBuilder WithRemark(string? remark)
    {
        _remark = remark;
        return this;
    }

    /// <summary>
    /// 构建 UserDetailDto
    /// </summary>
    public UserDetailDto Build() => new()
    {
        Id = _id,
        UserName = _userName,
        RealName = _realName,
        Email = _email,
        PhoneNumber = _phoneNumber,
        Status = _status,
        Role = _role,
        CreatedAt = _createdAt,
        LastLoginTime = _lastLoginTime,
        Remark = _remark
    };

    /// <summary>
    /// 构建 LoginRequest
    /// </summary>
    public LoginRequest BuildLoginRequest(string password = "Test123!") => new()
    {
        UserName = _userName,
        Password = password
    };

    /// <summary>
    /// 预置：管理员用户
    /// </summary>
    public static UserBuilder Admin() => Create()
        .WithUserName("admin")
        .WithRealName("系统管理员")
        .WithRole(UserRole.Admin);

    /// <summary>
    /// 预置：医生用户
    /// </summary>
    public static UserBuilder Doctor() => Create()
        .WithUserName("doctor")
        .WithRealName("王医生")
        .WithRole(UserRole.Doctor);

    /// <summary>
    /// 预置：已禁用用户
    /// </summary>
    public static UserBuilder Disabled() => Create()
        .WithUserName("disabled")
        .WithRealName("已禁用用户")
        .WithStatus(CommonStatus.Disabled);
}
