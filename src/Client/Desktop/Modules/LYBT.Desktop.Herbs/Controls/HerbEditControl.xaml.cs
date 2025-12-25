using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材编辑控件 - OpenSpec: extract-detail-controls Task 2.2
    /// 独立的药材编辑控件，可在HerbDetailView中复用
    /// </summary>
    public partial class HerbEditControl : UserControl
    {
        public HerbEditControl()
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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string HerbName
        {
            get => (string)GetValue(HerbNameProperty);
            set => SetValue(HerbNameProperty, value);
        }

        /// <summary>
        /// 拼音码（可编辑，用于修正多音字等识别错误）
        /// </summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(
                nameof(PinYinCode),
                typeof(string),
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public decimal Price
        {
            get => (decimal)GetValue(PriceProperty);
            set => SetValue(PriceProperty, value);
        }

        /// <summary>
        /// 成本价（可空，非必填）
        /// </summary>
        public static readonly DependencyProperty CostPriceProperty =
            DependencyProperty.Register(
                nameof(CostPrice),
                typeof(decimal?),
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(CommonStatus),
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(CommonStatus.Enabled, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public CommonStatus Status
        {
            get => (CommonStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>
        /// 状态选项列表
        /// </summary>
        public static readonly DependencyProperty StatusOptionsProperty =
            DependencyProperty.Register(
                nameof(StatusOptions),
                typeof(ObservableCollection<CommonStatus>),
                typeof(HerbEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<CommonStatus>? StatusOptions
        {
            get => (ObservableCollection<CommonStatus>?)GetValue(StatusOptionsProperty);
            set => SetValue(StatusOptionsProperty, value);
        }

        /// <summary>
        /// 功效
        /// </summary>
        public static readonly DependencyProperty HerbEffectProperty =
            DependencyProperty.Register(
                nameof(HerbEffect),
                typeof(string),
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                typeof(HerbEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Remark
        {
            get => (string)GetValue(RemarkProperty);
            set => SetValue(RemarkProperty, value);
        }

        /// <summary>
        /// 名称是否可编辑
        /// </summary>
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

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
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

        #endregion
    }
}
