using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Extensions {
    /// <summary>
    /// ListBox 简化扩展附加属性
    /// </summary>
    public static class ListBoxExtensions {
        #region BindableSelectedItems 附加属性

        /// <summary>
        /// 可绑定的选中项集合附加属性
        /// </summary>
        public static readonly DependencyProperty BindableSelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItems",
                typeof(IList),
                typeof(ListBoxExtensions),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableSelectedItemsChanged));

        /// <summary>
        /// 获取可绑定的选中项集合
        /// </summary>
        public static IList? GetBindableSelectedItems(DependencyObject obj) {
            return (IList?)obj.GetValue(BindableSelectedItemsProperty);
        }

        /// <summary>
        /// 设置可绑定的选中项集合
        /// </summary>
        public static void SetBindableSelectedItems(DependencyObject obj, IList? value) {
            obj.SetValue(BindableSelectedItemsProperty, value);
        }

        /// <summary>
        /// 绑定集合变化处理
        /// </summary>
        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is not ListBox listBox)
                return;

            // 移除旧的事件处理
            listBox.SelectionChanged -= ListBox_SelectionChanged;

            // 如果有新的集合，添加事件处理并同步选择
            if (e.NewValue is IList newList) {
                UpdateListBoxSelection(listBox, newList);
                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        /// <summary>
        /// ListBox选择变化事件处理
        /// </summary>
        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is not ListBox listBox)
                return;

            var boundList = GetBindableSelectedItems(listBox);
            if (boundList == null)
                return;

            // 更新绑定的集合
            boundList.Clear();
            foreach (var item in listBox.SelectedItems) {
                boundList.Add(item);
            }
        }

        /// <summary>
        /// 更新ListBox的选中项
        /// </summary>
        private static void UpdateListBoxSelection(ListBox listBox, IList selectedItems) {
            if (listBox == null || selectedItems == null)
                return;

            // 临时移除事件处理
            listBox.SelectionChanged -= ListBox_SelectionChanged;

            try {
                listBox.SelectedItems.Clear();

                foreach (var item in selectedItems) {
                    if (listBox.Items.Contains(item)) {
                        listBox.SelectedItems.Add(item);
                    }
                }
            } finally {
                // 重新添加事件处理
                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 手动刷新ListBox选择状态
        /// </summary>
        public static void RefreshSelection(ListBox listBox) {
            var boundList = GetBindableSelectedItems(listBox);
            if (boundList != null) {
                UpdateListBoxSelection(listBox, boundList);
            }
        }

        #endregion
    }
}