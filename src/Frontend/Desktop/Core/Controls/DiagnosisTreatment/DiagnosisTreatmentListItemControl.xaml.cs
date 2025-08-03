using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;

namespace LYBT.WPF.Client.Controls.DiagnosisTreatment
{
    /// <summary>
    /// DiagnosisTreatmentListItemControl.xaml 的交互逻辑
    /// 诊断治疗列表项控件
    /// </summary>
    public partial class DiagnosisTreatmentListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(DiagnosisTreatmentDto),
                typeof(DiagnosisTreatmentListItemControl),
                new PropertyMetadata(null));

        public DiagnosisTreatmentDto Data
        {
            get => (DiagnosisTreatmentDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public DiagnosisTreatmentListItemControl()
        {
            InitializeComponent();
        }
    }
}