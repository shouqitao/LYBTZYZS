using System.Windows;
using System.Windows.Controls;

namespace LYBT.WPF.Client.Controls.Base
{
    /// <summary>
    /// 所有DTO展示控件的基类
    /// </summary>
    /// <typeparam name="TDto">DTO类型</typeparam>
    public abstract class BaseDisplayControl<TDto> : UserControl
        where TDto : class
    {
        /// <summary>
        /// 数据依赖属性
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(TDto),
                typeof(BaseDisplayControl<TDto>),
                new PropertyMetadata(null, OnDataChanged));

        /// <summary>
        /// 获取或设置数据
        /// </summary>
        public TDto Data
        {
            get => (TDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <summary>
        /// 数据变更时的处理
        /// </summary>
        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseDisplayControl<TDto> control)
            {
                control.DataContext = e.NewValue;
                control.OnDataChanged(e.OldValue as TDto, e.NewValue as TDto);
            }
        }

        /// <summary>
        /// 数据变更时的虚方法，子类可重写
        /// </summary>
        protected virtual void OnDataChanged(TDto oldValue, TDto newValue)
        {
            // 子类可以重写此方法以执行额外的逻辑
        }

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(
                nameof(IsEditMode),
                typeof(bool),
                typeof(BaseDisplayControl<TDto>),
                new PropertyMetadata(false, OnIsEditModeChanged));

        /// <summary>
        /// 获取或设置是否处于编辑模式
        /// </summary>
        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        /// <summary>
        /// 编辑模式变更时的处理
        /// </summary>
        private static void OnIsEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseDisplayControl<TDto> control)
            {
                control.OnIsEditModeChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        /// <summary>
        /// 编辑模式变更时的虚方法，子类可重写
        /// </summary>
        protected virtual void OnIsEditModeChanged(bool oldValue, bool newValue)
        {
            // 子类可以重写此方法以切换展示/编辑模式
        }

        /// <summary>
        /// 显示模式
        /// </summary>
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(
                nameof(DisplayMode),
                typeof(DisplayMode),
                typeof(BaseDisplayControl<TDto>),
                new PropertyMetadata(DisplayMode.Default));

        /// <summary>
        /// 获取或设置显示模式
        /// </summary>
        public DisplayMode DisplayMode
        {
            get => (DisplayMode)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }
    }

    /// <summary>
    /// 通用显示模式枚举
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>默认模式</summary>
        Default,
        /// <summary>紧凑模式</summary>
        Compact,
        /// <summary>详细模式</summary>
        Detailed,
        /// <summary>列表模式</summary>
        List,
        /// <summary>卡片模式</summary>
        Card,
        /// <summary>表格行模式</summary>
        TableRow
    }
}