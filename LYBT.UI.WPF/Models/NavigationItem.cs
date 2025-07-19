using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LYBT.UI.WPF.Models {
    /// <summary>
    /// 增强版导航菜单项数据模型
    /// </summary>
    public class NavigationItem : INotifyPropertyChanged {
        private string _displayName;
        private string _targetView;
        private string _icon;
        private string _description;
        private bool _isEnabled = true;
        private bool _isVisible = true;
        private int _sortOrder;
        private DateTime _lastAccessTime;
        private int _accessCount;
        private string _category;
        private string _toolTip;

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public NavigationItem() {
            LastAccessTime = DateTime.MinValue;
            AccessCount = 0;
        }

        /// <summary>
        /// 基础构造函数
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="targetView">目标视图</param>
        /// <param name="icon">图标</param>
        public NavigationItem(string displayName, string targetView, string icon = null) : this() {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            TargetView = targetView ?? throw new ArgumentNullException(nameof(targetView));
            Icon = icon ?? "Circle";
            ToolTip = displayName;
        }

        /// <summary>
        /// 完整构造函数
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="targetView">目标视图</param>
        /// <param name="icon">图标</param>
        /// <param name="description">描述</param>
        /// <param name="category">分类</param>
        /// <param name="sortOrder">排序</param>
        public NavigationItem(string displayName, string targetView, string icon, string description, string category = null, int sortOrder = 0) : this(displayName, targetView, icon) {
            Description = description;
            Category = category;
            SortOrder = sortOrder;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>
        /// 目标视图名称
        /// </summary>
        public string TargetView {
            get => _targetView;
            set => SetProperty(ref _targetView, value);
        }

        /// <summary>
        /// 图标名称（MaterialDesign图标）
        /// </summary>
        public string Icon {
            get => _icon;
            set => SetProperty(ref _icon, value ?? "Circle");
        }

        /// <summary>
        /// 功能描述
        /// </summary>
        public string Description {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder {
            get => _sortOrder;
            set => SetProperty(ref _sortOrder, value);
        }

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime {
            get => _lastAccessTime;
            set => SetProperty(ref _lastAccessTime, value);
        }

        /// <summary>
        /// 访问次数
        /// </summary>
        public int AccessCount {
            get => _accessCount;
            set => SetProperty(ref _accessCount, value);
        }

        /// <summary>
        /// 功能分类
        /// </summary>
        public string Category {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        /// <summary>
        /// 工具提示
        /// </summary>
        public string ToolTip {
            get => _toolTip;
            set => SetProperty(ref _toolTip, value);
        }

        /// <summary>
        /// 是否为最近访问项（基于最后访问时间）
        /// </summary>
        public bool IsRecentItem => LastAccessTime > DateTime.MinValue &&
                                   DateTime.Now.Subtract(LastAccessTime).TotalDays <= 7;

        /// <summary>
        /// 是否为热门项（基于访问次数）
        /// </summary>
        public bool IsPopularItem => AccessCount >= 5;

        /// <summary>
        /// 格式化的最后访问时间
        /// </summary>
        public string FormattedLastAccessTime {
            get {
                if (LastAccessTime == DateTime.MinValue) {
                    return "从未访问";
                }

                var timeSpan = DateTime.Now.Subtract(LastAccessTime);
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays} 天前"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours} 小时前"
                        : "刚刚访问";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 记录访问
        /// </summary>
        public void RecordAccess() {
            LastAccessTime = DateTime.Now;
            AccessCount++;
            OnPropertyChanged(nameof(FormattedLastAccessTime));
            OnPropertyChanged(nameof(IsRecentItem));
            OnPropertyChanged(nameof(IsPopularItem));
        }

        /// <summary>
        /// 重置访问统计
        /// </summary>
        public void ResetAccessStats() {
            LastAccessTime = DateTime.MinValue;
            AccessCount = 0;
            OnPropertyChanged(nameof(FormattedLastAccessTime));
            OnPropertyChanged(nameof(IsRecentItem));
            OnPropertyChanged(nameof(IsPopularItem));
        }

        /// <summary>
        /// 创建副本
        /// </summary>
        /// <returns>NavigationItem副本</returns>
        public NavigationItem Clone() {
            return new NavigationItem {
                DisplayName = this.DisplayName,
                TargetView = this.TargetView,
                Icon = this.Icon,
                Description = this.Description,
                IsEnabled = this.IsEnabled,
                IsVisible = this.IsVisible,
                SortOrder = this.SortOrder,
                LastAccessTime = this.LastAccessTime,
                AccessCount = this.AccessCount,
                Category = this.Category,
                ToolTip = this.ToolTip
            };
        }

        /// <summary>
        /// 检查是否与指定视图匹配
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <returns>是否匹配</returns>
        public bool MatchesView(string viewName) {
            return string.Equals(TargetView, viewName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查是否包含指定关键字
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <returns>是否包含</returns>
        public bool ContainsKeyword(string keyword) {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            return (DisplayName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                   (Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                   (Category?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Overrides

        public override string ToString() {
            return $"{DisplayName} ({TargetView})";
        }

        public override bool Equals(object obj) {
            if (obj is NavigationItem other) {
                return string.Equals(TargetView, other.TargetView, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode() {
            return TargetView?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
        }

        #endregion
    }
}