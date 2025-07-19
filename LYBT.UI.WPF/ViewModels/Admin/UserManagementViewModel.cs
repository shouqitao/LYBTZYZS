using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 用户管理 ViewModel
    /// </summary>
    public partial class UserManagementViewModel : INotifyPropertyChanged {
        #region 私有字段

        private UserModel? _selectedUser;
        private UserModel? _editingUser;
        private bool _isEditable;
        private string _searchKeyword = string.Empty;
        private bool _isBusy;
        private ObservableCollection<UserModel> _users = new();
        private ObservableCollection<UserRole> _availableRoles = new();

        #endregion

        #region 公共属性

        /// <summary>
        /// 用户列表
        /// </summary>
        public ObservableCollection<UserModel> Users {
            get => _users;
            set {
                _users = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 选中的用户
        /// </summary>
        public UserModel? SelectedUser {
            get => _selectedUser;
            set {
                _selectedUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEnableUser));
                OnPropertyChanged(nameof(CanDisableUser));
                OnPropertyChanged(nameof(CanCreateDoctorProfile));

                // 当选择用户时，准备编辑数据
                if (value != null) {
                    PrepareEditingUser(value);
                }
            }
        }

        /// <summary>
        /// 正在编辑的用户
        /// </summary>
        public UserModel? EditingUser {
            get => _editingUser;
            set {
                _editingUser = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool IsEditable {
            get => _isEditable;
            set {
                _isEditable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditModeTitle));
            }
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword {
            get => _searchKeyword;
            set {
                _searchKeyword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否繁忙
        /// </summary>
        public bool IsBusy {
            get => _isBusy;
            set {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 所有可选的角色列表（用于角色选择界面）
        /// </summary>
        public ObservableCollection<UserRole> AvailableRoles {
            get => _availableRoles;
            set {
                _availableRoles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 编辑模式标题
        /// </summary>
        public string EditModeTitle => IsEditable ? "编辑用户" : "用户详情";

        /// <summary>
        /// 能否启用用户
        /// </summary>
        public bool CanEnableUser => SelectedUser?.IsActive == false;

        /// <summary>
        /// 能否禁用用户
        /// </summary>
        public bool CanDisableUser => SelectedUser?.IsActive == true;

        /// <summary>
        /// 能否创建医生档案
        /// </summary>
        public bool CanCreateDoctorProfile => SelectedUser?.Roles.Contains(UserRole.DiagnosingDoctor) == true;

        #endregion

        #region 命令属性

        public ICommand AddUserCommand { get; private set; }
        public ICommand EditUserCommand { get; private set; }
        public ICommand SaveUserCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand EnableUserCommand { get; private set; }
        public ICommand DisableUserCommand { get; private set; }
        public ICommand ResetPasswordCommand { get; private set; }
        public ICommand CreateDoctorProfileCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand LoadPageCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand SelectAllRolesCommand { get; private set; }
        public ICommand SelectNoneRolesCommand { get; private set; }

        #endregion

        #region 构造函数

        public UserManagementViewModel() {
            InitializeCommands();
            InitializeRoleList();
            LoadUsersAsync();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands() {
            AddUserCommand = new RelayCommand(AddUser);
            EditUserCommand = new RelayCommand(EditUser, () => SelectedUser != null);
            SaveUserCommand = new RelayCommand(SaveUser, () => IsEditable && EditingUser != null);
            CancelCommand = new RelayCommand(CancelEdit);
            EnableUserCommand = new RelayCommand(EnableUser, () => CanEnableUser);
            DisableUserCommand = new RelayCommand(DisableUser, () => CanDisableUser);
            ResetPasswordCommand = new RelayCommand(ResetPassword, () => SelectedUser != null);
            CreateDoctorProfileCommand = new RelayCommand(CreateDoctorProfile, () => CanCreateDoctorProfile);
            SearchCommand = new RelayCommand(SearchUsers);
            LoadPageCommand = new RelayCommand<int>(LoadPage);
            RefreshCommand = new RelayCommand(RefreshUsers);
            SelectAllRolesCommand = new RelayCommand(SelectAllRoles, () => IsEditable && EditingUser != null);
            SelectNoneRolesCommand = new RelayCommand(SelectNoneRoles, () => IsEditable && EditingUser != null);
        }

        /// <summary>
        /// 初始化角色列表
        /// </summary>
        private void InitializeRoleList() {
            var allRoles = Enum.GetValues<UserRole>();
            _availableRoles = new ObservableCollection<UserRole>(allRoles);
        }

        #endregion

        #region 用户操作方法

        /// <summary>
        /// 添加用户
        /// </summary>
        private void AddUser() {
            EditingUser = new UserModel {
                Id = Guid.NewGuid(),
                CreatedTime = DateTime.Now,
                IsActive = true,
                Roles = new List<UserRole>()
            };
            IsEditable = true;
        }

        /// <summary>
        /// 编辑用户
        /// </summary>
        private void EditUser() {
            if (SelectedUser == null)
                return;

            PrepareEditingUser(SelectedUser);
            IsEditable = true;
        }

        /// <summary>
        /// 准备编辑用户数据
        /// </summary>
        /// <param name="user">要编辑的用户</param>
        private void PrepareEditingUser(UserModel user) {
            // 创建用户的副本以避免直接修改原始数据
            EditingUser = new UserModel {
                Id = user.Id,
                UserName = user.UserName,
                RealName = user.RealName,
                PinyinCode = user.PinyinCode,
                Roles = new List<UserRole>(user.Roles),
                IsActive = user.IsActive,
                CreatedTime = user.CreatedTime,
                LastLoginTime = user.LastLoginTime,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FailedLoginCount = user.FailedLoginCount,
                LockoutEnd = user.LockoutEnd,
                PasswordHash = user.PasswordHash
            };
        }

        /// <summary>
        /// 保存用户
        /// </summary>
        private async void SaveUser() {
            if (EditingUser == null)
                return;

            try {
                IsBusy = true;

                // TODO: 调用业务服务保存用户
                // await _userService.SaveUserAsync(EditingUser);

                // 更新UI中的用户列表
                var existingUser = Users.FirstOrDefault(u => u.Id == EditingUser.Id);
                if (existingUser != null) {
                    // 更新现有用户
                    var index = Users.IndexOf(existingUser);
                    Users[index] = EditingUser;
                } else {
                    // 添加新用户
                    Users.Add(EditingUser);
                }

                SelectedUser = EditingUser;
                IsEditable = false;

                // TODO: 显示成功消息
                System.Windows.MessageBox.Show("用户保存成功！", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            } catch (Exception ex) {
                // TODO: 显示错误消息
                System.Windows.MessageBox.Show($"保存用户失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void CancelEdit() {
            if (SelectedUser != null) {
                PrepareEditingUser(SelectedUser);
            } else {
                EditingUser = null;
            }
            IsEditable = false;
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        private async void EnableUser() {
            if (SelectedUser == null)
                return;

            try {
                IsBusy = true;

                // TODO: 调用业务服务启用用户
                // await _userService.EnableUserAsync(SelectedUser.Id);

                SelectedUser.IsActive = true;
                PrepareEditingUser(SelectedUser);

                System.Windows.MessageBox.Show("用户已启用！", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"启用用户失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        private async void DisableUser() {
            if (SelectedUser == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要禁用用户 '{SelectedUser.RealName}' 吗？",
                "确认禁用",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            try {
                IsBusy = true;

                // TODO: 调用业务服务禁用用户
                // await _userService.DisableUserAsync(SelectedUser.Id);

                SelectedUser.IsActive = false;
                PrepareEditingUser(SelectedUser);

                System.Windows.MessageBox.Show("用户已禁用！", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"禁用用户失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        private async void ResetPassword() {
            if (SelectedUser == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要重置用户 '{SelectedUser.RealName}' 的密码吗？",
                "确认重置",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            try {
                IsBusy = true;

                // TODO: 调用业务服务重置密码
                // var newPassword = await _userService.ResetPasswordAsync(SelectedUser.Id);
                var newPassword = "123456"; // 临时默认密码

                System.Windows.MessageBox.Show($"密码重置成功！新密码：{newPassword}", "重置成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"重置密码失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 创建医生档案
        /// </summary>
        private async void CreateDoctorProfile() {
            if (SelectedUser == null)
                return;

            try {
                IsBusy = true;

                // TODO: 调用业务服务创建医生档案
                // await _doctorService.CreateDoctorProfileAsync(SelectedUser.Id);

                System.Windows.MessageBox.Show("医生档案创建成功！", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"创建医生档案失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        #endregion

        #region 角色管理方法

        /// <summary>
        /// 选择所有角色
        /// </summary>
        private void SelectAllRoles() {
            if (EditingUser?.Roles == null)
                return;

            EditingUser.Roles.Clear();
            foreach (var role in _availableRoles) {
                EditingUser.Roles.Add(role);
            }
        }

        /// <summary>
        /// 清除所有角色
        /// </summary>
        private void SelectNoneRoles() {
            EditingUser?.Roles?.Clear();
        }

        #endregion

        #region 搜索和分页方法

        /// <summary>
        /// 搜索用户
        /// </summary>
        private async void SearchUsers() {
            try {
                IsBusy = true;

                // TODO: 调用业务服务搜索用户
                // var searchResult = await _userService.SearchUsersAsync(SearchKeyword);
                // Users = new ObservableCollection<UserModel>(searchResult);

                await Task.Delay(500); // 模拟搜索延迟
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"搜索失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 加载指定页
        /// </summary>
        /// <param name="pageNumber">页码</param>
        private async void LoadPage(int pageNumber) {
            try {
                IsBusy = true;

                // TODO: 调用业务服务加载分页数据
                // var pageResult = await _userService.GetUsersPageAsync(pageNumber);
                // Users = new ObservableCollection<UserModel>(pageResult.Items);

                await Task.Delay(300); // 模拟加载延迟
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"加载数据失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 刷新用户列表
        /// </summary>
        private async void RefreshUsers() {
            await LoadUsersAsync();
        }

        /// <summary>
        /// 加载用户列表
        /// </summary>
        private async Task LoadUsersAsync() {
            try {
                IsBusy = true;

                // TODO: 调用业务服务加载用户
                // var users = await _userService.GetAllUsersAsync();
                // Users = new ObservableCollection<UserModel>(users);

                // 临时模拟数据
                Users = new ObservableCollection<UserModel>
                {
                    new UserModel
                    {
                        Id = Guid.NewGuid(),
                        UserName = "admin",
                        RealName = "系统管理员",
                        PinyinCode = "XTGLY",
                        Roles = new List<UserRole> { UserRole.Admin },
                        IsActive = true,
                        CreatedTime = DateTime.Now.AddDays(-30),
                        LastLoginTime = DateTime.Now.AddHours(-2),
                        Email = "admin@example.com",
                        PhoneNumber = "13800138000",
                        PasswordHash = "hashed_password",
                        FailedLoginCount = 0
                    },
                    new UserModel
                    {
                        Id = Guid.NewGuid(),
                        UserName = "doctor01",
                        RealName = "张医生",
                        PinyinCode = "ZYS",
                        Roles = new List<UserRole> { UserRole.DiagnosingDoctor },
                        IsActive = true,
                        CreatedTime = DateTime.Now.AddDays(-15),
                        LastLoginTime = DateTime.Now.AddHours(-1),
                        Email = "doctor@example.com",
                        PhoneNumber = "13800138001",
                        PasswordHash = "hashed_password",
                        FailedLoginCount = 0
                    }
                };

                await Task.Delay(300); // 模拟加载延迟
            } catch (Exception ex) {
                System.Windows.MessageBox.Show($"加载用户失败：{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            } finally {
                IsBusy = false;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 简单的RelayCommand实现
    /// </summary>
    public class RelayCommand : ICommand {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null) {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter) {
            _execute();
        }
    }

    /// <summary>
    /// 带参数的RelayCommand实现
    /// </summary>
    public class RelayCommand<T> : ICommand {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null) {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) {
            return _canExecute?.Invoke((T)parameter!) ?? true;
        }

        public void Execute(object? parameter) {
            _execute((T)parameter!);
        }
    }
}