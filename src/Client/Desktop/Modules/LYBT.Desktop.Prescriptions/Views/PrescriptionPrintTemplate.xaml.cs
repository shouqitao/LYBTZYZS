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
        /// 更新诊见显示 - 合并四诊信息
        /// </summary>
        private void UpdateSymptomsDisplay(PrescriptionPrintDto dto)
        {
            var symptoms = dto.Symptoms;
            if (string.IsNullOrEmpty(symptoms))
            {
                // 如果没有专门的症状字段，尝试合并四诊信息
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(dto.Inspection)) parts.Add(dto.Inspection);
                if (!string.IsNullOrEmpty(dto.Inquiry)) parts.Add(dto.Inquiry);
                if (!string.IsNullOrEmpty(dto.Palpation)) parts.Add(dto.Palpation);
                symptoms = string.Join("，", parts);
            }
            SymptomsText.Text = symptoms ?? string.Empty;
        }
    }
}
