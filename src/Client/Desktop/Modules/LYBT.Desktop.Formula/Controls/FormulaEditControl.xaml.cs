using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Models.ViewModels.Base;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// 验方编辑控件
    /// OpenSpec: extract-detail-controls Task 1.2
    /// OpenSpec: unify-herb-controls-to-herbs-module - 统一使用HerbListControl编辑处方
    /// 独立的验方编辑控件，可在FormulaDetailView中复用
    /// </summary>
    public partial class FormulaEditControl : UserControl
    {
        public FormulaEditControl()
        {
            InitializeComponent();
        }

        #region 基本信息属性

        /// <summary>
        /// 验方名称
        /// </summary>
        public static readonly DependencyProperty FormulaNameProperty =
            DependencyProperty.Register(
                nameof(FormulaName),
                typeof(string),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? FormulaName
        {
            get => (string?)GetValue(FormulaNameProperty);
            set => SetValue(FormulaNameProperty, value);
        }

        /// <summary>
        /// 分类
        /// </summary>
        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register(
                nameof(Category),
                typeof(string),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? Category
        {
            get => (string?)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        /// <summary>
        /// 性味归经
        /// </summary>
        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.Register(
                nameof(Property),
                typeof(string),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? Property
        {
            get => (string?)GetValue(PropertyProperty);
            set => SetValue(PropertyProperty, value);
        }

        /// <summary>
        /// 功效 (使用FormulaEffect避免与UIElement.Effect冲突)
        /// </summary>
        public static readonly DependencyProperty FormulaEffectProperty =
            DependencyProperty.Register(
                nameof(FormulaEffect),
                typeof(string),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? FormulaEffect
        {
            get => (string?)GetValue(FormulaEffectProperty);
            set => SetValue(FormulaEffectProperty, value);
        }

        /// <summary>
        /// 用法
        /// </summary>
        public static readonly DependencyProperty UsageProperty =
            DependencyProperty.Register(
                nameof(Usage),
                typeof(string),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? Usage
        {
            get => (string?)GetValue(UsageProperty);
            set => SetValue(UsageProperty, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public static readonly DependencyProperty RemarkProperty =
            DependencyProperty.Register(
                nameof(Remark),
                typeof(string),
                typeof(FormulaEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? Remark
        {
            get => (string?)GetValue(RemarkProperty);
            set => SetValue(RemarkProperty, value);
        }

        #endregion

        #region 审计信息属性

        /// <summary>
        /// 创建时间
        /// </summary>
        public static readonly DependencyProperty CreatedAtProperty =
            DependencyProperty.Register(
                nameof(CreatedAt),
                typeof(DateTime?),
                typeof(FormulaEditControl),
                new PropertyMetadata(null));

        public DateTime? CreatedAt
        {
            get => (DateTime?)GetValue(CreatedAtProperty);
            set => SetValue(CreatedAtProperty, value);
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public static readonly DependencyProperty UpdatedAtProperty =
            DependencyProperty.Register(
                nameof(UpdatedAt),
                typeof(DateTime?),
                typeof(FormulaEditControl),
                new PropertyMetadata(null));

        public DateTime? UpdatedAt
        {
            get => (DateTime?)GetValue(UpdatedAtProperty);
            set => SetValue(UpdatedAtProperty, value);
        }

        #endregion

        #region 药材列表属性
        // OpenSpec: unify-herb-controls-to-herbs-module - 统一使用HerbListControl编辑处方

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
