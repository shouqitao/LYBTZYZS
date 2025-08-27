using System;
using System.Collections.Generic;
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
    /// 用户管理视图模型（UltraThink v2.0 小诊所精简版）
    /// 移除过度设计的批量操作、锁定功能、多选功能，专注核心CRUD操作
    /// 适用于20人以下小诊所的简单直接的用户管理需求
    /// </summary>
    public class UserManagementViewModel : NewBaseListViewModel<UserDto>
    {
        #region Fields

        private readonly UserModule _userService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装
        private UserDto? _selectedUser;

        #endregion

        #region Properties

        /// <summary>选中的用户 - UltraThink v2.0: 直接使用DTO</summary>
        public UserDto? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ResetPasswordCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // 暴露基类的搜索和分页属性供XAML绑定
        public string SearchKeyword
        {
            get => SearchManager.SearchKeyword;
            set => SearchManager.SearchKeyword = value;
        }

        public DelegateCommand SearchCommand { get; private set; }

        public int CurrentPage => PaginationCoordinator.CurrentPage;
        public int TotalPages => PaginationCoordinator.TotalPages;
        public DelegateCommand FirstPageCommand { get; private set; }
        public DelegateCommand PreviousPageCommand { get; private set; }
        public DelegateCommand NextPageCommand { get; private set; }
        public DelegateCommand LastPageCommand { get; private set; }

        public string StatusText => $"共 {PaginationCoordinator.TotalCount} 条记录";

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<UserDto> ViewDetailsCommand { get; private set; }
        public DelegateCommand<UserDto> EditCommand { get; private set; }
        public DelegateCommand<UserDto> DeleteCommand { get; private set; }
        public DelegateCommand<UserDto> ResetPasswordCommand { get; private set; }
        public DelegateCommand<UserDto> ToggleStatusCommand { get; private set; }

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - BatchEnableCommand/BatchDisableCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计

        #endregion

        #region Constructor

        public UserManagementViewModel(
            UserModule userService,
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
            
            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除选择状态变化监听: 多选功能已移除
            
            // 修复: 移除Task.Run，使用Dispatcher异步加载避免跨线程UI更新问题
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(async () => await RefreshDataAsync()));
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            // 修复: 首先调用基类的命令初始化 (RefreshCommand, ClearErrorCommand, SelectItemCommand)
            base.InitializeCommands();
            
            AddCommand = new DelegateCommand(async () => await AddUserAsync());
            ViewDetailsCommand = new DelegateCommand<UserDto>(async user => await ViewDetailsAsync(user), CanExecuteUserCommand);
            EditCommand = new DelegateCommand<UserDto>(async user => await EditUserAsync(user), CanExecuteUserCommand);
            DeleteCommand = new DelegateCommand<UserDto>(async user => await DeleteUserAsync(user), CanExecuteUserCommand);
            ResetPasswordCommand = new DelegateCommand<UserDto>(async user => await ResetPasswordAsync(user), CanExecuteUserCommand);
            ToggleStatusCommand = new DelegateCommand<UserDto>(async user => await ToggleStatusAsync(user), CanExecuteUserCommand);
            
            // 搜索和分页命令初始化
            SearchCommand = new DelegateCommand(async () => await SearchManager.ExecuteSearchAsync());
            FirstPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToFirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToPreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToNextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToLastPageAsync());
            
            // UltraThink v2.0: 删除批量操作命令初始化 - 20人以下小诊所不需要复杂的批量操作
        }

        private bool CanExecuteUserCommand(UserDto user)
        {
            var canExecute = user != null && !IsLoading;
            System.Diagnostics.Debug.WriteLine($"🔍 CanExecuteUserCommand: user={user?.Username}, IsLoading={IsLoading}, canExecute={canExecute}");
            return canExecute;
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<UserDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 LoadDataAsync 开始: CurrentPage={request.CurrentPage}, PageSize={request.PageSize}, SearchKeyword='{request.SearchKeyword}'");
            
            try
            {
                // 转换为用户查询DTO
                var userQuery = new UserPagedQueryDto
                {
                    PageIndex = request.CurrentPage,
                    PageSize = request.PageSize,
                    Keyword = request.SearchKeyword
                };

                System.Diagnostics.Debug.WriteLine($"🔄 调用 _userService.GetPagedAsync...");
                var result = await _userService.GetPagedAsync(userQuery);
                
                if (result.IsSuccess)
                {
                    var totalCount = result.Data?.TotalCount ?? 0;
                    var itemCount = result.Data?.Items?.Count ?? 0;
                    System.Diagnostics.Debug.WriteLine($"✅ LoadDataAsync 成功: 总数={totalCount}, 当前页项目数={itemCount}");
                    
                    if (itemCount > 0)
                    {
                        var firstUser = result.Data.Items.First();
                        System.Diagnostics.Debug.WriteLine($"📄 第一个用户: {firstUser.Username} - {firstUser.RealName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ LoadDataAsync 失败: {result.ErrorMessage}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 LoadDataAsync 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 异常详情: {ex}");
                throw;
            }
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed

        #endregion

        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<UserDto>数据

        #region CRUD Operations

        private async Task AddUserAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔘 AddUserAsync 被调用");
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false
                };
                
                var result = await _dialogService.ShowDialogAsync("UserAddEditDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync("用户添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "添加用户失败");
                ShowError($"添加用户失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加用户失败: {ex.Message}", "错误");
            }
        }

        private async Task ViewDetailsAsync(UserDto user)
        {
            System.Diagnostics.Debug.WriteLine($"👁️ ViewDetailsAsync 被调用: {user?.Username}");
            if (user == null) 
            {
                System.Diagnostics.Debug.WriteLine("❌ ViewDetailsAsync: user为null");
                return;
            }

            try
            {
                // UltraThink Command绑定优化：实现查看用户详情
                var userInfo = $@"用户详细信息：
用户ID: {user.Id}
用户名: {user.UserName}
真实姓名: {user.RealName ?? "未设置"}
角色: {user.Role}
状态: {(user.Status == CommonStatus.Enabled ? "启用" : "禁用")}
创建时间: {user.CreateTime:yyyy-MM-dd HH:mm:ss}
更新时间: {user.UpdateTime:yyyy-MM-dd HH:mm:ss}";

                await _dialogService.ShowInformationAsync(userInfo, "用户详情");
            }
            catch (Exception ex)
            {
                LogError(ex, "查看用户详情失败: {UserId}", user.Id);
                ShowError($"查看用户详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看用户详情失败: {ex.Message}", "错误");
            }
        }

        private async Task EditUserAsync(UserDto user)
        {
            if (user == null) return;
            
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = true,
                    ["User"] = user
                };
                
                var result = await _dialogService.ShowDialogAsync("UserAddEditDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync($"用户 {user.RealName ?? user.UserName} 信息更新成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑用户失败: {UserId}", user.Id);
                ShowError($"编辑用户失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑用户失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteUserAsync(UserDto user)
        {
            if (user == null) return;
            
            // 系统管理员不允许删除
            if (user.Role == "Admin")
            {
                await _dialogService.ShowWarningAsync("不允许删除系统管理员账号", "警告");
                return;
            }
            
            // 用户不支持真正删除，只能禁用
            await ToggleStatusAsync(user);
        }

        #endregion

        #region Business Operations

        private async Task ResetPasswordAsync(UserDto user)
        {
            if (user == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要重置用户 {user.RealName ?? user.UserName} 的密码吗？",
                "重置密码");

            if (confirm)
            {
                try
                {
                    var result = await _userService.ResetPasswordAsync(user.Id, "ChangeMe123");
                    
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
            }
        }

        private async Task ToggleStatusAsync(UserDto user)
        {
            if (user == null) return;

            var isEnabled = user.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}用户 {user.RealName ?? user.UserName} 吗？",
                $"{action}用户");

            if (confirm)
            {
                try
                {
                    ServiceResult<bool> result;
                    if (isEnabled)
                    {
                        result = await _userService.DisableAsync(user.Id);
                    }
                    else
                    {
                        result = await _userService.EnableAsync(user.Id);
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
            }
        }

        #endregion

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchEnableAsync, BatchDisableAsync 等功能

        // UltraThink v2.0: 删除所有选择管理功能 - 20人以下小诊所不需要复杂的多选功能
        // 包括: ClearSelection, SelectAll 等功能
    }
}