using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Models.Items;

/// <summary>
/// 用户列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用UserDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与UserDetailDto一致，确保XAML绑定兼容
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
/// </summary>
public partial class UserItem : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _realName = string.Empty;

    [ObservableProperty]
    private UserRole _role;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _phoneNumber;

    [ObservableProperty]
    private string? _department;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _pinYinCode;

    [ObservableProperty]
    private CommonStatus _status;

    /// <summary>
    /// 创建时间 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为CreatedAt，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private DateTime _createdAt;

    /// <summary>
    /// 更新时间 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为UpdatedAt，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private DateTime? _updatedAt;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isHighlighted;

    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// 从UserDetailDto创建UserItem
    /// OpenSpec: unify-frontend-backend-types Phase 6 - 时间字段命名统一
    /// </summary>
    /// <remarks>已废弃：请使用UserMappingService.ToItem()</remarks>
    [Obsolete("请使用UserMappingService.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public static UserItem FromDto(UserDetailDto dto)
    {
        return new UserItem
        {
            Id = dto.Id,
            UserName = dto.UserName,
            RealName = dto.RealName,
            Role = dto.Role,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Department = null, // UserDetailDto中没有此属性
            Title = null, // UserDetailDto中没有此属性
            PinYinCode = dto.PinYinCode,
            Status = dto.Status,
            CreatedAt = dto.CreatedAt, // OpenSpec: unify-frontend-backend-types - 直接映射
            UpdatedAt = dto.UpdatedAt  // OpenSpec: unify-frontend-backend-types - 直接映射
        };
    }

    /// <summary>
    /// 转换为UserDetailDto（用于API调用）
    /// OpenSpec: unify-frontend-backend-types Phase 6 - 时间字段命名统一
    /// </summary>
    /// <remarks>已废弃：请使用UserMappingService.ToDto()</remarks>
    [Obsolete("请使用UserMappingService.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public UserDetailDto ToDto()
    {
        return new UserDetailDto
        {
            Id = Id,
            UserName = UserName,
            RealName = RealName,
            Role = Role,
            Email = Email,
            PhoneNumber = PhoneNumber,
            // Department 和 Title 在 UserDetailDto 中不存在
            PinYinCode = PinYinCode,
            Status = Status,
            CreatedAt = CreatedAt, // OpenSpec: unify-frontend-backend-types - 直接映射
            UpdatedAt = UpdatedAt ?? DateTime.MinValue // OpenSpec: unify-frontend-backend-types - 直接映射
        };
    }

    /// <summary>
    /// 从UserDetailDto更新当前项
    /// OpenSpec: unify-frontend-backend-types Phase 6 - 时间字段命名统一
    /// </summary>
    public void UpdateFromDto(UserDetailDto dto)
    {
        Id = dto.Id;
        UserName = dto.UserName;
        RealName = dto.RealName;
        Role = dto.Role;
        Email = dto.Email;
        PhoneNumber = dto.PhoneNumber;
        Department = null; // UserDetailDto中没有此属性
        Title = null; // UserDetailDto中没有此属性
        PinYinCode = dto.PinYinCode;
        Status = dto.Status;
        CreatedAt = dto.CreatedAt; // OpenSpec: unify-frontend-backend-types - 直接映射
        UpdatedAt = dto.UpdatedAt; // OpenSpec: unify-frontend-backend-types - 直接映射
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
    public string DisplayText => $"{RealName}({UserName}) - {RoleDisplayText}";

    /// <summary>
    /// 是否可以编辑
    /// </summary>
    public bool CanEdit => IsActive;

    /// <summary>
    /// 是否可以删除
    /// </summary>
    public bool CanDelete => !UserName.Equals("sysadmin", StringComparison.OrdinalIgnoreCase); // 系统管理员不能删除

    /// <summary>
    /// 是否可以重置密码
    /// </summary>
    public bool CanResetPassword => IsActive;
}
