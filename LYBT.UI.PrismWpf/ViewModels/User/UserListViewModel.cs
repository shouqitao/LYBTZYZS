using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.ViewModels.User
{
    /// <summary>
    /// 用户列表ViewModel
    /// </summary>
    public class UserListViewModel : BindableBase
    {
        #region Fields

        private ObservableCollection<UserInfo> _users = new();
        private UserInfo? _selectedUser;
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;

        #endregion

        #region Properties

        /// <summary>
        /// 用户列表
        /// </summary>
        public ObservableCollection<UserInfo> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        /// <summary>
        /// 选中的用户
        /// </summary>
        public UserInfo? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 新增用户命令
        /// </summary>
        public ICommand AddUserCommand { get; private set; }

        /// <summary>
        /// 编辑用户命令
        /// </summary>
        public ICommand EditUserCommand { get; private set; }

        /// <summary>
        /// 重置密码命令
        /// </summary>
        public ICommand ResetPasswordCommand { get; private set; }

        /// <summary>
        /// 切换用户状态命令
        /// </summary>
        public ICommand ToggleActiveCommand { get; private set; }

        /// <summary>
        /// 批量启用命令
        /// </summary>
        public ICommand BatchEnableCommand { get; private set; }

        /// <summary>
        /// 批量禁用命令
        /// </summary>
        public ICommand BatchDisableCommand { get; private set; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand { get; private set; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public ICommand PreviousPageCommand { get; private set; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public ICommand NextPageCommand { get; private set; }

        #endregion

        #region Constructor

        public UserListViewModel()
        {
            InitializeCommands();
            LoadData();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            AddUserCommand = new DelegateCommand(OnAddUser);
            EditUserCommand = new DelegateCommand<UserInfo>(OnEditUser);
            ResetPasswordCommand = new DelegateCommand<UserInfo>(OnResetPassword);
            ToggleActiveCommand = new DelegateCommand<UserInfo>(OnToggleActive);
            BatchEnableCommand = new DelegateCommand(OnBatchEnable);
            BatchDisableCommand = new DelegateCommand(OnBatchDisable);
            SearchCommand = new DelegateCommand(OnSearch);
            PreviousPageCommand = new DelegateCommand(OnPreviousPage, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(OnNextPage, () => CurrentPage < TotalPages);
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private async void LoadData()
        {
            // TODO: 调用API加载用户数据
            await Task.Delay(100); // 模拟异步操作
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        private void OnAddUser()
        {
            // TODO: 打开新增用户对话框
        }

        /// <summary>
        /// 编辑用户
        /// </summary>
        private void OnEditUser(UserInfo? user)
        {
            if (user == null) return;
            // TODO: 打开编辑用户对话框
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        private void OnResetPassword(UserInfo? user)
        {
            if (user == null) return;
            // TODO: 确认对话框后调用重置密码API
        }

        /// <summary>
        /// 切换用户状态
        /// </summary>
        private void OnToggleActive(UserInfo? user)
        {
            if (user == null) return;
            // TODO: 调用启用/禁用用户API
        }

        /// <summary>
        /// 批量启用
        /// </summary>
        private void OnBatchEnable()
        {
            // TODO: 获取选中的用户，调用批量启用API
        }

        /// <summary>
        /// 批量禁用
        /// </summary>
        private void OnBatchDisable()
        {
            // TODO: 获取选中的用户，调用批量禁用API
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private void OnSearch()
        {
            CurrentPage = 1;
            LoadData();
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void OnPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadData();
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void OnNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadData();
            }
        }

        #endregion
    }
}