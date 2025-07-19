using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 用户管理 ViewModel 的角色管理扩展
    /// </summary>
    public partial class UserManagementViewModel {
        #region 角色管理私有字段

        private ObservableCollection<UserRole>? _availableRoles;

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

            // 初始化角色相关命令
            SelectAllRolesCommand = new RelayCommand(SelectAllRoles, () => IsEditable && EditingUser != null);
            SelectNoneRolesCommand = new RelayCommand(SelectNoneRoles, () => IsEditable && EditingUser != null);
        }

        /// <summary>
        /// 初始化可用角色列表
        /// </summary>
        private void InitializeAvailableRoles() {
            var allRoles = Enum.GetValues<UserRole>();
            _availableRoles = new ObservableCollection<UserRole>(allRoles);
        }

        /// <summary>
        /// 选择所有角色
        /// </summary>
        private void SelectAllRoles() {
            if (EditingUser?.Roles == null)
                return;

            EditingUser.Roles.Clear();
            foreach (var role in AvailableRoles) {
                EditingUser.Roles.Add(role);
            }

            // 触发界面更新
            OnPropertyChanged(nameof(EditingUser));
        }

        /// <summary>
        /// 清除所有角色
        /// </summary>
        private void SelectNoneRoles() {
            if (EditingUser?.Roles == null)
                return;

            EditingUser.Roles.Clear();

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
            var attribute = (System.ComponentModel.DescriptionAttribute?)
                System.Attribute.GetCustomAttribute(field!, typeof(System.ComponentModel.DescriptionAttribute));

            return attribute?.Description ?? role.ToString();
        }

        #endregion
    }
}