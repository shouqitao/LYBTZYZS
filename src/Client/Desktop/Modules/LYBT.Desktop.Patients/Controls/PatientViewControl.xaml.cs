using System;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者预览控件 - OpenSpec: extract-detail-controls Task 3.1
    /// 独立的患者预览控件，可在PatientDetailView和其他需要展示患者信息的地方复用
    /// </summary>
    public partial class PatientViewControl : UserControl
    {
        public PatientViewControl()
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
                typeof(PatientViewControl),
                new PropertyMetadata(string.Empty));

        public string PatientName
        {
            get => (string)GetValue(PatientNameProperty);
            set => SetValue(PatientNameProperty, value);
        }

        /// <summary>
        /// 拼音码
        /// </summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(
                nameof(PinYinCode),
                typeof(string),
                typeof(PatientViewControl),
                new PropertyMetadata(string.Empty));

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
                typeof(string),
                typeof(PatientViewControl),
                new PropertyMetadata(string.Empty));

        public string Gender
        {
            get => (string)GetValue(GenderProperty);
            set => SetValue(GenderProperty, value);
        }

        /// <summary>
        /// 出生日期
        /// </summary>
        public static readonly DependencyProperty BirthDateProperty =
            DependencyProperty.Register(
                nameof(BirthDate),
                typeof(DateTime?),
                typeof(PatientViewControl),
                new PropertyMetadata(null));

        public DateTime? BirthDate
        {
            get => (DateTime?)GetValue(BirthDateProperty);
            set => SetValue(BirthDateProperty, value);
        }

        /// <summary>
        /// 年龄
        /// </summary>
        public static readonly DependencyProperty AgeProperty =
            DependencyProperty.Register(
                nameof(Age),
                typeof(int?),
                typeof(PatientViewControl),
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
                typeof(PatientViewControl),
                new PropertyMetadata(string.Empty));

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
                typeof(PatientViewControl),
                new PropertyMetadata(string.Empty));

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
                typeof(PatientViewControl),
                new PropertyMetadata(string.Empty));

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
                typeof(object),
                typeof(PatientViewControl),
                new PropertyMetadata(null));

        public object? Status
        {
            get => GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(PatientViewControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        #endregion
    }
}
