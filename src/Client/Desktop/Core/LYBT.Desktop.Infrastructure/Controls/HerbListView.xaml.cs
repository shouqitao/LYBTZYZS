using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 药材列表只读预览控件
    /// OpenSpec: unify-herb-list-controls
    ///
    /// 功能：
    /// - 使用ItemsControl + UniformGrid(Columns=4)展示药材卡片
    /// - 内部复用HerbCardControl作为ItemTemplate (IsEditMode=False)
    /// - 固定为只读模式，用于预览场景
    /// - 可配置是否显示价格
    /// </summary>
    public partial class HerbListView : UserControl
    {
        public HerbListView()
        {
            InitializeComponent();

            // 监听集合变化以更新空状态显示
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateEmptyPlaceholderVisibility();
        }

        #region HerbItems - 药材列表

        public static readonly DependencyProperty HerbItemsProperty =
            DependencyProperty.Register(
                nameof(HerbItems),
                typeof(IEnumerable),
                typeof(HerbListView),
                new PropertyMetadata(null, OnHerbItemsChanged));

        public IEnumerable? HerbItems
        {
            get => (IEnumerable?)GetValue(HerbItemsProperty);
            set => SetValue(HerbItemsProperty, value);
        }

        private static void OnHerbItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbListView view)
            {
                view.UpdateEmptyPlaceholderVisibility();
            }
        }

        #endregion

        #region ShowPrice - 是否显示价格

        public static readonly DependencyProperty ShowPriceProperty =
            DependencyProperty.Register(
                nameof(ShowPrice),
                typeof(bool),
                typeof(HerbListView),
                new PropertyMetadata(false));

        public bool ShowPrice
        {
            get => (bool)GetValue(ShowPriceProperty);
            set => SetValue(ShowPriceProperty, value);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 更新空状态提示的可见性
        /// </summary>
        private void UpdateEmptyPlaceholderVisibility()
        {
            if (EmptyPlaceholder == null)
                return;

            bool hasItems = false;
            if (HerbItems != null)
            {
                var enumerator = HerbItems.GetEnumerator();
                hasItems = enumerator.MoveNext();
            }

            // 无数据时显示空状态提示
            EmptyPlaceholder.Visibility = hasItems
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        #endregion
    }
}
