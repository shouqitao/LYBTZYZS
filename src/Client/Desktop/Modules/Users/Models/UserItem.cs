using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Models;

/// <summary>
/// 用户列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用UserDto，实现Desktop层与Shared层的解耦
/// 保持属性名与UserDto一致，确保XAML绑定兼容
/// </summary>
public partial class UserItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string realName = string.Empty;

    [ObservableProperty]
    private UserRole role;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? phoneNumber;

    [ObservableProperty]
    private string? department;

    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? pinYinCode;

    [ObservableProperty]
    private CommonStatus status;

    [ObservableProperty]
    private DateTime createTime;

    [ObservableProperty]
    private DateTime? updateTime;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isHighlighted;

    [ObservableProperty]
    private bool isEditing;

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
            Department = dto.Department,
            Title = dto.Title,
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
            Department = Department,
            Title = Title,
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
        Department = dto.Department;
        Title = dto.Title;
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
    public bool CanDelete => Id != 1; // 系统管理员ID=1不能删除

    /// <summary>
    /// 是否可以重置密码
    /// </summary>
    public bool CanResetPassword => IsActive;
}