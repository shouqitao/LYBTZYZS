namespace LYBT.UI.WPF.Models {
    /// <summary>
    /// 导航菜单项数据模型
    /// </summary>
    public class NavigationItem {
        /// <summary>
        /// 属性 DisplayName 的说明
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 属性 TargetView 的说明
        /// </summary>
        public string TargetView { get; set; }
        /// <summary>
        /// 属性 Icon 的说明
        /// </summary>
        public string Icon { get; set; } // 可选：图标
        public NavigationItem(string name, string view, string icon = null) {
            DisplayName = name;
            TargetView = view;
            Icon = icon;
        }
    }
}
