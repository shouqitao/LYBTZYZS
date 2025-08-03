using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.WPF.Client.Controls.Patients
{
    /// <summary>
    /// PatientListItemControl.xaml 的交互逻辑
    /// 患者列表项控件
    /// </summary>
    public partial class PatientListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(PatientDto),
                typeof(PatientListItemControl),
                new PropertyMetadata(null));

        public PatientDto Data
        {
            get => (PatientDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public PatientListItemControl()
        {
            InitializeComponent();
        }
    }
}