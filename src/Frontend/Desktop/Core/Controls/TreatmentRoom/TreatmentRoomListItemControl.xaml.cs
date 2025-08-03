using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.TreatmentRoom;

namespace LYBT.WPF.Client.Controls.TreatmentRoom
{
    /// <summary>
    /// TreatmentRoomListItemControl.xaml 的交互逻辑
    /// 治疗室列表项控件
    /// </summary>
    public partial class TreatmentRoomListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(TreatmentRoomDto),
                typeof(TreatmentRoomListItemControl),
                new PropertyMetadata(null));

        public TreatmentRoomDto Data
        {
            get => (TreatmentRoomDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(int),
                typeof(TreatmentRoomListItemControl),
                new PropertyMetadata(0));

        public int Duration
        {
            get => (int)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public TreatmentRoomListItemControl()
        {
            InitializeComponent();
        }
    }
}