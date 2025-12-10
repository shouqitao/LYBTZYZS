using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Presentation.Components
{
    /// <summary>
    /// 药材列表编辑器控件 - 封装药材卡片的列表展示
    /// OpenSpec: unify-medicalcase-view-edit-pattern - Task 2.2
    ///
    /// 功能：
    /// - 使用ItemsControl + UniformGrid(Columns=4)展示药材卡片
    /// - 内部复用HerbCardControl作为ItemTemplate
    /// - 支持编辑/只读模式切换
    /// - UI只显示药材名+剂量（不显示价格）
    /// </summary>
    public partial class HerbListEditor : UserControl
    {
        public HerbListEditor()
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
                typeof(HerbListEditor),
                new PropertyMetadata(null, OnHerbItemsChanged));

        public IEnumerable? HerbItems
        {
            get => (IEnumerable?)GetValue(HerbItemsProperty);
            set => SetValue(HerbItemsProperty, value);
        }

        private static void OnHerbItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbListEditor editor)
            {
                editor.UpdateEmptyPlaceholderVisibility();
            }
        }

        #endregion

        #region IsEditMode - 是否编辑模式

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(
                nameof(IsEditMode),
                typeof(bool),
                typeof(HerbListEditor),
                new PropertyMetadata(false, OnIsEditModeChanged));

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        private static void OnIsEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbListEditor editor)
            {
                editor.UpdateEmptyPlaceholderVisibility();
            }
        }

        #endregion

        #region DeleteHerbCommand - 删除药材命令

        public static readonly DependencyProperty DeleteHerbCommandProperty =
            DependencyProperty.Register(
                nameof(DeleteHerbCommand),
                typeof(ICommand),
                typeof(HerbListEditor),
                new PropertyMetadata(null));

        public ICommand? DeleteHerbCommand
        {
            get => (ICommand?)GetValue(DeleteHerbCommandProperty);
            set => SetValue(DeleteHerbCommandProperty, value);
        }

        #endregion

        #region DosageCompletedCommand - 剂量完成命令

        public static readonly DependencyProperty DosageCompletedCommandProperty =
            DependencyProperty.Register(
                nameof(DosageCompletedCommand),
                typeof(ICommand),
                typeof(HerbListEditor),
                new PropertyMetadata(null));

        public ICommand? DosageCompletedCommand
        {
            get => (ICommand?)GetValue(DosageCompletedCommandProperty);
            set => SetValue(DosageCompletedCommandProperty, value);
        }

        #endregion

        #region AddNewRowCommand - 添加新行命令

        public static readonly DependencyProperty AddNewRowCommandProperty =
            DependencyProperty.Register(
                nameof(AddNewRowCommand),
                typeof(ICommand),
                typeof(HerbListEditor),
                new PropertyMetadata(null));

        public ICommand? AddNewRowCommand
        {
            get => (ICommand?)GetValue(AddNewRowCommandProperty);
            set => SetValue(AddNewRowCommandProperty, value);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 更新空状态提示的可见性
        /// 仅在只读模式且无药材时显示
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

            // 只读模式且无数据时显示空状态提示
            EmptyPlaceholder.Visibility = (!IsEditMode && !hasItems)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion
    }
}
