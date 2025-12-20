using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者预览控件 - OpenSpec: extract-detail-controls Task 3.1
    /// 独立的患者预览控件，可在PatientDetailView和其他需要展示患者信息的地方复用
    /// OpenSpec: refactor-master-detail-layout - 详情区域UI优化
    /// </summary>
    public partial class PatientViewControl : UserControl
    {
        public PatientViewControl()
        {
            InitializeComponent();
        }

        #region 基本信息属性

        /// <summary>患者姓名</summary>
        public static readonly DependencyProperty PatientNameProperty =
            DependencyProperty.Register(nameof(PatientName), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string PatientName
        {
            get => (string)GetValue(PatientNameProperty);
            set => SetValue(PatientNameProperty, value);
        }

        /// <summary>拼音码</summary>
        public static readonly DependencyProperty PinYinCodeProperty =
            DependencyProperty.Register(nameof(PinYinCode), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string PinYinCode
        {
            get => (string)GetValue(PinYinCodeProperty);
            set => SetValue(PinYinCodeProperty, value);
        }

        /// <summary>性别</summary>
        public static readonly DependencyProperty GenderProperty =
            DependencyProperty.Register(nameof(Gender), typeof(Gender), typeof(PatientViewControl), new PropertyMetadata(Gender.Male));

        public Gender Gender
        {
            get => (Gender)GetValue(GenderProperty);
            set => SetValue(GenderProperty, value);
        }

        /// <summary>出生日期</summary>
        public static readonly DependencyProperty BirthDateProperty =
            DependencyProperty.Register(nameof(BirthDate), typeof(DateTime?), typeof(PatientViewControl), new PropertyMetadata(null));

        public DateTime? BirthDate
        {
            get => (DateTime?)GetValue(BirthDateProperty);
            set => SetValue(BirthDateProperty, value);
        }

        /// <summary>年龄</summary>
        public static readonly DependencyProperty AgeProperty =
            DependencyProperty.Register(nameof(Age), typeof(int?), typeof(PatientViewControl), new PropertyMetadata(null));

        public int? Age
        {
            get => (int?)GetValue(AgeProperty);
            set => SetValue(AgeProperty, value);
        }

        /// <summary>身份证号</summary>
        public static readonly DependencyProperty IdNumberProperty =
            DependencyProperty.Register(nameof(IdNumber), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string IdNumber
        {
            get => (string)GetValue(IdNumberProperty);
            set => SetValue(IdNumberProperty, value);
        }

        /// <summary>证件类型</summary>
        public static readonly DependencyProperty IdTypeProperty =
            DependencyProperty.Register(nameof(IdType), typeof(int), typeof(PatientViewControl), new PropertyMetadata(0));

        public int IdType
        {
            get => (int)GetValue(IdTypeProperty);
            set => SetValue(IdTypeProperty, value);
        }

        /// <summary>婚姻状态</summary>
        public static readonly DependencyProperty MaritalStatusProperty =
            DependencyProperty.Register(nameof(MaritalStatus), typeof(int), typeof(PatientViewControl), new PropertyMetadata(0));

        public int MaritalStatus
        {
            get => (int)GetValue(MaritalStatusProperty);
            set => SetValue(MaritalStatusProperty, value);
        }

        /// <summary>血型</summary>
        public static readonly DependencyProperty BloodTypeProperty =
            DependencyProperty.Register(nameof(BloodType), typeof(int), typeof(PatientViewControl), new PropertyMetadata(0));

        public int BloodType
        {
            get => (int)GetValue(BloodTypeProperty);
            set => SetValue(BloodTypeProperty, value);
        }

        #endregion

        #region 联系信息属性

        /// <summary>手机号码</summary>
        public static readonly DependencyProperty PhoneNumberProperty =
            DependencyProperty.Register(nameof(PhoneNumber), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string PhoneNumber
        {
            get => (string)GetValue(PhoneNumberProperty);
            set => SetValue(PhoneNumberProperty, value);
        }

        /// <summary>地址</summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(nameof(Address), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        #endregion

        #region 紧急联系人属性

        /// <summary>紧急联系人姓名</summary>
        public static readonly DependencyProperty EmergencyContactNameProperty =
            DependencyProperty.Register(nameof(EmergencyContactName), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string EmergencyContactName
        {
            get => (string)GetValue(EmergencyContactNameProperty);
            set => SetValue(EmergencyContactNameProperty, value);
        }

        /// <summary>紧急联系人电话</summary>
        public static readonly DependencyProperty EmergencyContactPhoneProperty =
            DependencyProperty.Register(nameof(EmergencyContactPhone), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string EmergencyContactPhone
        {
            get => (string)GetValue(EmergencyContactPhoneProperty);
            set => SetValue(EmergencyContactPhoneProperty, value);
        }

        /// <summary>紧急联系人关系</summary>
        public static readonly DependencyProperty EmergencyContactRelationProperty =
            DependencyProperty.Register(nameof(EmergencyContactRelation), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string EmergencyContactRelation
        {
            get => (string)GetValue(EmergencyContactRelationProperty);
            set => SetValue(EmergencyContactRelationProperty, value);
        }

        #endregion

        #region 病史信息属性

        /// <summary>过敏史</summary>
        public static readonly DependencyProperty AllergyHistoryProperty =
            DependencyProperty.Register(nameof(AllergyHistory), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string AllergyHistory
        {
            get => (string)GetValue(AllergyHistoryProperty);
            set => SetValue(AllergyHistoryProperty, value);
        }

        /// <summary>既往病史</summary>
        public static readonly DependencyProperty MedicalHistoryProperty =
            DependencyProperty.Register(nameof(MedicalHistory), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string MedicalHistory
        {
            get => (string)GetValue(MedicalHistoryProperty);
            set => SetValue(MedicalHistoryProperty, value);
        }

        #endregion

        #region 就诊信息属性

        /// <summary>最后就诊时间</summary>
        public static readonly DependencyProperty LastVisitTimeProperty =
            DependencyProperty.Register(nameof(LastVisitTime), typeof(DateTime?), typeof(PatientViewControl), new PropertyMetadata(null));

        public DateTime? LastVisitTime
        {
            get => (DateTime?)GetValue(LastVisitTimeProperty);
            set => SetValue(LastVisitTimeProperty, value);
        }

        /// <summary>就诊次数</summary>
        public static readonly DependencyProperty VisitCountProperty =
            DependencyProperty.Register(nameof(VisitCount), typeof(int), typeof(PatientViewControl), new PropertyMetadata(0));

        public int VisitCount
        {
            get => (int)GetValue(VisitCountProperty);
            set => SetValue(VisitCountProperty, value);
        }

        #endregion

        #region 系统信息属性

        /// <summary>状态</summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(CommonStatus), typeof(PatientViewControl), new PropertyMetadata(CommonStatus.Enabled));

        public CommonStatus Status
        {
            get => (CommonStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>是否显示状态字段</summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(nameof(ShowStatus), typeof(bool), typeof(PatientViewControl), new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        /// <summary>禁用原因</summary>
        public static readonly DependencyProperty DisableReasonProperty =
            DependencyProperty.Register(nameof(DisableReason), typeof(string), typeof(PatientViewControl), new PropertyMetadata(string.Empty));

        public string DisableReason
        {
            get => (string)GetValue(DisableReasonProperty);
            set => SetValue(DisableReasonProperty, value);
        }

        /// <summary>创建时间</summary>
        public static readonly DependencyProperty CreatedAtProperty =
            DependencyProperty.Register(nameof(CreatedAt), typeof(DateTime?), typeof(PatientViewControl), new PropertyMetadata(null));

        public DateTime? CreatedAt
        {
            get => (DateTime?)GetValue(CreatedAtProperty);
            set => SetValue(CreatedAtProperty, value);
        }

        /// <summary>更新时间</summary>
        public static readonly DependencyProperty UpdatedAtProperty =
            DependencyProperty.Register(nameof(UpdatedAt), typeof(DateTime?), typeof(PatientViewControl), new PropertyMetadata(null));

        public DateTime? UpdatedAt
        {
            get => (DateTime?)GetValue(UpdatedAtProperty);
            set => SetValue(UpdatedAtProperty, value);
        }

        #endregion
    }
}
