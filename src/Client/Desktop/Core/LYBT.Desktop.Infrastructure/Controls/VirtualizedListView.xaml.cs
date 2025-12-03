using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>高性能虚拟化列表视图控件</summary>
    public partial class VirtualizedListView : UserControl, INotifyPropertyChanged
    {
        public VirtualizedListView() { InitializeComponent(); DataContext = this; }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(VirtualizedListView), new PropertyMetadata(null, OnItemsSourceChanged));
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(VirtualizedListView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualizedListView), new PropertyMetadata(null));
        public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(VirtualizedListView), new PropertyMetadata(null));
        public static readonly DependencyProperty HasHeaderProperty = DependencyProperty.Register(nameof(HasHeader), typeof(bool), typeof(VirtualizedListView), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(VirtualizedListView), new PropertyMetadata(false));
        public static readonly DependencyProperty LoadingTextProperty = DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(VirtualizedListView), new PropertyMetadata("正在加载数据..."));
        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(VirtualizedListView), new PropertyMetadata(false));
        public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(VirtualizedListView), new PropertyMetadata("暂无数据"));

        public IEnumerable ItemsSource { get => (IEnumerable)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
        public object SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
        public DataTemplate ItemTemplate { get => (DataTemplate)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
        public object HeaderContent { get => GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
        public bool HasHeader { get => (bool)GetValue(HasHeaderProperty); set => SetValue(HasHeaderProperty, value); }
        public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
        public string LoadingText { get => (string)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
        public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value); }
        public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is VirtualizedListView listView) listView.UpdateEmptyState(); }

        private void UpdateEmptyState()
        {
            if (ItemsSource == null) { IsEmpty = true; return; }
            if (ItemsSource is ICollection collection) { IsEmpty = collection.Count == 0; return; }
            var enumerator = ItemsSource.GetEnumerator();
            IsEmpty = !enumerator.MoveNext();
            if (enumerator is IDisposable disposable) disposable.Dispose();
        }

        public void ScrollToItem(object item) { if (PART_VirtualizedListBox != null && item != null) PART_VirtualizedListBox.ScrollIntoView(item); }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
    }
}
