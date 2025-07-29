using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Models.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
        private string _searchKeyword = string.Empty;
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
        public DelegateCommand<UserInfo> DeleteUserCommand { get; }
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
            set => SetProperty(ref _currentPage, value);
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
            set => SetProperty(ref _totalCount, value);
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

        public UserManagementViewModel()
        {
            Users = new ObservableCollection<UserInfo>();
            UsersView = CollectionViewSource.GetDefaultView(Users);

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            AddUserCommand = new DelegateCommand(ExecuteAddUser);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            EditUserCommand = new DelegateCommand<UserInfo>(ExecuteEditUser);
            DeleteUserCommand = new DelegateCommand<UserInfo>(ExecuteDeleteUser);
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
                // TODO: 调用API获取用户列表
                await Task.Delay(1000); // 模拟API调用

                // 模拟数据
                Users.Clear();
                for (int i = 1; i <= 50; i++)
                {
                    Users.Add(new UserInfo
                    {
                        Id = Guid.NewGuid(),
                        UserName = $"user{i:D2}",
                        RealName = $"用户{i}",
                        Role = i % 5 == 0 ? LYBT.WPF.Client.Core.Enums.UserRole.SuperAdmin : LYBT.WPF.Client.Core.Enums.UserRole.FrontDesk,
                        Email = $"user{i}@example.com",
                        PhoneNumber = $"138000000{i:D2}",
                        IsActive = i % 10 != 0,
                        CreatedTime = DateTime.Now.AddDays(-i),
                        LastLoginTime = i % 3 == 0 ? DateTime.Now.AddHours(-i) : null
                    });
                }

                TotalCount = Users.Count;
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(TotalPages));

                // 更新分页命令状态
                FirstPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                LastPageCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteSearch()
        {
            // TODO: 实现搜索逻辑
            LoadUsers();
        }

        private void ExecuteAddUser()
        {
            // TODO: 打开新增用户对话框
        }

        private void ExecuteRefresh()
        {
            LoadUsers();
        }

        private void ExecuteEditUser(UserInfo user)
        {
            if (user == null) return;
            // TODO: 打开编辑用户对话框
        }

        private void ExecuteDeleteUser(UserInfo user)
        {
            if (user == null) return;
            // TODO: 确认删除用户
        }

        private void ExecuteResetPassword(UserInfo user)
        {
            if (user == null) return;
            // TODO: 重置用户密码
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