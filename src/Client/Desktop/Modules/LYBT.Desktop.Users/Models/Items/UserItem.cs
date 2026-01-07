using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Users.Models.Items;

/// <summary>
/// 用户列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用UserDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与UserDetailDto一致，确保XAML绑定兼容
/// OpenSpec: resolve-mapperly-source-generator-conflict - 使用BindableBase确保Mapperly兼容
/// </summary>
public class UserItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _userName = string.Empty;
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
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
        set
        {
            if (SetProperty(ref _role, value))
            {
                RaisePropertyChanged(nameof(RoleDisplayText));
                RaisePropertyChanged(nameof(RoleColor));
                RaisePropertyChanged(nameof(IsAdmin));
                RaisePropertyChanged(nameof(IsDoctor));
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
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
        set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(StatusColor));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(CanEdit));
                RaisePropertyChanged(nameof(CanResetPassword));
            }
        }
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    /// <summary>
    /// 更新时间
    /// </summary>
    private DateTime? _updatedAt;
    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
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

    #region 计算属性

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
    public string DisplayText => $"{RealName}({UserName}) - {RoleDisplayText}";

    /// <summary>
    /// 是否可以编辑
    /// </summary>
    public bool CanEdit => IsActive;

    /// <summary>
    /// 是否可以删除
    /// </summary>
    public bool CanDelete => !UserName.Equals("sysadmin", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 是否可以重置密码
    /// </summary>
    public bool CanResetPassword => IsActive;

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从UserDetailDto更新当前项
    /// </summary>
    public void UpdateFromDto(UserDetailDto dto)
    {
        Id = dto.Id;
        UserName = dto.UserName;
        RealName = dto.RealName;
        Role = dto.Role;
        Email = dto.Email;
        PhoneNumber = dto.PhoneNumber;
        Department = null;
        Title = null;
        PinYinCode = dto.PinYinCode;
        Status = dto.Status;
        CreatedAt = dto.CreatedAt;
        UpdatedAt = dto.UpdatedAt;
    }

    #endregion
}
