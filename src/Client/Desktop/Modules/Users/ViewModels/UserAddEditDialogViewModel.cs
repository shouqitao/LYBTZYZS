using System;
using System.Collections.Generic;
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
        private UserDto? _originalUser; // 🎯 修复：移除readonly，允许在OnDialogOpened中重新赋值
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

        /// <summary>是否是新用户（用于界面显示控制）</summary>
        public bool IsNewUser => !_isEditMode;

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

            // 🎯 修复：角色列表 - 使用正确的UserRole枚举值，删除护士和User角色
            Roles = new List<RoleItem>
            {
                new RoleItem { Value = "Doctor", DisplayName = "医生" },
                new RoleItem { Value = "Admin", DisplayName = "管理员" },
                new RoleItem { Value = "Pharmacist", DisplayName = "药师" },
                new RoleItem { Value = "Receptionist", DisplayName = "前台" },
                new RoleItem { Value = "Cashier", DisplayName = "收银员" },
                new RoleItem { Value = "Therapist", DisplayName = "理疗师" }
            };

            // 新建用户时角色选择禁用（固定为普通用户）
            // 编辑用户时也禁用（不允许修改角色）
            IsRoleSelectionEnabled = false;

            // 🎯 修复：构造函数不直接初始化编辑数据，等待OnDialogOpened调用
            // 这样可以避免数据被覆盖的问题
            if (_isEditMode && user != null)
            {
                DialogTitle = SystemConstants.EditUserDialogTitle;
                System.Diagnostics.Debug.WriteLine($"🔧 构造函数: 编辑模式，用户: {user.Username}");
            }
            else
            {
                DialogTitle = SystemConstants.AddUserDialogTitle;
                // 新增模式默认为医生角色
                SelectedRole = new RoleItem { Value = "Doctor", DisplayName = "医生" };
                System.Diagnostics.Debug.WriteLine("🔧 构造函数: 新增模式");
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

            // 🎯 修复：角色列表 - 使用正确的UserRole枚举值（兼容构造函数）
            Roles = new List<RoleItem>
            {
                new RoleItem { Value = "Doctor", DisplayName = "医生" },
                new RoleItem { Value = "Admin", DisplayName = "管理员" },
                new RoleItem { Value = "Pharmacist", DisplayName = "药师" },
                new RoleItem { Value = "Receptionist", DisplayName = "前台" },
                new RoleItem { Value = "Cashier", DisplayName = "收银员" },
                new RoleItem { Value = "Therapist", DisplayName = "理疗师" }
            };

            IsRoleSelectionEnabled = false;

            // 🎯 修复：兼容性构造函数也使用相同的逻辑
            if (_isEditMode && user != null)
            {
                DialogTitle = SystemConstants.EditUserDialogTitle;
                System.Diagnostics.Debug.WriteLine($"🔧 兼容构造函数: 编辑模式，用户: {user.Username}");
            }
            else
            {
                DialogTitle = SystemConstants.AddUserDialogTitle;
                SelectedRole = new RoleItem { Value = "Doctor", DisplayName = "医生" };
                System.Diagnostics.Debug.WriteLine("🔧 兼容构造函数: 新增模式");
            }

            InitializeDialog();
        }

        #endregion

        #region DialogViewModel Implementation

        protected override async Task<bool> SaveAsync()
        {
            try
            {
                // UltraThink v2.0: 移除前端验证逻辑，交由后端统一处理
                // 前端只保留最基础的UI状态检查，具体业务验证由后端Service处理

                if (_isEditMode && _originalUser != null)
                {
                    // 编辑模式
                    var updateRequest = new UserMutationDto
                    {
                        Id = _originalUser.Id,
                        Username = UserName.Trim(),
                        RealName = RealName.Trim(),
                        Role = SelectedRole?.Value ?? _originalUser.Role, // 🎯 修复：使用选中的角色，如果未选择则保持原有角色
                        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(), // 🎯 修复：包含邮箱字段
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                        Status = IsActive ? CommonStatus.Enabled : CommonStatus.Disabled, // 🎯 修复：包含状态字段
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
                        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(), // 🎯 修复：包含邮箱字段
                        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                        Role = SelectedRole?.Value ?? "Doctor", // 新建用户使用选中的角色，默认为医生
                        Status = IsActive ? CommonStatus.Enabled : CommonStatus.Disabled, // 🎯 修复：包含状态字段
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
            
            // 🎯 修复：正确设置邮箱数据
            Email = user.Email ?? string.Empty;
            PhoneNumber = user.PhoneNumber ?? string.Empty;
            
            // 🎯 修复：正确设置启用状态
            IsActive = user.Status == CommonStatus.Enabled;

            // 🎯 修复：根据实际角色正确设置选中项
            SelectedRole = Roles.FirstOrDefault(r => r.Value == user.Role) ?? 
                          Roles.FirstOrDefault(r => r.Value == "Doctor") ?? 
                          Roles.First();
            
            System.Diagnostics.Debug.WriteLine($"✅ InitializeEditData完成: UserName={UserName}, RealName={RealName}, Email={Email}, IsActive={IsActive}, Role={SelectedRole?.DisplayName}");
        }

        // UltraThink v2.0: 移除前端业务验证逻辑
        // 所有业务验证统一由后端Service和ValidationHelper处理
        // 前端专注UI交互，后端专注业务逻辑
        // 验证错误由后端ServiceResult返回，前端只负责显示错误信息

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
            System.Diagnostics.Debug.WriteLine($"🔧 OnDialogOpened 被调用，参数数量: {parameters?.Count ?? 0}");
            
            // 🎯 修复：优先检查IsEditMode参数，确保模式设置正确
            if (parameters?.ContainsKey("IsEditMode") == true && parameters["IsEditMode"] is bool isEditMode)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 参数设置编辑模式: {isEditMode}");
                _isEditMode = isEditMode;
                
                // 触发IsNewUser属性变更通知
                RaisePropertyChanged(nameof(IsNewUser));
            }
            
            // 🎯 修复：只在编辑模式且有用户数据时才初始化编辑数据
            if (_isEditMode && parameters?.ContainsKey("User") == true && parameters["User"] is UserDto user)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 编辑模式 - 初始化用户数据: {user.Username} - {user.RealName}");
                
                // 重新设置编辑数据
                _originalUser = user;
                InitializeEditData(user);
                
                // 强制触发所有属性变更通知
                RaisePropertyChanged(nameof(UserName));
                RaisePropertyChanged(nameof(RealName));
                RaisePropertyChanged(nameof(Email));
                RaisePropertyChanged(nameof(PhoneNumber));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(SelectedRole));
                
                System.Diagnostics.Debug.WriteLine($"✅ 编辑数据初始化完成: UserName={UserName}, RealName={RealName}, Email={Email}");
            }
            else if (!_isEditMode)
            {
                System.Diagnostics.Debug.WriteLine("🔧 新增模式 - 清空表单数据");
                // 新增模式：确保表单为空白状态
                UserName = string.Empty;
                RealName = string.Empty; 
                Email = string.Empty;
                PhoneNumber = string.Empty;
                IsActive = true; // 新用户默认启用
                SelectedRole = new RoleItem { Value = "Doctor", DisplayName = "医生" };
                
                // 触发UI更新
                RaisePropertyChanged(nameof(UserName));
                RaisePropertyChanged(nameof(RealName));
                RaisePropertyChanged(nameof(Email));
                RaisePropertyChanged(nameof(PhoneNumber));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(SelectedRole));
            }

            // 触发IsNewUser属性变更，确保用户名编辑状态正确
            RaisePropertyChanged(nameof(IsNewUser));

            DialogTitle = _isEditMode ? "编辑用户" : "新增用户";
            System.Diagnostics.Debug.WriteLine($"🔧 最终状态 - IsEditMode: {_isEditMode}, IsNewUser: {IsNewUser}, 标题: {DialogTitle}");
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