using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Controls
{
    /// <summary>
    /// 药材预览控件 - OpenSpec: extract-detail-controls Task 2.1
    /// 独立的药材预览控件，可在HerbDetailView和其他需要展示药材信息的地方复用
    /// OpenSpec: refactor-master-detail-layout - 详情区域UI优化
    /// </summary>
    public partial class HerbViewControl : UserControl
    {
        public HerbViewControl()
        {
            InitializeComponent();
        }

        #region 基本信息属性

        /// <summary>药材名称</summary>
        public static readonly DependencyProperty HerbNameProperty =
            DependencyProperty.Register(nameof(HerbName), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string HerbName
        {
            get => (string)GetValue(HerbNameProperty);
            set => SetValue(HerbNameProperty, value);
        }

        /// <summary>拼音码</summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(nameof(PinYinCode), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string PinYinCode
        {
            get => (string)GetValue(PinYinCodeProperty);
            set => SetValue(PinYinCodeProperty, value);
        }

        /// <summary>药材分类</summary>
        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register(nameof(Category), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Category
        {
            get => (string)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        /// <summary>药材性味</summary>
        public static readonly DependencyProperty PropertiesProperty =
            DependencyProperty.Register(nameof(Properties), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Properties
        {
            get => (string)GetValue(PropertiesProperty);
            set => SetValue(PropertiesProperty, value);
        }

        /// <summary>产地</summary>
        public static readonly DependencyProperty OriginProperty =
            DependencyProperty.Register(nameof(Origin), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Origin
        {
            get => (string)GetValue(OriginProperty);
            set => SetValue(OriginProperty, value);
        }

        /// <summary>规格</summary>
        public static readonly DependencyProperty SpecProperty =
            DependencyProperty.Register(nameof(Spec), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Spec
        {
            get => (string)GetValue(SpecProperty);
            set => SetValue(SpecProperty, value);
        }

        /// <summary>单位</summary>
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        #endregion

        #region 价格信息属性

        /// <summary>零售价</summary>
        public static readonly DependencyProperty PriceProperty =
            DependencyProperty.Register(nameof(Price), typeof(decimal), typeof(HerbViewControl), new PropertyMetadata(0m));

        public decimal Price
        {
            get => (decimal)GetValue(PriceProperty);
            set => SetValue(PriceProperty, value);
        }

        /// <summary>成本价</summary>
        public static readonly DependencyProperty CostPriceProperty =
            DependencyProperty.Register(nameof(CostPrice), typeof(decimal?), typeof(HerbViewControl), new PropertyMetadata(null));

        public decimal? CostPrice
        {
            get => (decimal?)GetValue(CostPriceProperty);
            set => SetValue(CostPriceProperty, value);
        }

        #endregion

        #region 功效用法属性

        /// <summary>功效</summary>
        public static readonly DependencyProperty HerbEffectProperty =
            DependencyProperty.Register(nameof(HerbEffect), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string HerbEffect
        {
            get => (string)GetValue(HerbEffectProperty);
            set => SetValue(HerbEffectProperty, value);
        }

        /// <summary>用法用量</summary>
        public static readonly DependencyProperty UsageProperty =
            DependencyProperty.Register(nameof(Usage), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Usage
        {
            get => (string)GetValue(UsageProperty);
            set => SetValue(UsageProperty, value);
        }

        /// <summary>备注</summary>
        public static readonly DependencyProperty RemarkProperty =
            DependencyProperty.Register(nameof(Remark), typeof(string), typeof(HerbViewControl), new PropertyMetadata(string.Empty));

        public string Remark
        {
            get => (string)GetValue(RemarkProperty);
            set => SetValue(RemarkProperty, value);
        }

        #endregion

        #region 系统信息属性

        /// <summary>状态</summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(CommonStatus), typeof(HerbViewControl), new PropertyMetadata(CommonStatus.Enabled));

        public CommonStatus Status
        {
            get => (CommonStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>是否显示状态字段</summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(nameof(ShowStatus), typeof(bool), typeof(HerbViewControl), new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        /// <summary>创建时间</summary>
        public static readonly DependencyProperty CreatedAtProperty =
            DependencyProperty.Register(nameof(CreatedAt), typeof(DateTime?), typeof(HerbViewControl), new PropertyMetadata(null));

        public DateTime? CreatedAt
        {
            get => (DateTime?)GetValue(CreatedAtProperty);
            set => SetValue(CreatedAtProperty, value);
        }

        /// <summary>更新时间</summary>
        public static readonly DependencyProperty UpdatedAtProperty =
            DependencyProperty.Register(nameof(UpdatedAt), typeof(DateTime?), typeof(HerbViewControl), new PropertyMetadata(null));

        public DateTime? UpdatedAt
        {
            get => (DateTime?)GetValue(UpdatedAtProperty);
            set => SetValue(UpdatedAtProperty, value);
        }

        #endregion
    }
}
