using LYBT.Common.Enums.Users;
using LYBT.Models;
using LYBT.Module.Users.Models;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    public class UserManagementViewModel : BindableBase {
        public ObservableCollection<UserModel> Users { get; } = new();
        public ObservableCollection<UserRole> RoleList { get; } = new ObservableCollection<UserRole>(
            (UserRole[])Enum.GetValues(typeof(UserRole)));

        private UserModel _editingUser;
        public UserModel EditingUser { get => _editingUser; set => SetProperty(ref _editingUser, value); }

        private string _editModeTitle = "新增用户";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        public DelegateCommand AddUserCommand { get; }
        public DelegateCommand SaveUserCommand { get; }

        private readonly IUserManagementService _userService;

        public UserManagementViewModel(IUserManagementService userService) {
            _userService = userService;
            AddUserCommand = new DelegateCommand(AddUser);
            SaveUserCommand = new DelegateCommand(async () => await SaveUser(), () => EditingUser != null).ObservesProperty(() => EditingUser);

            // 加载用户列表（略）
        }

        public void AddUser() {
            // 新增时，右侧表单清空
            EditingUser = new UserModel {
                IsActive = true,
                Role = UserRole.DiagnosingDoctor // 默认角色，可自定义
            };
            EditModeTitle = "新增用户";
        }

        private async Task SaveUser() {
            if (EditingUser == null)
                return;
            if (string.IsNullOrWhiteSpace(EditingUser.UserName) ||
                string.IsNullOrWhiteSpace(EditingUser.RealName)) {
                MessageBox.Show("账号和姓名不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (EditingUser.Id == Guid.Empty) // 新增
            {
                var ok = await _userService.AddUserAsync(EditingUser);
                if (!ok) {
                    MessageBox.Show("新增用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            // 刷新列表
            await LoadUsers();
            EditModeTitle = "编辑用户";
        }

        private async Task LoadUsers() {
            var list = await _userService.GetUsersAsync();
            Users.Clear();
            foreach (var u in list)
                Users.Add(u);
        }
    }
}
