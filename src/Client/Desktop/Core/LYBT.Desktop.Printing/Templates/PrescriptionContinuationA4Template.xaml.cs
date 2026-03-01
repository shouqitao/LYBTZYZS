using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Printing.Templates
{
    /// <summary>
    /// 处方续页打印模板 - A4纸张
    /// A4: 210mm x 297mm, 边距15mm, 字号较A5放大
    /// </summary>
    public partial class PrescriptionContinuationA4Template : UserControl
    {
        public PrescriptionContinuationA4Template()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置为最后一页（显示服法、医嘱、签名、费用区域）
        /// </summary>
        public void SetAsLastPage()
        {
            UsageText.Visibility = Visibility.Visible;
            AdviceText.Visibility = Visibility.Visible;
            SeparatorLine.Visibility = Visibility.Visible;
            SignatureRow.Visibility = Visibility.Visible;
            FeeRow.Visibility = Visibility.Visible;
        }
    }
}
