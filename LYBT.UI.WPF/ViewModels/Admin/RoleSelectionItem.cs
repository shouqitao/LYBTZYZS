using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 角色选择项ViewModel
    /// </summary>
    public class RoleSelectionItem : INotifyPropertyChanged {
        private bool _isSelected;
        private string _name;
        private string _description;

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name {
            get => _name;
            set {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色描述
        /// </summary>
        public string Description {
            get => _description;
            set {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected {
            get => _isSelected;
            set {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色值（用于数据绑定）
        /// </summary>
        public object Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 角色选择管理器
    /// </summary>
    public class RoleSelectionManager : INotifyPropertyChanged {
        private ObservableCollection<RoleSelectionItem> _roleItems;

        public RoleSelectionManager() {
            RoleItems = new ObservableCollection<RoleSelectionItem>();
        }

        /// <summary>
        /// 角色选择项集合
        /// </summary>
        public ObservableCollection<RoleSelectionItem> RoleItems {
            get => _roleItems;
            set {
                if (_roleItems != null) {
                    // 移除旧的事件监听
                    foreach (var item in _roleItems) {
                        item.PropertyChanged -= RoleItem_PropertyChanged;
                    }
                }

                _roleItems = value;

                if (_roleItems != null) {
                    // 添加新的事件监听
                    foreach (var item in _roleItems) {
                        item.PropertyChanged += RoleItem_PropertyChanged;
                    }
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedRoles));
            }
        }

        /// <summary>
        /// 选中的角色值集合
        /// </summary>
        public ObservableCollection<object> SelectedRoles {
            get {
                if (_roleItems == null)
                    return new ObservableCollection<object>();

                return new ObservableCollection<object>(
                    _roleItems.Where(item => item.IsSelected)
                             .Select(item => item.Value));
            }
        }

        /// <summary>
        /// 设置选中的角色
        /// </summary>
        /// <param name="selectedRoles">选中的角色值集合</param>
        public void SetSelectedRoles(System.Collections.Generic.IEnumerable<object> selectedRoles) {
            if (_roleItems == null || selectedRoles == null)
                return;

            var selectedSet = new System.Collections.Generic.HashSet<object>(selectedRoles);

            foreach (var item in _roleItems) {
                item.IsSelected = selectedSet.Contains(item.Value);
            }
        }

        /// <summary>
        /// 角色选择项属性变化处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RoleItem_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(RoleSelectionItem.IsSelected)) {
                OnPropertyChanged(nameof(SelectedRoles));
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        public void SelectAll() {
            if (_roleItems == null)
                return;

            foreach (var item in _roleItems) {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// 全不选
        /// </summary>
        public void SelectNone() {
            if (_roleItems == null)
                return;

            foreach (var item in _roleItems) {
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// 反选
        /// </summary>
        public void InvertSelection() {
            if (_roleItems == null)
                return;

            foreach (var item in _roleItems) {
                item.IsSelected = !item.IsSelected;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 用户管理ViewModel的角色相关扩展
    /// </summary>
    public partial class UserManagementViewModel {
        private RoleSelectionManager _roleSelectionManager;

        /// <summary>
        /// 角色选择管理器
        /// </summary>
        public RoleSelectionManager RoleSelectionManager {
            get => _roleSelectionManager ?? (_roleSelectionManager = new RoleSelectionManager());
            set {
                _roleSelectionManager = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色列表（用于界面绑定）
        /// </summary>
        public ObservableCollection<RoleSelectionItem> RoleList => RoleSelectionManager.RoleItems;

        /// <summary>
        /// 初始化角色列表
        /// </summary>
        /// <param name="availableRoles">可用角色枚举</param>
        public void InitializeRoles<T>(System.Collections.Generic.IEnumerable<T> availableRoles) where T : System.Enum {
            var roleItems = new ObservableCollection<RoleSelectionItem>();

            foreach (var role in availableRoles) {
                roleItems.Add(new RoleSelectionItem {
                    Name = role.ToString(),
                    Description = GetEnumDescription(role),
                    Value = role,
                    IsSelected = false
                });
            }

            RoleSelectionManager.RoleItems = roleItems;
        }

        /// <summary>
        /// 获取枚举描述
        /// </summary>
        /// <param name="enumValue">枚举值</param>
        /// <returns>描述文本</returns>
        private string GetEnumDescription(System.Enum enumValue) {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = (System.ComponentModel.DescriptionAttribute)
                System.Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DescriptionAttribute));

            return attribute?.Description ?? enumValue.ToString();
        }

        /// <summary>
        /// 更新用户角色选择状态
        /// </summary>
        /// <param name="userRoles">用户拥有的角色</param>
        public void UpdateUserRoleSelection(System.Collections.Generic.IEnumerable<object> userRoles) {
            RoleSelectionManager.SetSelectedRoles(userRoles);
        }

        /// <summary>
        /// 获取用户选中的角色
        /// </summary>
        /// <returns>选中的角色集合</returns>
        public System.Collections.Generic.IEnumerable<object> GetSelectedRoles() {
            return RoleSelectionManager.SelectedRoles;
        }
    }
}