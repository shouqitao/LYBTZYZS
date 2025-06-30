namespace LYBT.UI.WPF.Models {
    /// <summary>
    /// 导航菜单项数据模型
    /// </summary>
    public class NavigationItem {
        public string DisplayName { get; set; }
        public string TargetView { get; set; }
        public string Icon { get; set; } // 可选：图标
        public NavigationItem(string name, string view, string icon = null) {
            DisplayName = name;
            TargetView = view;
            Icon = icon;
        }
    }
}
