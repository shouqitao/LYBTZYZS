using System.ComponentModel.DataAnnotations;
using System.Windows;
using LYBT.Desktop.Users.Services;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Users;
using AutoMapper;
// UltraThink v2.0: Desktop层直接使用DTO，移除Info层转换

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户新增/编辑对话框视图模型
    /// </summary>
    public class UserAddEditDialogViewModel : BindableBase
    {
        private readonly UserModuleService _userService;
        private readonly IMapper _mapper;
        private readonly UserDto? _originalUser;

        private string _userName = string.Empty;
        private string _realName = string.Empty;
        private string _email = string.Empty;
        private string _phoneNumber = string.Empty;
        private bool _isActive = true;
        private RoleItem? _selectedRole;
        private string _validationMessage = string.Empty;
        private bool _isNewUser;
        private bool _isRoleSelectionEnabled;

        public List<RoleItem> Roles { get; }

        /// <summary>角色选择是否启用（新建用户时禁用，固定为普通用户）</summary>
        public bool IsRoleSelectionEnabled
        {
            get => _isRoleSelectionEnabled;
            set => SetProperty(ref _isRoleSelectionEnabled, value);
        }
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
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        /// <summary>电话号码</summary>
        public string PhoneNumber
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

        /// <summary>保存完成回调</summary>
        public Action<bool>? SaveCompleteCallback { get; set; }

        /// <summary>关闭对话框回调</summary>
        public Action? CloseDialogCallback { get; set; }

        public UserAddEditDialogViewModel(UserModuleService userService, IMapper mapper, UserDto? user = null)
        {
            _userService = userService;
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _originalUser = user;
            _isNewUser = user == null;

            // 角色列表 - 只允许创建普通用户
            // 管理员只限sysadmin，不能通过用户管理创建
            Roles = new List<RoleItem>
            {
                new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" }
            };

            // 新建用户时角色选择禁用（固定为普通用户）
            // 编辑用户时也禁用（不允许修改角色）
            IsRoleSelectionEnabled = false;

            // 如果是编辑模式，加载用户数据
            if (user != null)
            {
                LoadUserData(user);
            }
            else
            {
                // 新增模式固定为普通用户角色
                SelectedRole = new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" };
            }

            SaveCommand = new DelegateCommand(async () => await ExecuteSave(), CanExecuteSave);
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

        private void LoadUserData(UserDto user)
        {
            UserName = user.Username;
            RealName = user.RealName;
            Email = string.Empty; // Email字段已按优化标准移除
            PhoneNumber = user.PhoneNumber ?? string.Empty;
            IsActive = user.Status == CommonStatus.Enabled; // 使用Status属性

            // 角色固定：sysadmin是管理员（但不能修改），其他都是普通用户
            // 编辑时角色不可更改，固定显示为普通用户
            SelectedRole = new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" };
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   SelectedRole != null;
        }

        private async Task ExecuteSave()
        {
            if (!ValidateInput())
                return;

            try
            {
                bool success;

                if (IsNewUser)
                {
                    // UltraThink v2.0: 直接创建UserCreateDto
                    var createRequest = new UserCreateDto
                    {
                        Username = UserName.Trim(),
                        RealName = RealName.Trim(),
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                        Role = "User", // 新建用户固定为普通用户角色
                        Password = "ChangeMe123", // 默认密码
                        ConfirmPassword = "ChangeMe123" // 确认密码
                    };

                    var response = await _userService.CreateAsync(createRequest);
                    success = response.IsSuccess;

                    if (!success)
                    {
                        ValidationMessage = response.ErrorMessage ?? "创建用户失败";
                        return;
                    }
                }
                else
                {
                    // 更新用户
                    if (_originalUser == null)
                    {
                        ValidationMessage = "原始用户信息不能为空";
                        return;
                    }

                    // UltraThink v2.0: 直接创建UserUpdateDto
                    var updateRequest = new UserUpdateDto
                    {
                        Id = _originalUser.Id,
                        Username = UserName.Trim(),
                        RealName = RealName.Trim(),
                        Role = "User", // 编辑时固定为普通用户角色
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim()
                    };

                    var response = await _userService.UpdateAsync(updateRequest);
                    success = response.IsSuccess;

                    if (!success)
                    {
                        ValidationMessage = response.ErrorMessage ?? "更新用户失败";
                        return;
                    }
                }

                // 成功后调用回调并关闭对话框
                DialogResult = true;
                SaveCompleteCallback?.Invoke(true);
                // 注意：不要在这里调用 CloseDialog()，让回调处理关闭
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
        public string Value { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}