using LYBT.WPF.Client.Core.Models.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Enums;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;

namespace LYBT.WPF.Client.Modules.SystemManagement.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型
    /// </summary>
    public class UserManagementViewModel : BindableBase
    {
        private readonly IUserService _userService;
        
        private string _searchKeyword = string.Empty;
        private string _searchType = "all"; // 默认全部搜索
        private UserInfo _selectedUser;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public ObservableCollection<UserInfo> Users { get; }
        public ICollectionView UsersView { get; }

        // Commands
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddUserCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<UserInfo> EditUserCommand { get; }
        public DelegateCommand<UserInfo> ToggleUserStatusCommand { get; }
        public DelegateCommand<UserInfo> ResetPasswordCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }
        
        /// <summary>搜索类型</summary>
        public string SearchType
        {
            get => _searchType;
            set => SetProperty(ref _searchType, value);
        }

        /// <summary>选中的用户</summary>
        public UserInfo SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanGoFirstPage));
                    RaisePropertyChanged(nameof(CanGoPreviousPage));
                    RaisePropertyChanged(nameof(CanGoNextPage));
                    RaisePropertyChanged(nameof(CanGoLastPage));
                    
                    // 更新命令状态
                    FirstPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                    NextPageCommand?.RaiseCanExecuteChanged();
                    LastPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(TotalPages));
                    RaisePropertyChanged(nameof(CanGoFirstPage));
                    RaisePropertyChanged(nameof(CanGoPreviousPage));
                    RaisePropertyChanged(nameof(CanGoNextPage));
                    RaisePropertyChanged(nameof(CanGoLastPage));
                    
                    // 更新命令状态
                    FirstPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                    NextPageCommand?.RaiseCanExecuteChanged();
                    LastPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {TotalCount} 条记录，第 {CurrentPage} 页，共 {TotalPages} 页";

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>是否可以跳转到第一页</summary>
        public bool CanGoFirstPage => CurrentPage > 1;

        /// <summary>是否可以跳转到上一页</summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>是否可以跳转到下一页</summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>是否可以跳转到最后一页</summary>
        public bool CanGoLastPage => CurrentPage < TotalPages;

        public UserManagementViewModel(IUserService userService)
        {
            _userService = userService;
            
            Users = new ObservableCollection<UserInfo>();
            UsersView = CollectionViewSource.GetDefaultView(Users);

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            AddUserCommand = new DelegateCommand(ExecuteAddUser);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            EditUserCommand = new DelegateCommand<UserInfo>(ExecuteEditUser);
            ToggleUserStatusCommand = new DelegateCommand<UserInfo>(ExecuteToggleUserStatus);
            ResetPasswordCommand = new DelegateCommand<UserInfo>(ExecuteResetPassword);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, CanExecuteLastPage);

            // 加载初始数据
            LoadUsers();
        }

        private async void LoadUsers()
        {
            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始加载用户列表，搜索关键词: '{SearchKeyword}', 搜索类型: '{SearchType}', 页码: {CurrentPage}");
                
                var request = new UserPagedQueryDto
                {
                    CurrentPage = CurrentPage,
                    PageSize = PageSize
                };

                // 根据搜索类型设置查询参数
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    switch (SearchType)
                    {
                        case "all":
                            request.SearchKeyword = SearchKeyword;
                            break;
                        case "username":
                            request.Username = SearchKeyword;
                            break;
                        case "realname":
                            request.RealName = SearchKeyword;
                            break;
                        case "pinyin":
                            request.PinyinCode = SearchKeyword;
                            break;
                        default:
                            request.SearchKeyword = SearchKeyword;
                            break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"发送请求: SearchKeyword={request.SearchKeyword}, Username={request.Username}, RealName={request.RealName}, Page={request.CurrentPage}, PageSize={request.PageSize}");
                
                var result = await _userService.SearchUsersAsync(request);

                Users.Clear();
                foreach (var user in result.Items)
                {
                    Users.Add(user);
                }

                TotalCount = result.TotalCount;
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(TotalPages));

                // 更新分页命令状态
                FirstPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                LastPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载用户列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteSearch()
        {
            System.Diagnostics.Debug.WriteLine($"执行搜索，关键词: '{SearchKeyword}'");
            CurrentPage = 1; // 搜索时重置到第一页
            LoadUsers();
        }

        private void ExecuteAddUser()
        {
            try
            {
                var dialog = new Views.UserAddEditDialog();
                var viewModel = new UserAddEditDialogViewModel(_userService);
                dialog.DataContext = viewModel;
                dialog.Owner = Application.Current.MainWindow;
                
                var result = dialog.ShowDialog();
                if (result == true)
                {
                    LoadUsers(); // 刷新列表
                    MessageBox.Show("用户创建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开新增用户对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteRefresh()
        {
            LoadUsers();
        }

        private void ExecuteEditUser(UserInfo user)
        {
            if (user == null) return;
            
            try
            {
                var dialog = new Views.UserAddEditDialog();
                var viewModel = new UserAddEditDialogViewModel(_userService, user);
                dialog.DataContext = viewModel;
                dialog.Owner = Application.Current.MainWindow;
                
                var result = dialog.ShowDialog();
                if (result == true)
                {
                    LoadUsers(); // 刷新列表
                    MessageBox.Show("用户信息更新成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑用户对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteToggleUserStatus(UserInfo user)
        {
            if (user == null) return;
            
            var action = user.IsActive ? "禁用" : "启用";
            var result = MessageBox.Show($"确定要{action}用户 '{user.RealName}' 吗？", $"确认{action}", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ApiResponse<object> response;
                    if (user.IsActive)
                    {
                        response = await _userService.DisableUserAsync(user.Id);
                    }
                    else
                    {
                        response = await _userService.EnableUserAsync(user.Id);
                    }
                    
                    if (response.IsSuccess)
                    {
                        MessageBox.Show($"用户已{action}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers(); // 刷新列表
                    }
                    else
                    {
                        MessageBox.Show($"{action}用户失败: {response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{action}用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExecuteResetPassword(UserInfo user)
        {
            if (user == null) return;
            
            var result = MessageBox.Show($"确定要重置用户 '{user.RealName}' 的密码吗？", "确认重置", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _userService.ResetPasswordAsync(user.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("密码重置成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"重置密码失败: {response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"重置密码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
            LoadUsers();
        }

        private bool CanExecuteFirstPage()
        {
            return CurrentPage > 1;
        }

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadUsers();
            }
        }

        private bool CanExecutePreviousPage()
        {
            return CurrentPage > 1;
        }

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadUsers();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            LoadUsers();
        }

        private bool CanExecuteLastPage()
        {
            return CurrentPage < TotalPages;
        }
    }
}