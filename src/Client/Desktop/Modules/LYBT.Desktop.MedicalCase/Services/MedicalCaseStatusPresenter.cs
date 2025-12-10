using System.Windows.Media;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 医案状态展示器 - 负责状态文本/颜色计算和展示
    /// OpenSpec: refactor-viewmodel-layer - Phase 5.1
    ///
    /// 职责:
    /// - 管理诊断状态 (ConsultationStatus)
    /// - 管理处方状态 (PrescriptionStatus)
    /// - 管理操作可用性 (CanPrintPrescription, CanComplete)
    /// - 提供状态更新方法
    /// </summary>
    public class MedicalCaseStatusPresenter : BindableBase
    {
        #region 颜色常量

        private static readonly Color GreenColor = Color.FromRgb(76, 175, 80);
        private static readonly Color OrangeColor = Color.FromRgb(255, 152, 0);
        private static readonly Color GrayColor = Color.FromRgb(158, 158, 158);

        #endregion

        #region 诊断状态属性

        private string _consultationStatusText = "未完成";
        /// <summary>
        /// 诊断状态文本
        /// </summary>
        public string ConsultationStatusText
        {
            get => _consultationStatusText;
            set => SetProperty(ref _consultationStatusText, value);
        }

        private Brush _consultationStatusColor = new SolidColorBrush(OrangeColor);
        /// <summary>
        /// 诊断状态颜色
        /// </summary>
        public Brush ConsultationStatusColor
        {
            get => _consultationStatusColor;
            set => SetProperty(ref _consultationStatusColor, value);
        }

        #endregion

        #region 处方状态属性

        private bool _showPrescriptionStatus;
        /// <summary>
        /// 是否显示处方状态标签
        /// </summary>
        public bool ShowPrescriptionStatus
        {
            get => _showPrescriptionStatus;
            set => SetProperty(ref _showPrescriptionStatus, value);
        }

        private string _prescriptionStatusText = "待诊断";
        /// <summary>
        /// 处方状态文本
        /// </summary>
        public string PrescriptionStatusText
        {
            get => _prescriptionStatusText;
            set => SetProperty(ref _prescriptionStatusText, value);
        }

        private Brush _prescriptionStatusBackground = new SolidColorBrush(GrayColor);
        /// <summary>
        /// 处方状态背景色
        /// </summary>
        public Brush PrescriptionStatusBackground
        {
            get => _prescriptionStatusBackground;
            set => SetProperty(ref _prescriptionStatusBackground, value);
        }

        private string _prescriptionStatusSummary = "待开方";
        /// <summary>
        /// 处方状态摘要
        /// </summary>
        public string PrescriptionStatusSummary
        {
            get => _prescriptionStatusSummary;
            set => SetProperty(ref _prescriptionStatusSummary, value);
        }

        private Brush _prescriptionStatusSummaryColor = new SolidColorBrush(GrayColor);
        /// <summary>
        /// 处方状态摘要颜色
        /// </summary>
        public Brush PrescriptionStatusSummaryColor
        {
            get => _prescriptionStatusSummaryColor;
            set => SetProperty(ref _prescriptionStatusSummaryColor, value);
        }

        #endregion

        #region 操作可用性属性

        private bool _canPrintPrescription;
        /// <summary>
        /// 是否可以打印处方
        /// </summary>
        public bool CanPrintPrescription
        {
            get => _canPrintPrescription;
            set => SetProperty(ref _canPrintPrescription, value);
        }

        private bool _canComplete;
        /// <summary>
        /// 是否可以完成看诊
        /// </summary>
        public bool CanComplete
        {
            get => _canComplete;
            set => SetProperty(ref _canComplete, value);
        }

        #endregion

        #region 状态更新方法

        /// <summary>
        /// 更新诊断状态
        /// </summary>
        /// <param name="isCompleted">是否已完成诊断</param>
        public void UpdateConsultationStatus(bool isCompleted)
        {
            if (isCompleted)
            {
                ConsultationStatusText = "已完成";
                ConsultationStatusColor = new SolidColorBrush(GreenColor);
            }
            else
            {
                ConsultationStatusText = "未完成";
                ConsultationStatusColor = new SolidColorBrush(OrangeColor);
            }
        }

        /// <summary>
        /// 更新处方状态
        /// </summary>
        /// <param name="isCompleted">是否已完成处方</param>
        /// <param name="customText">自定义状态文本（可选）</param>
        public void UpdatePrescriptionStatus(bool isCompleted, string? customText = null)
        {
            ShowPrescriptionStatus = true;

            if (isCompleted)
            {
                PrescriptionStatusText = "已完成";
                PrescriptionStatusBackground = new SolidColorBrush(GreenColor);
                PrescriptionStatusSummary = "已开方";
                PrescriptionStatusSummaryColor = new SolidColorBrush(GreenColor);
            }
            else
            {
                PrescriptionStatusText = customText ?? "待开方";
                PrescriptionStatusBackground = new SolidColorBrush(GrayColor);
                PrescriptionStatusSummary = customText ?? "待开方";
                PrescriptionStatusSummaryColor = new SolidColorBrush(GrayColor);
            }
        }

        /// <summary>
        /// 重置所有状态到初始值
        /// </summary>
        public void Reset()
        {
            ConsultationStatusText = "未完成";
            ConsultationStatusColor = new SolidColorBrush(OrangeColor);

            ShowPrescriptionStatus = false;
            PrescriptionStatusText = "待诊断";
            PrescriptionStatusBackground = new SolidColorBrush(GrayColor);
            PrescriptionStatusSummary = "待开方";
            PrescriptionStatusSummaryColor = new SolidColorBrush(GrayColor);

            CanPrintPrescription = false;
            CanComplete = false;
        }

        /// <summary>
        /// 设置诊断完成后的处方面板状态
        /// </summary>
        /// <param name="needsPrescription">是否需要开处方</param>
        public void OnConsultationCompleted(bool needsPrescription)
        {
            UpdateConsultationStatus(true);

            if (needsPrescription)
            {
                UpdatePrescriptionStatus(false, "待开方");
            }
            else
            {
                UpdatePrescriptionStatus(false, "无需开方");
                CanComplete = true;
            }
        }

        /// <summary>
        /// 设置处方完成后的状态
        /// </summary>
        public void OnPrescriptionCompleted()
        {
            UpdatePrescriptionStatus(true);
            CanPrintPrescription = true;
            CanComplete = true;
        }

        #endregion
    }
}
