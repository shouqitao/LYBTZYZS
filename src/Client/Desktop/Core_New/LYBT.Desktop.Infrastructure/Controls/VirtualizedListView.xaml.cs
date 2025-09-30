using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{

    /// <summary>
    /// 高性能虚拟化列表视图控件 - Infrastructure简化版本
    /// </summary>
    public partial class VirtualizedListView : UserControl, INotifyPropertyChanged
    {

        public VirtualizedListView()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region 依赖属性

        /// <summary>
        /// 数据源
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(VirtualizedListView),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(VirtualizedListView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// 项目模板
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualizedListView),
                new PropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// 头部内容
        /// </summary>
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(VirtualizedListView),
                new PropertyMetadata(null));

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        /// <summary>
        /// 是否有头部
        /// </summary>
        public static readonly DependencyProperty HasHeaderProperty =
            DependencyProperty.Register(nameof(HasHeader), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool HasHeader
        {
            get => (bool)GetValue(HasHeaderProperty);
            set => SetValue(HasHeaderProperty, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// 加载文本
        /// </summary>
        public static readonly DependencyProperty LoadingTextProperty =
            DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(VirtualizedListView),
                new PropertyMetadata("正在加载数据..."));

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public static readonly DependencyProperty IsEmptyProperty =
            DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool IsEmpty
        {
            get => (bool)GetValue(IsEmptyProperty);
            set => SetValue(IsEmptyProperty, value);
        }

        /// <summary>
        /// 空数据文本
        /// </summary>
        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(VirtualizedListView),
                new PropertyMetadata("暂无数据"));

        public string EmptyText
        {
            get => (string)GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        #endregion 依赖属性

        #region 事件处理

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtualizedListView listView)
            {
                listView.UpdateEmptyState();
            }
        }

        #endregion 事件处理

        #region 私有方法

        /// <summary>
        /// 更新空状态
        /// </summary>
        private void UpdateEmptyState()
        {
            if (ItemsSource == null)
            {
                IsEmpty = true;
                return;
            }

            // 检查集合是否为空
            if (ItemsSource is ICollection collection)
            {
                IsEmpty = collection.Count == 0;
            }
            else
            {
                // 对于非Collection类型，尝试获取第一个元素
                var enumerator = ItemsSource.GetEnumerator();
                IsEmpty = !enumerator.MoveNext();
                if (enumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        #endregion 私有方法

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        #endregion INotifyPropertyChanged

        #region 公共方法

        /// <summary>
        /// 滚动到指定项目
        /// </summary>
        public void ScrollToItem(object item)
        {
            if (PART_VirtualizedListBox != null && item != null)
            {
                PART_VirtualizedListBox.ScrollIntoView(item);
            }
        }

        #endregion 公共方法
    }
}