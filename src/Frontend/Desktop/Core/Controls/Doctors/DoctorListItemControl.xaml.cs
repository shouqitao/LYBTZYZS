using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Doctors;

namespace LYBT.WPF.Client.Controls.Doctors
{
    /// <summary>
    /// DoctorListItemControl.xaml 的交互逻辑
    /// 医生列表项控件
    /// </summary>
    public partial class DoctorListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(DoctorDto),
                typeof(DoctorListItemControl),
                new PropertyMetadata(null));

        public DoctorDto Data
        {
            get => (DoctorDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public DoctorListItemControl()
        {
            InitializeComponent();
        }
    }
}