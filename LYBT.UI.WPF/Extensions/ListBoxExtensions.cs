using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.UI.WPF.Extensions {
    /// <summary>
    /// ListBox 扩展附加属性，用于支持多选绑定
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
                new PropertyMetadata(null, OnBindableSelectedItemsChanged));

        /// <summary>
        /// 获取可绑定的选中项集合
        /// </summary>
        /// <param name="obj">ListBox对象</param>
        /// <returns>选中项集合</returns>
        public static IList? GetBindableSelectedItems(DependencyObject obj) {
            return (IList?)obj.GetValue(BindableSelectedItemsProperty);
        }

        /// <summary>
        /// 设置可绑定的选中项集合
        /// </summary>
        /// <param name="obj">ListBox对象</param>
        /// <param name="value">选中项集合</param>
        public static void SetBindableSelectedItems(DependencyObject obj, IList? value) {
            obj.SetValue(BindableSelectedItemsProperty, value);
        }

        /// <summary>
        /// 可绑定选中项集合变化时的处理
        /// </summary>
        /// <param name="d">依赖对象</param>
        /// <param name="e">事件参数</param>
        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is not ListBox listBox)
                return;

            // 移除旧的事件处理
            listBox.SelectionChanged -= ListBox_SelectionChanged;

            // 添加新的事件处理
            if (e.NewValue is IList newList) {
                // 初始化ListBox选中状态
                UpdateListBoxSelection(listBox, newList);

                // 监听ListBox选择变化
                listBox.SelectionChanged += ListBox_SelectionChanged;

                // 监听集合变化（如果支持）
                if (newList is INotifyCollectionChanged newCollection) {
                    // 移除旧的集合变化监听
                    if (e.OldValue is INotifyCollectionChanged oldCollection) {
                        oldCollection.CollectionChanged -= (sender, args) => {
                            if (sender is IList senderList)
                                UpdateListBoxSelection(listBox, senderList);
                        };
                    }

                    // 添加新的集合变化监听
                    newCollection.CollectionChanged += (sender, args) => {
                        if (sender is IList senderList)
                            UpdateListBoxSelection(listBox, senderList);
                    };
                }
            }
        }

        /// <summary>
        /// ListBox选择变化处理
        /// </summary>
        /// <param name="sender">ListBox对象</param>
        /// <param name="e">选择变化事件参数</param>
        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is not ListBox listBox)
                return;

            UpdateBoundCollection(listBox);
        }

        /// <summary>
        /// 更新ListBox的选中状态
        /// </summary>
        /// <param name="listBox">ListBox控件</param>
        /// <param name="selectedItems">选中项集合</param>
        private static void UpdateListBoxSelection(ListBox listBox, IList selectedItems) {
            if (listBox == null || selectedItems == null)
                return;

            // 临时移除事件处理，避免循环触发
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

        /// <summary>
        /// 更新绑定的集合
        /// </summary>
        /// <param name="listBox">ListBox控件</param>
        private static void UpdateBoundCollection(ListBox listBox) {
            var boundList = GetBindableSelectedItems(listBox);
            if (boundList == null)
                return;

            // 临时移除集合变化事件处理，避免循环触发
            if (boundList is INotifyCollectionChanged boundCollectionToUnsubscribe) {
                boundCollectionToUnsubscribe.CollectionChanged -= (sender, args) => {
                    if (sender is IList senderList)
                        UpdateListBoxSelection(listBox, senderList);
                };
            }

            try {
                boundList.Clear();

                foreach (var item in listBox.SelectedItems) {
                    boundList.Add(item);
                }
            } finally {
                // 重新添加集合变化事件处理
                if (boundList is INotifyCollectionChanged boundCollectionToSubscribe) {
                    boundCollectionToSubscribe.CollectionChanged += (sender, args) => {
                        if (sender is IList senderList)
                            UpdateListBoxSelection(listBox, senderList);
                    };
                }
            }
        }

        #endregion
    }
}