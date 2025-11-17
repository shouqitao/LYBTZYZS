using System.Windows;

namespace LYBT.Desktop.Foundation.Helpers
{
    /// <summary>
    /// 可见性辅助类 - 统一管理Visibility转换逻辑
    /// Issue #2148: 创建VisibilityHelper优化Desktop层Visibility直接使用
    /// </summary>
    public static class VisibilityHelper
    {
        /// <summary>
        /// 将布尔值转换为Visibility (true=Visible, false=Collapsed)
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        /// <returns>Visibility枚举值</returns>
        public static Visibility ToVisibility(bool isVisible)
            => isVisible ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 将布尔值转换为Visibility (true=Visible, false=Hidden)
        /// Hidden模式会保留元素占用的布局空间
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        /// <returns>Visibility枚举值</returns>
        public static Visibility ToVisibilityHidden(bool isVisible)
            => isVisible ? Visibility.Visible : Visibility.Hidden;
    }
}
