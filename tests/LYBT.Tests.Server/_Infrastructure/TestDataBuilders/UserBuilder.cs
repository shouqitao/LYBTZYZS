using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds UserInputDto payloads for API calls.
/// </summary>
public sealed class UserBuilder
{
    private string _userName = $"user_{Guid.NewGuid():N}"[..12];
    private string _realName = "测试用户";
    private string? _email;
    private string? _phoneNumber;
    private UserRole _role = UserRole.Doctor;
    private string _password = "TestUser2025@";

    public static UserBuilder Default() => new();

    public UserBuilder WithUserName(string name) { _userName = name; return this; }
    public UserBuilder WithRealName(string name) { _realName = name; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithPhone(string phone) { _phoneNumber = phone; return this; }
    public UserBuilder WithRole(UserRole role) { _role = role; return this; }
    public UserBuilder WithPassword(string pwd) { _password = pwd; return this; }

    public object Build() => new
    {
        UserName = _userName,
        RealName = _realName,
        Email = _email,
        PhoneNumber = _phoneNumber,
        Role = _role,
        Password = _password,
        ConfirmPassword = _password
    };
}
