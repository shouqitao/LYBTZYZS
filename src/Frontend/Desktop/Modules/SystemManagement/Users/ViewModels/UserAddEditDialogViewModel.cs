using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Users.ViewModels
{
    /// <summary>
    /// 用户新增/编辑对话框视图模型
    /// </summary>
    public class UserAddEditDialogViewModel : BindableBase
    {
        private readonly IUserService _userService;
        private readonly UserInfo? _originalUser;
        
        private string _userName = string.Empty;
        private string _realName = string.Empty;
        private string? _email;
        private string? _phoneNumber;
        private bool _isActive = true;
        private RoleItem? _selectedRole;
        private string _validationMessage = string.Empty;
        private bool _isNewUser;
        
        public List<RoleItem> Roles { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>用户名</summary>
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>真实姓名</summary>
        public string RealName
        {
            get => _realName;
            set => SetProperty(ref _realName, value);
        }

        /// <summary>邮箱</summary>
        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        /// <summary>电话号码</summary>
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        /// <summary>是否启用</summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>选中的角色</summary>
        public RoleItem? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        /// <summary>验证消息</summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>是否为新用户</summary>
        public bool IsNewUser
        {
            get => _isNewUser;
            set => SetProperty(ref _isNewUser, value);
        }

        /// <summary>窗口标题</summary>
        public string WindowTitle => IsNewUser ? "新增用户" : "编辑用户";

        /// <summary>对话框结果</summary>
        public bool? DialogResult { get; private set; }

        public UserAddEditDialogViewModel(IUserService userService, UserInfo? user = null)
        {
            _userService = userService;
            _originalUser = user;
            _isNewUser = user == null;

            // 初始化角色列表 - 使用共享扩展方法
            var rolesWithDisplayNames = UserRoleExtensions.GetAllRolesWithDisplayNames();
            Roles = rolesWithDisplayNames
                .Select(r => new RoleItem { Value = r.Role, DisplayName = r.DisplayName })
                .ToList();

            // 如果是编辑模式，加载用户数据
            if (user != null)
            {
                LoadUserData(user);
            }
            else
            {
                // 新增模式默认选择第一个角色
                SelectedRole = Roles.FirstOrDefault();
            }

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 监听属性变化以更新命令状态
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UserName) || e.PropertyName == nameof(RealName))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            };
        }

        private void LoadUserData(UserInfo user)
        {
            UserName = user.UserName;
            RealName = user.RealName;
            Email = user.Email;
            PhoneNumber = user.PhoneNumber;
            IsActive = user.IsActive;
            SelectedRole = Roles.FirstOrDefault(r => r.Value == user.Role);
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(UserName) && 
                   !string.IsNullOrWhiteSpace(RealName) && 
                   SelectedRole != null;
        }

        private async void ExecuteSave()
        {
            if (!ValidateInput())
                return;

            try
            {
                bool success;
                
                if (IsNewUser)
                {
                    // 新增用户
                    var createRequest = new UserCreateRequest
                    {
                        UserName = UserName.Trim(),
                        RealName = RealName.Trim(),
                        Role = SelectedRole!.Value,
                        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim()
                    };

                    var response = await _userService.CreateUserAsync(createRequest);
                    success = response.IsSuccess;
                    
                    if (!success)
                    {
                        ValidationMessage = response.Message ?? "创建用户失败";
                        return;
                    }
                }
                else
                {
                    // 更新用户
                    var updateRequest = new UserUpdateRequest
                    {
                        Id = _originalUser!.Id,
                        UserName = UserName.Trim(),
                        RealName = RealName.Trim(),
                        Role = SelectedRole!.Value,
                        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                        IsActive = IsActive
                    };

                    var response = await _userService.UpdateUserAsync(updateRequest);
                    success = response.IsSuccess;
                    
                    if (!success)
                    {
                        ValidationMessage = response.Message ?? "更新用户失败";
                        return;
                    }
                }

                // 成功后关闭对话框
                DialogResult = true;
                CloseDialog();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"操作失败: {ex.Message}";
            }
        }

        private void ExecuteCancel()
        {
            DialogResult = false;
            CloseDialog();
        }

        private void CloseDialog()
        {
            // 寻找并关闭当前对话框
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = DialogResult;
                    window.Close();
                    break;
                }
            }
        }

        private bool ValidateInput()
        {
            ValidationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(UserName))
            {
                ValidationMessage = "用户名不能为空";
                return false;
            }

            if (UserName.Length > 32)
            {
                ValidationMessage = "用户名长度不能超过32个字符";
                return false;
            }

            if (string.IsNullOrWhiteSpace(RealName))
            {
                ValidationMessage = "真实姓名不能为空";
                return false;
            }

            if (RealName.Length > 50)
            {
                ValidationMessage = "真实姓名长度不能超过50个字符";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                var emailAttribute = new EmailAddressAttribute();
                if (!emailAttribute.IsValid(Email))
                {
                    ValidationMessage = "邮箱格式不正确";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length > 20)
            {
                ValidationMessage = "电话号码长度不能超过20个字符";
                return false;
            }

            if (SelectedRole == null)
            {
                ValidationMessage = "请选择用户角色";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 角色项
    /// </summary>
    public class RoleItem
    {
        public UserRole Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}