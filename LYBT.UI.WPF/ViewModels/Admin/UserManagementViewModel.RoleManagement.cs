using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 用户管理 ViewModel 的角色管理扩展
    /// </summary>
    public partial class UserManagementViewModel {
        #region 角色管理私有字段

        private ObservableCollection<UserRole>? _availableRoles;
        private ObservableCollection<RoleSelectionItem>? _roleSelectionItems;

        #endregion

        #region 角色管理公共属性

        /// <summary>
        /// 所有可用的角色选项（用于角色选择界面）
        /// </summary>
        public ObservableCollection<UserRole> AvailableRoles {
            get {
                if (_availableRoles == null) {
                    InitializeAvailableRoles();
                }
                return _availableRoles!;
            }
            set {
                _availableRoles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色选择项列表（用于界面绑定）
        /// </summary>
        public ObservableCollection<RoleSelectionItem> RoleSelectionItems {
            get {
                if (_roleSelectionItems == null) {
                    InitializeRoleSelectionItems();
                }
                return _roleSelectionItems!;
            }
            set {
                _roleSelectionItems = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region 角色管理命令

        /// <summary>
        /// 选择所有角色命令
        /// </summary>
        public ICommand SelectAllRolesCommand { get; private set; }

        /// <summary>
        /// 清除所有角色命令
        /// </summary>
        public ICommand SelectNoneRolesCommand { get; private set; }

        #endregion

        #region 角色管理方法

        /// <summary>
        /// 角色管理功能初始化的实现
        /// </summary>
        partial void InitializeRoleManagementFeatures() {
            // 初始化角色列表
            InitializeAvailableRoles();
            InitializeRoleSelectionItems();

            // 初始化角色相关命令
            SelectAllRolesCommand = new RelayCommand(SelectAllRoles, () => IsEditable && EditingUser != null);
            SelectNoneRolesCommand = new RelayCommand(SelectNoneRoles, () => IsEditable && EditingUser != null);
        }

        /// <summary>
        /// 编辑用户变化时的处理
        /// </summary>
        partial void OnEditingUserChanged() {
            UpdateRoleSelectionItems();
        }

        /// <summary>
        /// 初始化可用角色列表
        /// </summary>
        private void InitializeAvailableRoles() {
            var allRoles = Enum.GetValues<UserRole>();
            _availableRoles = new ObservableCollection<UserRole>(allRoles);
        }

        /// <summary>
        /// 初始化角色选择项列表
        /// </summary>
        private void InitializeRoleSelectionItems() {
            _roleSelectionItems = new ObservableCollection<RoleSelectionItem>();

            foreach (var role in Enum.GetValues<UserRole>()) {
                var item = new RoleSelectionItem {
                    Role = role,
                    DisplayName = GetRoleDisplayName(role),
                    Description = GetRoleDisplayName(role),
                    IsSelected = false
                };

                // 监听选择状态变化
                item.SelectionChanged += RoleSelectionItem_SelectionChanged;

                _roleSelectionItems.Add(item);
            }
        }

        /// <summary>
        /// 角色选择项状态变化处理
        /// </summary>
        /// <param name="sender">角色选择项</param>
        /// <param name="e">事件参数</param>
        private void RoleSelectionItem_SelectionChanged(object? sender, EventArgs e) {
            if (sender is RoleSelectionItem item && EditingUser != null) {
                if (item.IsSelected) {
                    // 添加角色
                    if (!EditingUser.Roles.Contains(item.Role)) {
                        EditingUser.Roles.Add(item.Role);
                    }
                } else {
                    // 移除角色
                    EditingUser.Roles.Remove(item.Role);
                }

                // 触发界面更新
                OnPropertyChanged(nameof(EditingUser));
            }
        }

        /// <summary>
        /// 更新角色选择状态（当用户变化时调用）
        /// </summary>
        private void UpdateRoleSelectionItems() {
            if (_roleSelectionItems == null || EditingUser == null)
                return;

            foreach (var item in _roleSelectionItems) {
                // 临时移除事件监听，避免循环触发
                item.SelectionChanged -= RoleSelectionItem_SelectionChanged;

                item.IsSelected = EditingUser.Roles?.Contains(item.Role) == true;

                // 重新添加事件监听
                item.SelectionChanged += RoleSelectionItem_SelectionChanged;
            }
        }

        /// <summary>
        /// 选择所有角色
        /// </summary>
        private void SelectAllRoles() {
            if (EditingUser?.Roles == null || _roleSelectionItems == null)
                return;

            // 临时移除事件监听
            foreach (var item in _roleSelectionItems) {
                item.SelectionChanged -= RoleSelectionItem_SelectionChanged;
            }

            try {
                EditingUser.Roles.Clear();
                foreach (var item in _roleSelectionItems) {
                    item.IsSelected = true;
                    EditingUser.Roles.Add(item.Role);
                }
            } finally {
                // 重新添加事件监听
                foreach (var item in _roleSelectionItems) {
                    item.SelectionChanged += RoleSelectionItem_SelectionChanged;
                }
            }

            // 触发界面更新
            OnPropertyChanged(nameof(EditingUser));
        }

        /// <summary>
        /// 清除所有角色
        /// </summary>
        private void SelectNoneRoles() {
            if (EditingUser?.Roles == null || _roleSelectionItems == null)
                return;

            // 临时移除事件监听
            foreach (var item in _roleSelectionItems) {
                item.SelectionChanged -= RoleSelectionItem_SelectionChanged;
            }

            try {
                EditingUser.Roles.Clear();
                foreach (var item in _roleSelectionItems) {
                    item.IsSelected = false;
                }
            } finally {
                // 重新添加事件监听
                foreach (var item in _roleSelectionItems) {
                    item.SelectionChanged += RoleSelectionItem_SelectionChanged;
                }
            }

            // 触发界面更新
            OnPropertyChanged(nameof(EditingUser));
        }

        /// <summary>
        /// 检查用户是否拥有指定角色
        /// </summary>
        /// <param name="user">用户模型</param>
        /// <param name="role">角色</param>
        /// <returns>是否拥有该角色</returns>
        public bool UserHasRole(UserModel? user, UserRole role) {
            return user?.Roles?.Contains(role) == true;
        }

        /// <summary>
        /// 为用户添加角色
        /// </summary>
        /// <param name="user">用户模型</param>
        /// <param name="role">要添加的角色</param>
        public void AddRoleToUser(UserModel? user, UserRole role) {
            if (user?.Roles == null)
                return;

            if (!user.Roles.Contains(role)) {
                user.Roles.Add(role);
            }
        }

        /// <summary>
        /// 从用户中移除角色
        /// </summary>
        /// <param name="user">用户模型</param>
        /// <param name="role">要移除的角色</param>
        public void RemoveRoleFromUser(UserModel? user, UserRole role) {
            user?.Roles?.Remove(role);
        }

        /// <summary>
        /// 获取角色的显示名称
        /// </summary>
        /// <param name="role">角色枚举</param>
        /// <returns>角色的显示名称</returns>
        public string GetRoleDisplayName(UserRole role) {
            var field = role.GetType().GetField(role.ToString());
            var attribute = (DescriptionAttribute?)
                System.Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));

            return attribute?.Description ?? role.ToString();
        }

        #endregion
    }
}