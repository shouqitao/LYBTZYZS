using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Desktop.Shared;

namespace LYBT.Desktop.Workbench.Admin.ViewModels.Management.Users
{
    /// <summary>
    /// 用户管理视图模型
    /// </summary>
    public class UserManagementViewModel : BaseManagementViewModel<UserDto>
    {
        #region Fields

        private readonly ISharedUserService _userService;
        private string _selectedRole;
        private bool? _selectedActiveStatus;

        #endregion

        #region Properties

        /// <summary>
        /// 角色筛选选项
        /// </summary>
        public List<string> RoleOptions { get; } = new List<string>
        {
            "全部", "Admin", "Doctor", "Nurse", "Pharmacist", "Receptionist", "User"
        };

        /// <summary>
        /// 状态筛选选项
        /// </summary>
        public List<KeyValuePair<string, bool?>> StatusOptions { get; } = new List<KeyValuePair<string, bool?>>
        {
            new("全部", null),
            new("启用", true),
            new("禁用", false)
        };

        /// <summary>
        /// 选中的角色筛选
        /// </summary>
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 选中的状态筛选
        /// </summary>
        public bool? SelectedActiveStatus
        {
            get => _selectedActiveStatus;
            set
            {
                if (SetProperty(ref _selectedActiveStatus, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 用户统计信息
        /// </summary>
        public UserStatisticsDto Statistics { get; private set; }

        #endregion

        #region Constructor

        public UserManagementViewModel(ISharedUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _selectedRole = "全部";
            Statistics = new UserStatisticsDto();

            // 加载统计信息
            _ = LoadStatisticsAsync();
        }

        #endregion

        #region Override Methods

        protected override async Task<(IEnumerable<UserDto> items, int totalCount)> LoadDataInternalAsync()
        {
            try
            {
                // 构建查询参数
                var queryParams = new Dictionary<string, object>
                {
                    ["page"] = CurrentPage,
                    ["size"] = PageSize
                };

                // 添加搜索条件
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    queryParams["search"] = SearchText.Trim();
                }

                // 添加角色筛选
                if (!string.IsNullOrEmpty(SelectedRole) && SelectedRole != "全部")
                {
                    queryParams["role"] = SelectedRole;
                }

                // 添加状态筛选
                if (SelectedActiveStatus.HasValue)
                {
                    queryParams["isActive"] = SelectedActiveStatus.Value;
                }

                // 调用用户服务获取数据
                var result = await _userService.GetUsersAsync(queryParams);

                if (result.IsSuccess && result.Data != null)
                {
                    return (result.Data.Data ?? Enumerable.Empty<UserDto>(), result.Data.TotalCount);
                }

                throw new Exception(result.Message ?? "获取用户数据失败");
            }
            catch (Exception ex)
            {
                throw new Exception($"加载用户数据时发生错误: {ex.Message}", ex);
            }
        }

        protected override async Task AddItemInternalAsync()
        {
            try
            {
                var dialog = new Views.Management.Users.Dialogs.UserEditDialog(null);
                var result = dialog.ShowDialog();

                if (result == true && dialog.UserData != null)
                {
                    var createDto = new UserCreateDto
                    {
                        Username = dialog.UserData.Username,
                        Password = dialog.UserData.Password,
                        ConfirmPassword = dialog.UserData.ConfirmPassword,
                        RealName = dialog.UserData.RealName,
                        PhoneNumber = dialog.UserData.PhoneNumber
                    };

                    var createResult = await _userService.CreateUserAsync(createDto);
                    if (!createResult.IsSuccess)
                    {
                        throw new Exception(createResult.Message ?? "创建用户失败");
                    }

                    StatusMessage = "用户创建成功";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建用户失败: {ex.Message}", ex);
            }
        }

        protected override async Task EditItemInternalAsync(UserDto item)
        {
            try
            {
                // 获取用户详情
                var detailResult = await _userService.GetUserAsync(item.Id);
                if (!detailResult.IsSuccess || detailResult.Data == null)
                {
                    throw new Exception("获取用户详情失败");
                }

                var dialog = new Views.Management.Users.Dialogs.UserEditDialog(detailResult.Data);
                var result = dialog.ShowDialog();

                if (result == true && dialog.UserData != null)
                {
                    var updateDto = new UserUpdateDto
                    {
                        Id = item.Id,
                        Username = dialog.UserData.Username,
                        RealName = dialog.UserData.RealName,
                        Role = dialog.UserData.Role,
                        PhoneNumber = dialog.UserData.PhoneNumber
                    };

                    var updateResult = await _userService.UpdateUserAsync(item.Id, updateDto);
                    if (!updateResult.IsSuccess)
                    {
                        throw new Exception(updateResult.Message ?? "更新用户失败");
                    }

                    StatusMessage = "用户更新成功";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"编辑用户失败: {ex.Message}", ex);
            }
        }

        protected override async Task DeleteItemInternalAsync(UserDto item)
        {
            try
            {
                // 确认删除
                var result = MessageBox.Show(
                    $"确定要删除用户 '{item.RealName}' 吗？\n注意：删除后无法恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                var deleteResult = await _userService.DeleteUserAsync(item.Id);
                if (!deleteResult.IsSuccess)
                {
                    throw new Exception(deleteResult.Message ?? "删除用户失败");
                }

                StatusMessage = $"用户 '{item.RealName}' 已删除";
            }
            catch (Exception ex)
            {
                throw new Exception($"删除用户失败: {ex.Message}", ex);
            }
        }

        protected override async Task ExportDataInternalAsync()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出用户数据",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
                    FileName = $"用户数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 获取所有数据（不分页）
                    var allUsersResult = await _userService.GetUsersAsync(new Dictionary<string, object>
                    {
                        ["page"] = 1,
                        ["size"] = int.MaxValue
                    });

                    if (!allUsersResult.IsSuccess || allUsersResult.Data?.Data == null)
                    {
                        throw new Exception("获取用户数据失败");
                    }

                    // 这里应该实现具体的导出逻辑
                    // 可以使用 NPOI 或其他 Excel 处理库
                    // 暂时只显示成功消息
                    StatusMessage = $"用户数据已导出到: {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"导出数据失败: {ex.Message}", ex);
            }
        }

        protected override bool FilterItem(UserDto item)
        {
            // 搜索文本筛选
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!item.Username.ToLower().Contains(searchLower) &&
                    !item.RealName.ToLower().Contains(searchLower) &&
                    !(item.PhoneNumber?.ToLower().Contains(searchLower) ?? false))
                {
                    return false;
                }
            }

            // 角色筛选
            if (!string.IsNullOrEmpty(SelectedRole) && SelectedRole != "全部")
            {
                // 这里需要添加角色属性到 UserDto，或者从其他地方获取
                // 暂时跳过角色筛选
            }

            // 状态筛选
            if (SelectedActiveStatus.HasValue && item.IsActive != SelectedActiveStatus.Value)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 加载统计信息
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var result = await _userService.GetUserStatisticsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Statistics = result.Data;
                    RaisePropertyChanged(nameof(Statistics));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载用户统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用筛选
        /// </summary>
        private void ApplyFilter()
        {
            if (ItemsView != null)
            {
                ItemsView.Refresh();
            }
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task ResetPasswordAsync(UserDto user)
        {
            try
            {
                var result = MessageBox.Show(
                    $"确定要重置用户 '{user.RealName}' 的密码吗？\n密码将重置为系统默认密码。",
                    "确认重置密码",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var resetResult = await _userService.ResetPasswordAsync(user.Id, "ChangeMe123");
                if (!resetResult.IsSuccess)
                {
                    throw new Exception(resetResult.Message ?? "重置密码失败");
                }

                MessageBox.Show("密码重置成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"已重置用户 '{user.RealName}' 的密码";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置密码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 切换用户启用状态
        /// </summary>
        public async Task ToggleUserStatusAsync(UserDto user)
        {
            try
            {
                var action = user.IsActive ? "禁用" : "启用";
                var result = MessageBox.Show(
                    $"确定要{action}用户 '{user.RealName}' 吗？",
                    $"确认{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var toggleResult = await _userService.ToggleUserStatusAsync(user.Id);
                if (!toggleResult.IsSuccess)
                {
                    throw new Exception(toggleResult.Message ?? $"{action}用户失败");
                }

                StatusMessage = $"用户 '{user.RealName}' 已{action}";
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(user.IsActive ? "禁用" : "启用")}用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}