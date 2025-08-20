using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.Users;
using LYBT.Desktop.Services;
using LYBT.Desktop.Users.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型（UltraThink架构重构版）
    /// 使用新的三层架构：PaginationCoordinator + SearchManager + NewBaseListViewModel
    /// 实现完全的关注点分离和单一职责原则
    /// </summary>
    public class UserManagementViewModel : NewBaseListViewModel<UserDto>
    {
        #region Fields

        private readonly UserModuleService _userService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        private ObservableCollection<UserViewModel> _userViewModels = new();
        private UserViewModel? _selectedUserViewModel;

        #endregion

        #region Properties

        /// <summary>用户视图模型集合 - 替代原始的UserInfo集合</summary>
        public ObservableCollection<UserViewModel> UserViewModels
        {
            get => _userViewModels;
            set => SetProperty(ref _userViewModels, value);
        }

        /// <summary>选中的用户视图模型</summary>
        public UserViewModel? SelectedUserViewModel
        {
            get => _selectedUserViewModel;
            set
            {
                if (SetProperty(ref _selectedUserViewModel, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ResetPasswordCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>批量选中的用户数量</summary>
        public int SelectedUsersCount => UserViewModels.Count(u => u.IsSelected);

        /// <summary>是否有选中的用户</summary>
        public bool HasSelectedUsers => SelectedUsersCount > 0;

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<UserViewModel> EditCommand { get; private set; } = null!;
        public DelegateCommand<UserViewModel> DeleteCommand { get; private set; } = null!;
        public DelegateCommand<UserViewModel> ResetPasswordCommand { get; private set; } = null!;
        public DelegateCommand<UserViewModel> ToggleStatusCommand { get; private set; } = null!;
        public DelegateCommand BatchEnableCommand { get; private set; } = null!;
        public DelegateCommand BatchDisableCommand { get; private set; } = null!;
        public DelegateCommand ClearSelectionCommand { get; private set; } = null!;
        public DelegateCommand SelectAllCommand { get; private set; } = null!;

        #endregion

        #region Constructor

        public UserManagementViewModel(
            UserModuleService userService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<UserManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();
            
            // 监听选择状态变化
            UserViewModels.CollectionChanged += (s, e) => UpdateSelectionProperties();
            
            // 初始化加载数据
            _ = RefreshDataAsync();
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddUserAsync());
            EditCommand = new DelegateCommand<UserViewModel>(async user => await EditUserAsync(user), CanExecuteUserCommand);
            DeleteCommand = new DelegateCommand<UserViewModel>(async user => await DeleteUserAsync(user), CanExecuteUserCommand);
            ResetPasswordCommand = new DelegateCommand<UserViewModel>(async user => await ResetPasswordAsync(user), CanExecuteUserCommand);
            ToggleStatusCommand = new DelegateCommand<UserViewModel>(async user => await ToggleStatusAsync(user), CanExecuteUserCommand);
            
            BatchEnableCommand = new DelegateCommand(async () => await BatchEnableAsync(), () => HasSelectedUsers);
            BatchDisableCommand = new DelegateCommand(async () => await BatchDisableAsync(), () => HasSelectedUsers);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedUsers);
            SelectAllCommand = new DelegateCommand(SelectAll);
        }

        private bool CanExecuteUserCommand(UserViewModel user)
        {
            return user != null && !IsLoading;
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<UserDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // 转换为用户查询DTO
            var userQuery = new UserPagedQueryDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword
            };

            return await _userService.GetPagedAsync(userQuery);
        }

        protected override void OnDataLoaded(PagedResult<UserDto> data)
        {
            base.OnDataLoaded(data);
            
            // 将UserDto转换为UserViewModel
            UpdateUserViewModels(data.Items);
        }

        protected override void OnDataLoadFailed(string errorMessage)
        {
            base.OnDataLoadFailed(errorMessage);
            
            // 清空用户视图模型
            UserViewModels.Clear();
            UpdateSelectionProperties();
            
            // 显示错误
            _ = _dialogService.ShowErrorAsync(errorMessage, "加载失败");
        }

        #endregion

        #region User ViewModels Management

        private void UpdateUserViewModels(System.Collections.Generic.List<UserDto> userDtos)
        {
            // 保存当前选择状态
            var selectedIds = UserViewModels.Where(u => u.IsSelected).Select(u => u.Id).ToHashSet();
            
            // 清空并重新创建
            UserViewModels.Clear();
            
            foreach (var dto in userDtos)
            {
                // 直接使用UserDto创建UserViewModel
                var userViewModel = UserViewModel.Create(dto);
                
                // 恢复选择状态
                if (selectedIds.Contains(userViewModel.Id))
                {
                    userViewModel.IsSelected = true;
                }
                
                // 监听选择状态变化
                userViewModel.State.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(UserStateViewModel.IsSelected))
                    {
                        UpdateSelectionProperties();
                    }
                };
                
                UserViewModels.Add(userViewModel);
            }
            
            UpdateSelectionProperties();
        }

        private void UpdateSelectionProperties()
        {
            RaisePropertyChanged(nameof(SelectedUsersCount));
            RaisePropertyChanged(nameof(HasSelectedUsers));
            
            BatchEnableCommand.RaiseCanExecuteChanged();
            BatchDisableCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region CRUD Operations

        private async Task AddUserAsync()
        {
            try
            {
                // TODO: 实现用户创建对话框
                await _dialogService.ShowInformationAsync("新增用户功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "添加用户失败");
                ShowError($"添加用户失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加用户失败: {ex.Message}", "错误");
            }
        }

        private async Task EditUserAsync(UserViewModel userViewModel)
        {
            if (userViewModel == null) return;
            
            try
            {
                // TODO: 实现用户编辑对话框
                await _dialogService.ShowInformationAsync($"编辑用户 {userViewModel.DisplayName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑用户失败: {UserId}", userViewModel.Id);
                ShowError($"编辑用户失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑用户失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteUserAsync(UserViewModel userViewModel)
        {
            if (userViewModel == null) return;
            
            // 系统管理员不允许删除
            if (userViewModel.Display.IsSysAdmin)
            {
                await _dialogService.ShowWarningAsync("不允许删除系统管理员账号", "警告");
                return;
            }
            
            // 用户不支持真正删除，只能禁用
            await ToggleStatusAsync(userViewModel);
        }

        #endregion

        #region Business Operations

        private async Task ResetPasswordAsync(UserViewModel userViewModel)
        {
            if (userViewModel == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要重置用户 {userViewModel.DisplayName} 的密码吗？",
                "重置密码");

            if (confirm)
            {
                try
                {
                    userViewModel.IsLoading = true;
                    
                    var result = await _userService.ResetPasswordAsync(userViewModel.Id);
                    
                    if (result.IsSuccess)
                    {
                        await _dialogService.ShowInformationAsync(
                            "密码重置成功！新密码: ChangeMe123", 
                            "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "密码重置失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "用户操作失败");
                    ShowError($"用户操作失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"用户操作失败: {ex.Message}", "错误");
                }
                finally
                {
                    userViewModel.IsLoading = false;
                }
            }
        }

        private async Task ToggleStatusAsync(UserViewModel userViewModel)
        {
            if (userViewModel == null) return;

            var isEnabled = userViewModel.UserData.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}用户 {userViewModel.DisplayName} 吗？",
                $"{action}用户");

            if (confirm)
            {
                try
                {
                    userViewModel.IsLoading = true;
                    
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _userService.DisableAsync(userViewModel.Id);
                    }
                    else
                    {
                        result = await _userService.EnableAsync(userViewModel.Id);
                    }

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"用户{action}成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? $"用户{action}失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "用户操作失败");
                    ShowError($"用户操作失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"用户操作失败: {ex.Message}", "错误");
                }
                finally
                {
                    userViewModel.IsLoading = false;
                }
            }
        }

        #endregion

        #region Batch Operations

        private async Task BatchEnableAsync()
        {
            var selectedUsers = UserViewModels.Where(u => u.IsSelected).ToList();
            if (!selectedUsers.Any()) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要启用选中的 {selectedUsers.Count} 个用户吗？",
                "批量启用");

            if (confirm)
            {
                try
                {
                    var ids = selectedUsers.Select(u => u.Id).ToList();
                    var result = await _userService.BatchEnableAsync(ids);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"已成功启用 {result.Data} 个用户", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "批量启用失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "用户操作失败");
                    ShowError($"用户操作失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"用户操作失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task BatchDisableAsync()
        {
            var selectedUsers = UserViewModels.Where(u => u.IsSelected && !u.Display.IsSysAdmin).ToList();
            if (!selectedUsers.Any())
            {
                await _dialogService.ShowWarningAsync("没有可禁用的用户（系统管理员不能被禁用）", "警告");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要禁用选中的 {selectedUsers.Count} 个用户吗？",
                "批量禁用");

            if (confirm)
            {
                try
                {
                    var ids = selectedUsers.Select(u => u.Id).ToList();
                    var result = await _userService.BatchDisableAsync(ids);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"已成功禁用 {result.Data} 个用户", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "批量禁用失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "用户操作失败");
                    ShowError($"用户操作失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"用户操作失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

        #region Selection Management

        private void ClearSelection()
        {
            foreach (var user in UserViewModels)
            {
                user.IsSelected = false;
            }
        }

        private void SelectAll()
        {
            foreach (var user in UserViewModels)
            {
                user.IsSelected = true;
            }
        }

        #endregion
    }
}