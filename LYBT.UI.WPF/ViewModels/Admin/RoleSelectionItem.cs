using LYBT.Common.Enums.Users;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 角色选择项 ViewModel
    /// </summary>
    public class RoleSelectionItem : INotifyPropertyChanged {
        private bool _isSelected;

        /// <summary>
        /// 角色枚举值
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    OnPropertyChanged();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// 选择状态变化事件
        /// </summary>
        public event EventHandler? SelectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}