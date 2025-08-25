using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LYBT.Desktop.Users.Services;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Events;
using LYBT.Shared.Models.Contracts.Users;
using AutoMapper;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Models.Common;
// UltraThink v2.0: Desktop层直接使用DTO，移除Info层转换

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户新增/编辑对话框视图模型
    /// </summary>
    public class UserAddEditDialogViewModel : DialogViewModel, ICustomDialogAware
    {
        private readonly UserModule _userService;
        private readonly IMapper _mapper;
        private readonly UserDto? _originalUser;
        private bool _isEditMode;

        private string _userName = string.Empty;
        private string _realName = string.Empty;
        private string _email = string.Empty;
        private string _phoneNumber = string.Empty;
        private bool _isActive = true;
        private RoleItem? _selectedRole;
        private bool _isRoleSelectionEnabled;

        public List<RoleItem> Roles { get; }

        /// <summary>角色选择是否启用（新建用户时禁用，固定为普通用户）</summary>
        public bool IsRoleSelectionEnabled
        {
            get => _isRoleSelectionEnabled;
            set => SetProperty(ref _isRoleSelectionEnabled, value);
        }

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
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userService">用户服务</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <param name="user">要编辑的用户信息（null表示新增模式）</param>
        public UserAddEditDialogViewModel(
            UserModule userService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService,
            UserDto? user = null)
            : base(eventAggregator, errorHandlingService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _originalUser = user;
            _isEditMode = user != null;

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
            if (_isEditMode && user != null)
            {
                InitializeEditData(user);
            }
            else
            {
                DialogTitle = SystemConstants.AddUserDialogTitle;
                // 新增模式固定为普通用户角色
                SelectedRole = new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" };
            }

            InitializeDialog();
        }

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        public UserAddEditDialogViewModel(
            UserModule userService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            UserDto? user = null)
            : base(eventAggregator)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _originalUser = user;
            _isEditMode = user != null;

            // 角色列表初始化
            Roles = new List<RoleItem>
            {
                new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" }
            };

            IsRoleSelectionEnabled = false;

            if (_isEditMode && user != null)
            {
                InitializeEditData(user);
            }
            else
            {
                DialogTitle = SystemConstants.AddUserDialogTitle;
                SelectedRole = new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" };
            }

            InitializeDialog();
        }

        #endregion

        #region DialogViewModel Implementation

        protected override async Task<bool> SaveAsync()
        {
            try
            {
                if (!ValidateInput())
                {
                    return false;
                }

                if (_isEditMode && _originalUser != null)
                {
                    // 编辑模式
                    var updateRequest = new UserMutationDto
                    {
                        Id = _originalUser.Id,
                        Username = UserName.Trim(),
                        RealName = RealName.Trim(),
                        Role = "User", // 编辑时固定为普通用户角色
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                        IsCreateOperation = false // 设置为更新操作
                    };

                    var response = await _userService.UpdateAsync(updateRequest);
                    
                    if (!response.IsSuccess)
                    {
                        ErrorMessage = response.ErrorMessage ?? "更新用户失败";
                        return false;
                    }
                }
                else
                {
                    // 新增模式
                    var createRequest = new UserMutationDto
                    {
                        Username = UserName.Trim(),
                        RealName = RealName.Trim(),
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                        Role = "User", // 新建用户固定为普通用户角色
                        Password = "ChangeMe123", // 默认密码
                        ConfirmPassword = "ChangeMe123", // 确认密码
                        IsCreateOperation = true // 设置为创建操作
                    };

                    var response = await _userService.CreateAsync(createRequest);
                    
                    if (!response.IsSuccess)
                    {
                        ErrorMessage = response.ErrorMessage ?? "创建用户失败";
                        return false;
                    }
                }

                // 保存成功，关闭对话框
                RaiseRequestClose(true);
                return true;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("保存用户", ex);
                return false;
            }
        }

        protected override bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   SelectedRole != null;
        }

        protected override void InitializeDialog()
        {
            base.InitializeDialog();
            
            // 监听属性变化以更新Command状态
            SaveCommand.ObservesProperty(() => UserName);
            SaveCommand.ObservesProperty(() => RealName);
            SaveCommand.ObservesProperty(() => SelectedRole);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化编辑数据
        /// </summary>
        private void InitializeEditData(UserDto user)
        {
            DialogTitle = SystemConstants.EditUserDialogTitle;
            UserName = user.Username;
            RealName = user.RealName;
            Email = string.Empty; // Email字段已按优化标准移除
            PhoneNumber = user.PhoneNumber ?? string.Empty;
            IsActive = user.Status == CommonStatus.Enabled; // 使用Status属性

            // 角色固定：sysadmin是管理员（但不能修改），其他都是普通用户
            // 编辑时角色不可更改，固定显示为普通用户
            SelectedRole = new RoleItem { Value = "用户", DisplayName = "普通用户（医生）" };
        }

        private bool ValidateInput()
        {
            ClearError();

            if (string.IsNullOrWhiteSpace(UserName))
            {
                ErrorMessage = "用户名不能为空";
                return false;
            }

            if (UserName.Length > 32)
            {
                ErrorMessage = "用户名长度不能超过32个字符";
                return false;
            }

            if (string.IsNullOrWhiteSpace(RealName))
            {
                ErrorMessage = "真实姓名不能为空";
                return false;
            }

            if (RealName.Length > 50)
            {
                ErrorMessage = "真实姓名长度不能超过50个字符";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                var emailAttribute = new EmailAddressAttribute();
                if (!emailAttribute.IsValid(Email))
                {
                    ErrorMessage = "邮箱格式不正确";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length > 20)
            {
                ErrorMessage = "电话号码长度不能超过20个字符";
                return false;
            }

            if (SelectedRole == null)
            {
                ErrorMessage = "请选择用户角色";
                return false;
            }

            return true;
        }

        #endregion

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => DialogTitle ?? (_isEditMode ? "编辑用户" : "新增用户");

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog()
        {
            return !IsSaving && !IsLoading;
        }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">传入的参数</param>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters?.ContainsKey("IsEditMode") == true && parameters["IsEditMode"] is bool isEditMode)
            {
                _isEditMode = isEditMode;
            }

            if (parameters?.ContainsKey("User") == true && parameters["User"] is UserDto user)
            {
                InitializeEditData(user);
            }

            DialogTitle = _isEditMode ? "编辑用户" : "新增用户";
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源或执行其他关闭操作
        }

        /// <summary>
        /// 重写取消操作以使用ICustomDialogAware接口
        /// </summary>
        protected override void ExecuteCancel()
        {
            OnDialogClosing();
            RaiseRequestClose(false);
        }

        /// <summary>
        /// 触发关闭对话框请求
        /// </summary>
        protected void RaiseRequestClose(bool? dialogResult)
        {
            var result = dialogResult == true 
                ? CustomDialogResult.Success(new Dictionary<string, object>())
                : CustomDialogResult.Cancel();
                
            RequestClose?.Invoke(result);
        }

        #endregion
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