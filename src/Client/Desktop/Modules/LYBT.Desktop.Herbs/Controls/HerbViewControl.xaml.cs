using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材预览控件 - OpenSpec: extract-detail-controls Task 2.1
    /// 独立的药材预览控件，可在HerbDetailView和其他需要展示药材信息的地方复用
    /// </summary>
    public partial class HerbViewControl : UserControl
    {
        public HerbViewControl()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        /// <summary>
        /// 药材名称
        /// </summary>
        public static readonly DependencyProperty HerbNameProperty =
            DependencyProperty.Register(
                nameof(HerbName),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string HerbName
        {
            get => (string)GetValue(HerbNameProperty);
            set => SetValue(HerbNameProperty, value);
        }

        /// <summary>
        /// 拼音码
        /// </summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(
                nameof(PinYinCode),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string PinYinCode
        {
            get => (string)GetValue(PinYinCodeProperty);
            set => SetValue(PinYinCodeProperty, value);
        }

        /// <summary>
        /// 产地
        /// </summary>
        public static readonly DependencyProperty OriginProperty =
            DependencyProperty.Register(
                nameof(Origin),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string Origin
        {
            get => (string)GetValue(OriginProperty);
            set => SetValue(OriginProperty, value);
        }

        /// <summary>
        /// 规格
        /// </summary>
        public static readonly DependencyProperty SpecProperty =
            DependencyProperty.Register(
                nameof(Spec),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string Spec
        {
            get => (string)GetValue(SpecProperty);
            set => SetValue(SpecProperty, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(
                nameof(Unit),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        /// <summary>
        /// 零售价
        /// </summary>
        public static readonly DependencyProperty PriceProperty =
            DependencyProperty.Register(
                nameof(Price),
                typeof(decimal),
                typeof(HerbViewControl),
                new PropertyMetadata(0m));

        public decimal Price
        {
            get => (decimal)GetValue(PriceProperty);
            set => SetValue(PriceProperty, value);
        }

        /// <summary>
        /// 成本价
        /// </summary>
        public static readonly DependencyProperty CostPriceProperty =
            DependencyProperty.Register(
                nameof(CostPrice),
                typeof(decimal?),
                typeof(HerbViewControl),
                new PropertyMetadata(null));

        public decimal? CostPrice
        {
            get => (decimal?)GetValue(CostPriceProperty);
            set => SetValue(CostPriceProperty, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(object),
                typeof(HerbViewControl),
                new PropertyMetadata(null));

        public object? Status
        {
            get => GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>
        /// 功效
        /// </summary>
        public static readonly DependencyProperty HerbEffectProperty =
            DependencyProperty.Register(
                nameof(HerbEffect),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string HerbEffect
        {
            get => (string)GetValue(HerbEffectProperty);
            set => SetValue(HerbEffectProperty, value);
        }

        /// <summary>
        /// 用法用量
        /// </summary>
        public static readonly DependencyProperty UsageProperty =
            DependencyProperty.Register(
                nameof(Usage),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string Usage
        {
            get => (string)GetValue(UsageProperty);
            set => SetValue(UsageProperty, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public static readonly DependencyProperty RemarkProperty =
            DependencyProperty.Register(
                nameof(Remark),
                typeof(string),
                typeof(HerbViewControl),
                new PropertyMetadata(string.Empty));

        public string Remark
        {
            get => (string)GetValue(RemarkProperty);
            set => SetValue(RemarkProperty, value);
        }

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(HerbViewControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        #endregion
    }
}
