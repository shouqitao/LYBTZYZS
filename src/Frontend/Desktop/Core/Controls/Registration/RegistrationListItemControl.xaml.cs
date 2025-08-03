using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.WPF.Client.Controls.Registration
{
    /// <summary>
    /// RegistrationListItemControl.xaml 的交互逻辑
    /// 挂号列表项控件
    /// </summary>
    public partial class RegistrationListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(RegistrationDto),
                typeof(RegistrationListItemControl),
                new PropertyMetadata(null));

        public RegistrationDto Data
        {
            get => (RegistrationDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public RegistrationListItemControl()
        {
            InitializeComponent();
        }
    }
}