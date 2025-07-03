using LYBT.Common.Enums.Users;
using LYBT.Models;
using LYBT.Module.Users.Dtos;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Microsoft.VisualBasic;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 类 UserManagementViewModel 的说明
    /// </summary>
    public class UserManagementViewModel : BindableBase {
        /// <summary>
        /// 属性 Users 的说明
        /// </summary>
        public ObservableCollection<UserDto> Users { get; } = new();
        /// <summary>
        /// 属性 RoleList 的说明
        /// </summary>
        public ObservableCollection<UserRole> RoleList { get; } = new ObservableCollection<UserRole>(
            (UserRole[])Enum.GetValues(typeof(UserRole)));

        private UserDto _editingUser;
        /// <summary>
        /// 属性 EditingUser 的说明
        /// </summary>
        public UserDto EditingUser { get => _editingUser; set => SetProperty(ref _editingUser, value); }

        private UserDto _selectedUser;
        /// <summary>
        /// 属性 SelectedUser 的说明
        /// </summary>
        public UserDto SelectedUser { get => _selectedUser; set => SetProperty(ref _selectedUser, value); }

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 属性 SearchKeyword 的说明
        /// </summary>
        public string SearchKeyword { get => _searchKeyword; set => SetProperty(ref _searchKeyword, value); }

        private string _password;
        /// <summary>
        /// 属性 Password 的说明
        /// </summary>
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        private string _editModeTitle = "新增用户";
        /// <summary>
        /// 属性 EditModeTitle 的说明
        /// </summary>
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        /// <summary>
        /// 属性 AddUserCommand 的说明
        /// </summary>
        public DelegateCommand AddUserCommand { get; }
        /// <summary>
        /// 属性 SaveUserCommand 的说明
        /// </summary>
        public DelegateCommand SaveUserCommand { get; }
        /// <summary>
        /// 属性 SearchCommand 的说明
        /// </summary>
        public DelegateCommand SearchCommand { get; }
        /// <summary>
        /// 属性 DisableUserCommand 的说明
        /// </summary>
        public DelegateCommand DisableUserCommand { get; }
        /// <summary>
        /// 属性 ResetPasswordCommand 的说明
        /// </summary>
        public DelegateCommand ResetPasswordCommand { get; }

        private readonly IUserService _userService;

        public UserManagementViewModel(IUserService userService) {
            _userService = userService;
            AddUserCommand = new DelegateCommand(AddUser);
            SaveUserCommand = new DelegateCommand(async () => await SaveUser(), () => EditingUser != null).ObservesProperty(() => EditingUser);
            SearchCommand = new DelegateCommand(async () => await LoadUsers());
            DisableUserCommand = new DelegateCommand(async () => await DisableUser(), () => SelectedUser != null).ObservesProperty(() => SelectedUser);
            ResetPasswordCommand = new DelegateCommand(async () => await ResetPassword(), () => SelectedUser != null).ObservesProperty(() => SelectedUser);

            _ = LoadUsers();
        }

        /// <summary>
        /// 方法 AddUser 的说明
        /// </summary>
        public void AddUser() {
            // 新增时，右侧表单清空
            EditingUser = new UserDto {
                IsActive = true,
                Role = UserRole.DiagnosingDoctor // 默认角色，可自定义
            };
            EditModeTitle = "新增用户";
            Password = string.Empty;
        }

        /// <summary>
        /// 方法 SaveUser 的说明
        /// </summary>
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
                var createDto = new UserCreateDto {
                    UserName = EditingUser.UserName,
                    RealName = EditingUser.RealName,
                    Role = EditingUser.Role,
                    Roles = EditingUser.Roles,
                    IsActive = EditingUser.IsActive,
                    Email = EditingUser.Email,
                    PhoneNumber = EditingUser.PhoneNumber,
                    Password = Password
                };
                var ok = await _userService.AddUserAsync(createDto);
                if (!ok) {
                    MessageBox.Show("新增用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            } else {
                var editDto = new UserEditDto {
                    Id = EditingUser.Id,
                    RealName = EditingUser.RealName,
                    Role = EditingUser.Role,
                    Roles = EditingUser.Roles,
                    IsActive = EditingUser.IsActive,
                    Email = EditingUser.Email,
                    PhoneNumber = EditingUser.PhoneNumber,
                    Password = string.IsNullOrWhiteSpace(Password) ? null : Password
                };
                var ok = await _userService.UpdateUserAsync(editDto);
                if (!ok) {
                    MessageBox.Show("保存用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            // 刷新列表
            await LoadUsers();
            EditModeTitle = "编辑用户";
        }

        /// <summary>
        /// 方法 LoadUsers 的说明
        /// </summary>
        private async Task LoadUsers() {
            var list = await _userService.SearchAsync(SearchKeyword);
            Users.Clear();
            foreach (var u in list)
                Users.Add(u);
        }

        /// <summary>
        /// 方法 DisableUser 的说明
        /// </summary>
        private async Task DisableUser() {
            if (SelectedUser == null)
                return;
            if (await _userService.DisableUserAsync(SelectedUser.Id))
                await LoadUsers();
            else
                MessageBox.Show("禁用用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 方法 ResetPassword 的说明
        /// </summary>
        private async Task ResetPassword() {
            if (SelectedUser == null)
                return;
            string newPwd = Microsoft.VisualBasic.Interaction.InputBox("请输入新密码", "重置密码", "");
            if (string.IsNullOrWhiteSpace(newPwd))
                return;
            if (await _userService.ResetPasswordAsync(SelectedUser.Id, newPwd))
                MessageBox.Show("密码已重置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("重置密码失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
