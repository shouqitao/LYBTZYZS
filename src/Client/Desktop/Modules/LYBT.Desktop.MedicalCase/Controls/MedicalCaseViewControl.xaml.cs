using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Controls
{
    /// <summary>
    /// 医疗案例预览控件 - OpenSpec: extract-detail-controls Task 5.1
    /// 独立的医疗案例预览控件，可在MedicalCaseDetailView中复用
    /// </summary>
    public partial class MedicalCaseViewControl : UserControl
    {
        public MedicalCaseViewControl()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        /// <summary>
        /// 医疗案例详情对象
        /// 接收完整的MedicalCaseDetail DTO用于显示
        /// </summary>
        public static readonly DependencyProperty MedicalCaseDetailProperty =
            DependencyProperty.Register(
                nameof(MedicalCaseDetail),
                typeof(object),
                typeof(MedicalCaseViewControl),
                new PropertyMetadata(null));

        public object? MedicalCaseDetail
        {
            get => GetValue(MedicalCaseDetailProperty);
            set => SetValue(MedicalCaseDetailProperty, value);
        }

        /// <summary>
        /// 是否有诊疗记录
        /// </summary>
        public static readonly DependencyProperty HasConsultationProperty =
            DependencyProperty.Register(
                nameof(HasConsultation),
                typeof(bool),
                typeof(MedicalCaseViewControl),
                new PropertyMetadata(false));

        public bool HasConsultation
        {
            get => (bool)GetValue(HasConsultationProperty);
            set => SetValue(HasConsultationProperty, value);
        }

        /// <summary>
        /// 是否有处方
        /// </summary>
        public static readonly DependencyProperty HasPrescriptionProperty =
            DependencyProperty.Register(
                nameof(HasPrescription),
                typeof(bool),
                typeof(MedicalCaseViewControl),
                new PropertyMetadata(false));

        public bool HasPrescription
        {
            get => (bool)GetValue(HasPrescriptionProperty);
            set => SetValue(HasPrescriptionProperty, value);
        }

        #endregion
    }
}
