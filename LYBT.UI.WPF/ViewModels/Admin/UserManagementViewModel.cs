using LYBT.Common.Enums.Users;
using LYBT.Models;
using LYBT.Module.Users.Dtos;
using Prism.Commands;
using Prism.Mvvm;
using Refit;
using System.Text.Json;
using System.Linq;

using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Interfaces;


using System;
using System.Collections.Generic;
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
        public UserDto SelectedUser {
            get => _selectedUser;
            set {
                if (SetProperty(ref _selectedUser, value)) {
                    if (value != null) {
                        _ = LoadSelectedUserAsync(value);
                    }
                }
            }
        }

        private async Task LoadSelectedUserAsync(UserDto user) {
            var detail = await _userService.GetByIdAsync(user.Id) ?? user;
            // 选择用户时复制一份到编辑区，避免直接修改列表项
            EditingUser = new UserDto {
                Id = detail.Id,
                UserName = detail.UserName,
                RealName = detail.RealName,
                Roles = detail.Roles,
                IsActive = detail.IsActive,
                Email = detail.Email,
                PhoneNumber = detail.PhoneNumber
            };
            IsEditable = false;
            EditModeTitle = "用户详情";
        }

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 属性 SearchKeyword 的说明
        /// </summary>
        public string SearchKeyword { get => _searchKeyword; set => SetProperty(ref _searchKeyword, value); }


        private string _editModeTitle = "新增用户";
        /// <summary>
        /// 属性 EditModeTitle 的说明
        /// </summary>
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        private bool _isEditable;
        /// <summary>
        /// 详情区域是否可编辑
        /// </summary>
        public bool IsEditable { get => _isEditable; set => SetProperty(ref _isEditable, value); }

        /// <summary>
        /// 属性 AddUserCommand 的说明
        /// </summary>
        public DelegateCommand AddUserCommand { get; }
        /// <summary>
        /// 编辑命令
        /// </summary>
        public DelegateCommand EditUserCommand { get; }
        /// <summary>
        /// 属性 SaveUserCommand 的说明
        /// </summary>
        public DelegateCommand SaveUserCommand { get; }
        /// <summary>
        /// 取消编辑命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }
        /// <summary>
        /// 属性 SearchCommand 的说明
        /// </summary>
        public DelegateCommand SearchCommand { get; }
        /// <summary>
        /// 属性 DisableUserCommand 的说明
        /// </summary>
        public DelegateCommand DisableUserCommand { get; }
        /// <summary>
        /// 属性 EnableUserCommand 的说明
        /// </summary>
        public DelegateCommand EnableUserCommand { get; }
        /// <summary>
        /// 属性 ResetPasswordCommand 的说明
        /// </summary>
        public DelegateCommand ResetPasswordCommand { get; }
        /// <summary>
        /// 创建医生档案命令
        /// </summary>
        public DelegateCommand CreateDoctorProfileCommand { get; }

        private readonly Interfaces.IUserService _userService;
        private readonly IDoctorService _doctorService;

        public UserManagementViewModel(Interfaces.IUserService userService, IDoctorService doctorService) {
            _userService = userService;
            _doctorService = doctorService;
            AddUserCommand = new DelegateCommand(AddUser);
            EditUserCommand = new DelegateCommand(EditUser, () => SelectedUser != null).ObservesProperty(() => SelectedUser);
            SaveUserCommand = new DelegateCommand(async () => await SaveUser(), () => EditingUser != null && IsEditable)
                .ObservesProperty(() => EditingUser)
                .ObservesProperty(() => IsEditable);
            SearchCommand = new DelegateCommand(async () => await LoadUsers());
            DisableUserCommand = new DelegateCommand(async () => await DisableUser(), () => SelectedUser != null).ObservesProperty(() => SelectedUser);
            EnableUserCommand = new DelegateCommand(async () => await EnableUser(), () => SelectedUser != null).ObservesProperty(() => SelectedUser);
            ResetPasswordCommand = new DelegateCommand(async () => await ResetPassword(), () => SelectedUser != null).ObservesProperty(() => SelectedUser);
            CreateDoctorProfileCommand =
                new DelegateCommand(async () => await CreateDoctorProfileAsync(),
                                    () => SelectedUser != null)
                .ObservesProperty(() => SelectedUser);
            CancelCommand = new DelegateCommand(CancelEdit);

            _ = LoadRoles();
            _ = LoadUsers();
        }

        /// <summary>
        /// 方法 AddUser 的说明
        /// </summary>
        public void AddUser() {
            // 新增时，右侧表单清空
            EditingUser = new UserDto {
                IsActive = true,
                Roles = new List<UserRole>()
            };
            SelectedUser = null;
            EditModeTitle = "新增用户";
            IsEditable = true;
        }

        private void EditUser() {
            if (SelectedUser != null) {
                IsEditable = true;
                EditModeTitle = "编辑用户";
            }
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
            if (EditingUser.Roles == null || EditingUser.Roles.Count == 0) {
                MessageBox.Show("角色不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try {
                if (EditingUser.Id == Guid.Empty) // 新增
                {
                    var createDto = new UserCreateDto {
                        UserName = EditingUser.UserName,
                        RealName = EditingUser.RealName,
                        Roles = EditingUser.Roles,
                        IsActive = EditingUser.IsActive,
                        Email = EditingUser.Email,
                        PhoneNumber = EditingUser.PhoneNumber
                    };
                    var ok = await _userService.AddUserAsync(createDto);
                    if (!ok) {
                        MessageBox.Show("新增用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                } else {
                    var editDto = new UserDetailDto {
                        Id = EditingUser.Id,
                        RealName = EditingUser.RealName,
                        Roles = EditingUser.Roles,
                        IsActive = EditingUser.IsActive,
                        Email = EditingUser.Email,
                        PhoneNumber = EditingUser.PhoneNumber
                    };
                    var ok = await _userService.UpdateUserAsync(editDto);
                    if (!ok) {
                        MessageBox.Show("保存用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            } catch (Exception ex) {
                string msg = ex.Message;
                if (ex is ApiException apiEx && !string.IsNullOrEmpty(apiEx.Content)) {
                    try {
                        var doc = JsonDocument.Parse(apiEx.Content);
                        if (doc.RootElement.TryGetProperty("message", out var m))
                            msg = m.GetString() ?? msg;

                        else if (doc.RootElement.TryGetProperty("errors", out var errs)) {
                            var parts = errs.EnumerateObject()
                                .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                                .Where(s => !string.IsNullOrEmpty(s));
                            msg = string.Join("; ", parts);
                        }

                    } catch {
                        // ignore parse errors
                    }
                }
                MessageBox.Show($"操作失败：{msg}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // 刷新列表
            await LoadUsers();
            IsEditable = false;
            EditModeTitle = "用户详情";
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
        /// 方法 EnableUser 的说明
        /// </summary>
        private async Task EnableUser() {
            if (SelectedUser == null)
                return;
            if (await _userService.EnableUserAsync(SelectedUser.Id))
                await LoadUsers();
            else
                MessageBox.Show("启用用户失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 方法 LoadRoles 的说明
        /// </summary>
        private async Task LoadRoles() {
            var roles = await _userService.GetRolesAsync();
            RoleList.Clear();
            foreach (var r in roles)
                RoleList.Add(r);
        }

        /// <summary>
        /// 方法 ResetPassword 的说明
        /// </summary>
        private async Task ResetPassword() {
            if (SelectedUser == null)
                return;
            if (await _userService.ResetPasswordAsync(SelectedUser.Id))
                MessageBox.Show("密码已重置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("重置密码失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task CreateDoctorProfileAsync() {
            if (SelectedUser == null)
                return;
            var roles = SelectedUser.Roles ?? new List<UserRole>();
            if (!roles.Contains(UserRole.DiagnosingDoctor) && !roles.Contains(UserRole.TreatmentDoctor)) {
                MessageBox.Show("此用户不包含医生角色，无法创建医生档案", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var existing = await _doctorService.GetByUserIdAsync(SelectedUser.Id);
            if (existing != null) {
                MessageBox.Show("该用户已有医生档案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 创建档案编辑窗口并预填用户信息
            var vm = new DoctorProfileViewModel(_doctorService, null, null) {
                Doctor = new DoctorDetailDto {
                    UserId = SelectedUser.Id,
                    UserName = SelectedUser.UserName,
                    RealName = SelectedUser.RealName
                },
                Mode = ProfileMode.Create,
                IsEditable = true,
                EditModeTitle = "新增医生档案"
            };
            var win = new DoctorProfileWindow { DataContext = vm };
            vm.CancelAction = () => win.Close();
            win.ShowDialog();
        }

        private void CancelEdit() {
            if (SelectedUser != null)
                _ = LoadSelectedUserAsync(SelectedUser);
            else
                EditingUser = null;
            IsEditable = false;
            EditModeTitle = "用户详情";
        }
    }
}
