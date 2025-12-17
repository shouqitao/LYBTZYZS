using System.Windows.Controls;
using LYBT.Desktop.Prescriptions.Models;

namespace LYBT.Desktop.Prescriptions.Views
{
    /// <summary>
    /// 处方打印模板 - 基于A5纸张的普通处方笺
    /// OpenSpec: enhance-prescription-print
    /// </summary>
    /// <remarks>
    /// 此UserControl作为打印模板，通过DataBinding绑定PrescriptionPrintDto数据。
    /// 使用FixedDocument机制转换后进行打印，确保WYSIWYG效果。
    /// </remarks>
    public partial class PrescriptionPrintTemplate : UserControl
    {
        public PrescriptionPrintTemplate()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PrescriptionPrintDto dto)
            {
                UpdateSymptomsDisplay(dto);
            }
        }

        /// <summary>
        /// 更新诊见显示 - 合并诊断信息
        /// OpenSpec: refactor-diagnosis-fields - 移除FourDiagnosis，使用PresentIllness+TongueDiagnosis+PulseDiagnosis
        /// </summary>
        private void UpdateSymptomsDisplay(PrescriptionPrintDto dto)
        {
            var symptoms = dto.Symptoms;
            if (string.IsNullOrEmpty(symptoms))
            {
                // 合并诊断信息：现病史 + 舌诊 + 脉诊
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(dto.PresentIllness)) parts.Add(dto.PresentIllness);
                if (!string.IsNullOrEmpty(dto.TongueDiagnosis)) parts.Add($"舌诊：{dto.TongueDiagnosis}");
                if (!string.IsNullOrEmpty(dto.PulseDiagnosis)) parts.Add($"脉诊：{dto.PulseDiagnosis}");
                symptoms = string.Join("，", parts);
            }
            SymptomsText.Text = symptoms ?? string.Empty;
        }
    }
}
