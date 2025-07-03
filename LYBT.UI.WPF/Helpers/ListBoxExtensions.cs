using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Helpers {
    /// <summary>
    /// Provides binding support for <see cref="ListBox.SelectedItems"/>.
    /// </summary>
    public static class ListBoxExtensions {
        public static readonly DependencyProperty BindableSelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItems",
                typeof(IList),
                typeof(ListBoxExtensions),
                new PropertyMetadata(null, OnBindableSelectedItemsChanged));

        public static IList? GetBindableSelectedItems(DependencyObject obj) =>
            (IList?)obj.GetValue(BindableSelectedItemsProperty);

        public static void SetBindableSelectedItems(DependencyObject obj, IList? value) =>
            obj.SetValue(BindableSelectedItemsProperty, value);

        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ListBox lb) {
                lb.SelectionChanged -= ListBox_SelectionChanged;

                if (e.NewValue is IList list) {
                    lb.SelectedItems.Clear();
                    foreach (var item in list)
                        lb.SelectedItems.Add(item);
                }

                lb.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is ListBox lb && GetBindableSelectedItems(lb) is IList list) {
                list.Clear();
                foreach (var item in lb.SelectedItems)
                    list.Add(item);
            }
        }
    }
}
