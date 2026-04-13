using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Desktop.Models.ViewModels.Base;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材编辑控件
    /// OpenSpec: frontend-architecture-unification - 对象DP模式
    ///
    /// 可复用的药材编辑控件，通过对象DP绑定
    /// </summary>
    public partial class HerbEditControl : UserControl
    {
        public HerbEditControl()
        {
            InitializeComponent();
        }

        #region 对象属性 - 强类型编辑上下文

        /// <summary>
        /// 药材编辑上下文 (强类型对象 DP)
        /// </summary>
        public static readonly DependencyProperty HerbProperty =
            DependencyProperty.Register(
                nameof(Herb),
                typeof(HerbEditContext),
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public HerbEditContext? Herb
        {
            get => (HerbEditContext?)GetValue(HerbProperty);
            set => SetValue(HerbProperty, value);
        }

        #endregion

        #region 辅助属性

        /// <summary>状态选项列表</summary>
        public static readonly DependencyProperty StatusOptionsProperty =
            DependencyProperty.Register(
                nameof(StatusOptions),
                typeof(System.Collections.ObjectModel.ObservableCollection<LYBT.Shared.Models.Enums.CommonStatus>),
                typeof(HerbEditControl),
                new PropertyMetadata(null));

        public System.Collections.ObjectModel.ObservableCollection<LYBT.Shared.Models.Enums.CommonStatus>? StatusOptions
        {
            get => (System.Collections.ObjectModel.ObservableCollection<LYBT.Shared.Models.Enums.CommonStatus>?)GetValue(StatusOptionsProperty);
            set => SetValue(StatusOptionsProperty, value);
        }

        /// <summary>名称是否可编辑</summary>
        public static readonly DependencyProperty IsNameEditableProperty =
            DependencyProperty.Register(
                nameof(IsNameEditable),
                typeof(bool),
                typeof(HerbEditControl),
                new PropertyMetadata(true));

        public bool IsNameEditable
        {
            get => (bool)GetValue(IsNameEditableProperty);
            set => SetValue(IsNameEditableProperty, value);
        }

        /// <summary>是否显示状态字段</summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(HerbEditControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        /// <summary>
        /// 验证错误源 - 用于显示验证错误消息
        /// OpenSpec: ui-validation-framework
        /// </summary>
        public static readonly DependencyProperty ErrorsSourceProperty =
            DependencyProperty.Register(
                nameof(ErrorsSource),
                typeof(ValidationErrorsAccessor),
                typeof(HerbEditControl),
                new PropertyMetadata(null));

        public ValidationErrorsAccessor? ErrorsSource
        {
            get => (ValidationErrorsAccessor?)GetValue(ErrorsSourceProperty);
            set => SetValue(ErrorsSourceProperty, value);
        }

        #endregion
    }
}
