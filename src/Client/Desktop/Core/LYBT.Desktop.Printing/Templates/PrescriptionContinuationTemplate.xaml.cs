using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Printing.Templates
{
    /// <summary>
    /// 处方续页打印模板 - 用于药材超过12味时的分页打印
    /// T4-S5-09: 分页支持
    /// </summary>
    public partial class PrescriptionContinuationTemplate : UserControl
    {
        public PrescriptionContinuationTemplate()
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
