using System.Collections;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Formula.Models.Items;
using LYBT.Desktop.Models.ViewModels.Base;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// 验方编辑控件
    /// OpenSpec: extract-detail-controls Task 1.2
    /// OpenSpec: unify-herb-controls-to-herbs-module - 统一使用HerbListControl编辑处方
    /// OpenSpec: frontend-architecture-unification - 对象DP模式
    ///
    /// 可复用的验方编辑控件，通过对象DP绑定
    /// </summary>
    public partial class FormulaEditControl : UserControl
    {
        public FormulaEditControl()
        {
            InitializeComponent();
        }

        #region 对象属性 - 强类型编辑上下文

        /// <summary>
        /// 验方编辑上下文 (强类型对象 DP)
        /// </summary>
        public static readonly DependencyProperty FormulaProperty =
            DependencyProperty.Register(
                nameof(Formula),
                typeof(FormulaEditContext),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public FormulaEditContext? Formula
        {
            get => (FormulaEditContext?)GetValue(FormulaProperty);
            set => SetValue(FormulaProperty, value);
        }

        #endregion

        #region 药材列表属性

        /// <summary>
        /// 所有可用药材列表 - 用于HerbListControl药材选择
        /// </summary>
        public static readonly DependencyProperty AllHerbsProperty =
            DependencyProperty.Register(
                nameof(AllHerbs),
                typeof(IEnumerable),
                typeof(FormulaEditControl),
                new PropertyMetadata(null));

        public IEnumerable? AllHerbs
        {
            get => (IEnumerable?)GetValue(AllHerbsProperty);
            set => SetValue(AllHerbsProperty, value);
        }

        /// <summary>
        /// 药材列表 - 用于HerbListControl编辑(双向绑定)
        /// </summary>
        public static readonly DependencyProperty HerbItemsProperty =
            DependencyProperty.Register(
                nameof(HerbItems),
                typeof(IEnumerable),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public IEnumerable? HerbItems
        {
            get => (IEnumerable?)GetValue(HerbItemsProperty);
            set => SetValue(HerbItemsProperty, value);
        }

        /// <summary>
        /// 药材数量
        /// </summary>
        public static readonly DependencyProperty HerbCountProperty =
            DependencyProperty.Register(
                nameof(HerbCount),
                typeof(int),
                typeof(FormulaEditControl),
                new PropertyMetadata(0));

        public int HerbCount
        {
            get => (int)GetValue(HerbCountProperty);
            set => SetValue(HerbCountProperty, value);
        }

        #endregion

        #region 验证属性

        /// <summary>
        /// 验证错误源 - 用于显示验证错误消息
        /// OpenSpec: ui-validation-framework
        /// </summary>
        public static readonly DependencyProperty ErrorsSourceProperty =
            DependencyProperty.Register(
                nameof(ErrorsSource),
                typeof(ValidationErrorsAccessor),
                typeof(FormulaEditControl),
                new PropertyMetadata(null));

        public ValidationErrorsAccessor? ErrorsSource
        {
            get => (ValidationErrorsAccessor?)GetValue(ErrorsSourceProperty);
            set => SetValue(ErrorsSourceProperty, value);
        }

        #endregion
    }
}
