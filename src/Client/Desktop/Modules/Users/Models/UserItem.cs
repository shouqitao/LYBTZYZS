using System;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Models;

/// <summary>
/// 用户列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用UserDto，实现Desktop层与Shared层的解耦
/// 保持属性名与UserDto一致，确保XAML绑定兼容
/// </summary>
public class UserItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _realName = string.Empty;
    public string RealName
    {
        get => _realName;
        set => SetProperty(ref _realName, value);
    }

    private UserRole _role;
    public UserRole Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    private string? _email;
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string? _phoneNumber;
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    private string? _department;
    public string? Department
    {
        get => _department;
        set => SetProperty(ref _department, value);
    }

    private string? _title;
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string? _pinYinCode;
    public string? PinYinCode
    {
        get => _pinYinCode;
        set => SetProperty(ref _pinYinCode, value);
    }

    private CommonStatus _status;
    public CommonStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private DateTime _createTime;
    public DateTime CreateTime
    {
        get => _createTime;
        set => SetProperty(ref _createTime, value);
    }

    private DateTime? _updateTime;
    public DateTime? UpdateTime
    {
        get => _updateTime;
        set => SetProperty(ref _updateTime, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 从UserDto创建UserItem
    /// </summary>
    public static UserItem FromDto(UserDto dto)
    {
        return new UserItem
        {
            Id = dto.Id,
            Username = dto.Username,
            RealName = dto.RealName,
            Role = dto.Role,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Department = null, // UserDto中没有此属性
            Title = null, // UserDto中没有此属性
            PinYinCode = dto.PinYinCode,
            Status = dto.Status,
            CreateTime = dto.CreateTime,
            UpdateTime = dto.UpdateTime
        };
    }

    /// <summary>
    /// 转换为UserDto（用于API调用）
    /// </summary>
    public UserDto ToDto()
    {
        return new UserDto
        {
            Id = Id,
            Username = Username,
            RealName = RealName,
            Role = Role,
            Email = Email,
            PhoneNumber = PhoneNumber,
            // Department 和 Title 在 UserDto 中不存在
            PinYinCode = PinYinCode,
            Status = Status,
            CreateTime = CreateTime,
            UpdateTime = UpdateTime
        };
    }

    /// <summary>
    /// 从UserDto更新当前项
    /// </summary>
    public void UpdateFromDto(UserDto dto)
    {
        Id = dto.Id;
        Username = dto.Username;
        RealName = dto.RealName;
        Role = dto.Role;
        Email = dto.Email;
        PhoneNumber = dto.PhoneNumber;
        Department = null; // UserDto中没有此属性
        Title = null; // UserDto中没有此属性
        PinYinCode = dto.PinYinCode;
        Status = dto.Status;
        CreateTime = dto.CreateTime;
        UpdateTime = dto.UpdateTime;
    }

    /// <summary>
    /// 角色显示文本
    /// </summary>
    public string RoleDisplayText => Role switch
    {
        UserRole.Admin => "管理员",
        UserRole.Doctor => "医师",
        _ => "未知"
    };

    /// <summary>
    /// 角色颜色（用于UI绑定）
    /// </summary>
    public string RoleColor => Role switch
    {
        UserRole.Admin => "#9C27B0",
        UserRole.Doctor => "#2196F3",
        _ => "#757575"
    };

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        CommonStatus.Enabled => "正常",
        CommonStatus.Disabled => "禁用",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        CommonStatus.Enabled => "#4CAF50",
        CommonStatus.Disabled => "#F44336",
        _ => "#757575"
    };

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive => Status == CommonStatus.Enabled;

    /// <summary>
    /// 是否是管理员
    /// </summary>
    public bool IsAdmin => Role == UserRole.Admin;

    /// <summary>
    /// 是否是医师
    /// </summary>
    public bool IsDoctor => Role == UserRole.Doctor;

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{RealName}({Username}) - {RoleDisplayText}";

    /// <summary>
    /// 是否可以编辑
    /// </summary>
    public bool CanEdit => IsActive;

    /// <summary>
    /// 是否可以删除
    /// </summary>
    public bool CanDelete => !Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase); // 系统管理员不能删除

    /// <summary>
    /// 是否可以重置密码
    /// </summary>
    public bool CanResetPassword => IsActive;
}