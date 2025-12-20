using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者编辑控件 - OpenSpec: extract-detail-controls Task 3.2
    /// 独立的患者编辑控件，可在PatientDetailView中复用
    /// </summary>
    public partial class PatientEditControl : UserControl
    {
        public PatientEditControl()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        /// <summary>
        /// 患者姓名
        /// </summary>
        public static readonly DependencyProperty PatientNameProperty =
            DependencyProperty.Register(
                nameof(PatientName),
                typeof(string),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string PatientName
        {
            get => (string)GetValue(PatientNameProperty);
            set => SetValue(PatientNameProperty, value);
        }

        /// <summary>
        /// 拼音码（可编辑，用于修正多音字等识别错误）
        /// </summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(
                nameof(PinYinCode),
                typeof(string),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string PinYinCode
        {
            get => (string)GetValue(PinYinCodeProperty);
            set => SetValue(PinYinCodeProperty, value);
        }

        /// <summary>
        /// 性别
        /// </summary>
        public static readonly DependencyProperty GenderProperty =
            DependencyProperty.Register(
                nameof(Gender),
                typeof(Gender),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(LYBT.Shared.Models.Enums.Gender.Male, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Gender Gender
        {
            get => (Gender)GetValue(GenderProperty);
            set => SetValue(GenderProperty, value);
        }

        /// <summary>
        /// 性别选项列表
        /// </summary>
        public static readonly DependencyProperty GenderOptionsProperty =
            DependencyProperty.Register(
                nameof(GenderOptions),
                typeof(ObservableCollection<Gender>),
                typeof(PatientEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<Gender>? GenderOptions
        {
            get => (ObservableCollection<Gender>?)GetValue(GenderOptionsProperty);
            set => SetValue(GenderOptionsProperty, value);
        }

        /// <summary>
        /// 出生日期
        /// </summary>
        public static readonly DependencyProperty BirthDateProperty =
            DependencyProperty.Register(
                nameof(BirthDate),
                typeof(DateTime?),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime? BirthDate
        {
            get => (DateTime?)GetValue(BirthDateProperty);
            set => SetValue(BirthDateProperty, value);
        }

        /// <summary>
        /// 年龄（只读，根据出生日期计算）
        /// </summary>
        public static readonly DependencyProperty AgeProperty =
            DependencyProperty.Register(
                nameof(Age),
                typeof(int?),
                typeof(PatientEditControl),
                new PropertyMetadata(null));

        public int? Age
        {
            get => (int?)GetValue(AgeProperty);
            set => SetValue(AgeProperty, value);
        }

        /// <summary>
        /// 身份证号
        /// </summary>
        public static readonly DependencyProperty IdNumberProperty =
            DependencyProperty.Register(
                nameof(IdNumber),
                typeof(string),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string IdNumber
        {
            get => (string)GetValue(IdNumberProperty);
            set => SetValue(IdNumberProperty, value);
        }

        /// <summary>
        /// 手机号码
        /// </summary>
        public static readonly DependencyProperty PhoneNumberProperty =
            DependencyProperty.Register(
                nameof(PhoneNumber),
                typeof(string),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string PhoneNumber
        {
            get => (string)GetValue(PhoneNumberProperty);
            set => SetValue(PhoneNumberProperty, value);
        }

        /// <summary>
        /// 地址
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(
                nameof(Address),
                typeof(string),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(CommonStatus),
                typeof(PatientEditControl),
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
                typeof(PatientEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<CommonStatus>? StatusOptions
        {
            get => (ObservableCollection<CommonStatus>?)GetValue(StatusOptionsProperty);
            set => SetValue(StatusOptionsProperty, value);
        }

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(PatientEditControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        #endregion
    }
}
