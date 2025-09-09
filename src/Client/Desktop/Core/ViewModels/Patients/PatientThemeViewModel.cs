using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Patients
{

    /// <summary>
    /// 患者主题样式视图模型 - UltraThink架构的主题层
    /// 负责患者显示的颜色、样式等主题相关属性
    /// </summary>
    public class PatientThemeViewModel : BindableBase
    {

        #region Fields

        private PatientDto _patientData;

        #endregion Fields

        #region Constructor

        public PatientThemeViewModel(PatientDto patientData)
        {
            _patientData = patientData;
        }

        #endregion Constructor

        #region Color Properties

        /// <summary>状态颜色</summary>
        public Brush StatusColor => _patientData.Status switch
        {
            CommonStatus.Enabled => Brushes.Green,
            CommonStatus.Disabled => Brushes.Gray,
            _ => Brushes.Orange
        };

        /// <summary>性别颜色</summary>
        public Brush GenderColor => _patientData.Gender switch
        {
            Gender.Male => Brushes.LightBlue,
            Gender.Female => Brushes.Pink,
            _ => Brushes.LightGray
        };

        /// <summary>过敏警告颜色</summary>
        public Brush AllergyColor => string.IsNullOrWhiteSpace(_patientData.AllergyHistory) ?
            Brushes.Transparent : Brushes.Orange;

        /// <summary>背景颜色</summary>
        public Brush BackgroundColor => _patientData.Status switch
        {
            CommonStatus.Enabled => Brushes.White,
            CommonStatus.Disabled => new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            _ => Brushes.LightYellow
        };

        /// <summary>边框颜色</summary>
        public Brush BorderColor => _patientData.Status switch
        {
            CommonStatus.Enabled => Brushes.LightGray,
            CommonStatus.Disabled => Brushes.Gray,
            _ => Brushes.Orange
        };

        #endregion Color Properties

        #region Style Properties

        /// <summary>状态图标</summary>
        public string StatusIcon => _patientData.Status switch
        {
            CommonStatus.Enabled => "✓",
            CommonStatus.Disabled => "✗",
            _ => "?"
        };

        /// <summary>性别图标</summary>
        public string GenderIcon => _patientData.Gender switch
        {
            Gender.Male => "♂",
            Gender.Female => "♀",
            _ => "?"
        };

        /// <summary>过敏警告图标</summary>
        public string AllergyIcon => string.IsNullOrWhiteSpace(_patientData.AllergyHistory) ? string.Empty : "⚠";

        /// <summary>年龄分组样式名</summary>
        public string AgeGroupStyle => _patientData.Age switch
        {
            <= 18 => "Child",
            <= 60 => "Adult",
            _ => "Elder"
        };

        /// <summary>状态样式名</summary>
        public string StatusStyle => _patientData.Status switch
        {
            CommonStatus.Enabled => "Normal",
            CommonStatus.Disabled => "Disabled",
            _ => "Unknown"
        };

        #endregion Style Properties

        #region Opacity Properties

        /// <summary>整体透明度</summary>
        public double Opacity => _patientData.Status switch
        {
            CommonStatus.Enabled => 1.0,
            CommonStatus.Disabled => 0.6,
            _ => 0.8
        };

        /// <summary>文本透明度</summary>
        public double TextOpacity => _patientData.Status switch
        {
            CommonStatus.Enabled => 1.0,
            CommonStatus.Disabled => 0.5,
            _ => 0.7
        };

        #endregion Opacity Properties

        #region Update Methods

        /// <summary>
        /// 更新患者数据
        /// </summary>
        public void UpdatePatientData(PatientDto newPatientData)
        {
            _patientData = newPatientData;

            // 通知所有主题属性变化
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(GenderColor));
            RaisePropertyChanged(nameof(AllergyColor));
            RaisePropertyChanged(nameof(BackgroundColor));
            RaisePropertyChanged(nameof(BorderColor));

            RaisePropertyChanged(nameof(StatusIcon));
            RaisePropertyChanged(nameof(GenderIcon));
            RaisePropertyChanged(nameof(AllergyIcon));
            RaisePropertyChanged(nameof(AgeGroupStyle));
            RaisePropertyChanged(nameof(StatusStyle));

            RaisePropertyChanged(nameof(Opacity));
            RaisePropertyChanged(nameof(TextOpacity));
        }

        #endregion Update Methods
    }
}
