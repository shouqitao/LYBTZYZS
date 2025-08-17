using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Core;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using LYBT.Shared.Interfaces.Services;
using AutoMapper;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Desktop.Users.Services.Interfaces;
using LYBT.Desktop.Core.Models.Common;
using Prism.Mvvm;
// UltraThink四层架构重构：使用模块化服务，消除对SharedServices的依赖

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型（UltraThink架构重构版）
    /// UltraThink模块化架构：使用IUserModuleService，实现模块自包含
    /// </summary>
    public class UserManagementViewModelSimple : BindableBase
    {
        private readonly IUserModuleService _userModuleService;
        private readonly ICustomDialogService _commonDialogService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        #region 属性
        
        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        private ObservableCollection<UserInfo> _users = new();
        public ObservableCollection<UserInfo> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }
        
        private UserInfo? _selectedUser;
        public UserInfo? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ResetPasswordCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }
        
        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadDataAsync();
                }
            }
        }
        
        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }
        
        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }
        
        #endregion

        #region Commands
        
        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand<UserInfo> EditCommand { get; }
        public DelegateCommand<UserInfo> DeleteCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<UserInfo> ResetPasswordCommand { get; }
        public DelegateCommand<UserInfo> ToggleStatusCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        
        #endregion

        public UserManagementViewModelSimple(
            IUserModuleService userModuleService,
            ICustomDialogService commonDialogService,
            ICustomDialogService dialogService,
            IMapper mapper)
        {
            _userModuleService = userModuleService ?? throw new ArgumentNullException(nameof(userModuleService));
            _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            LoadCommand = new DelegateCommand(async () => await LoadDataAsync());
            AddCommand = new DelegateCommand(async () => await AddAsync());
            EditCommand = new DelegateCommand<UserInfo>(async user => await EditAsync(user), user => user != null);
            DeleteCommand = new DelegateCommand<UserInfo>(async user => await DeleteAsync(user), user => user != null);
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ResetPasswordCommand = new DelegateCommand<UserInfo>(async user => await ResetPasswordAsync(user), user => user != null);
            ToggleStatusCommand = new DelegateCommand<UserInfo>(async user => await ToggleStatusAsync(user), user => user != null);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);
            
            // 初始化加载数据
            _ = LoadDataAsync();
        }

        #region 数据操作方法
        
        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = CurrentPage,
                    PageSize = PageSize,
                    Keyword = SearchKeyword
                };
                
                var result = await _userModuleService.GetPagedAsync(query);
                if (result.IsSuccess)
                {
                    Users.Clear();
                    foreach (var user in result.Data.Items)
                    {
                        Users.Add(user);
                    }
                    
                    TotalCount = result.Data.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                    
                    // 更新分页命令状态
                    PreviousPageCommand.RaiseCanExecuteChanged();
                    NextPageCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "加载用户列表失败", "错误");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载用户列表异常: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 搜索用户
        /// </summary>
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }
        
        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }
        
        /// <summary>
        /// 上一页
        /// </summary>
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }
        
        /// <summary>
        /// 下一页
        /// </summary>
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }
        
        #endregion
        
        #region CRUD操作
        
        /// <summary>
        /// 新增用户
        /// </summary>
        private async Task AddAsync()
        {
            try
            {
                var createInfo = new UserCreateInfo();
                
                // 这里可以打开对话框进行用户创建
                // 暂时使用简单的实现
                await _commonDialogService.ShowInformationAsync("新增用户功能开发中", "提示");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"添加用户失败: {ex.Message}", "错误");
            }
        }
        
        /// <summary>
        /// 编辑用户
        /// </summary>
        private async Task EditAsync(UserInfo user)
        {
            if (user == null) return;
            
            try
            {
                var updateInfo = UserUpdateInfo.FromUserInfo(user);
                
                // 这里可以打开对话框进行用户编辑
                // 暂时使用简单的实现
                await _commonDialogService.ShowInformationAsync($"编辑用户 {user.RealName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"编辑用户失败: {ex.Message}", "错误");
            }
        }
        
        /// <summary>
        /// 删除用户
        /// </summary>
        private async Task DeleteAsync(UserInfo user)
        {
            if (user == null) return;
            
            // 不允许删除系统管理员账号
            if (user.Username == "admin" || user.Username == "sysadmin")
            {
                await _commonDialogService.ShowWarningAsync("不允许删除系统管理员账号", "警告");
                return;
            }
            
            // 用户不支持删除，只能禁用
            await ToggleStatusAsync(user);
        }
        
        #endregion

        #region 业务操作方法

        /// <summary>
        /// 重置密码
        /// </summary>
        private async Task ResetPasswordAsync(UserInfo user)
        {
            if (user == null) return;

            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要重置用户 {user.RealName} 的密码吗？",
                "重置密码");

            if (confirm)
            {
                var result = await _userModuleService.ResetPasswordAsync(user.Id);
                if (result.IsSuccess)
                {
                    await _commonDialogService.ShowInformationAsync(
                        $"密码重置成功！新密码: {result.Data}", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "密码重置失败",
                        "错误");
                }
            }
        }

        /// <summary>
        /// 切换用户状态
        /// </summary>
        private async Task ToggleStatusAsync(UserInfo user)
        {
            if (user == null) return;

            var action = user.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要{action}用户 {user.RealName} 吗？",
                $"{action}用户");

            if (confirm)
            {
                ServiceResult result;
                if (user.Status == CommonStatus.Enabled)
                {
                    result = await _userModuleService.DisableAsync(user.Id);
                }
                else
                {
                    result = await _userModuleService.EnableAsync(user.Id);
                }

                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync($"用户{action}成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"用户{action}失败",
                        "错误");
                }
            }
        }

        #endregion
    }
}