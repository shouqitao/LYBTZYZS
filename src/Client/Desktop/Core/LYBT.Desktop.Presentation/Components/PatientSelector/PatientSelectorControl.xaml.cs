using System.Windows.Controls;
using Prism.Events;

namespace LYBT.Desktop.Presentation.Components.PatientSelector
{
    /// <summary>
    /// PatientSelectorControl.xaml 的交互逻辑
    /// </summary>
    public partial class PatientSelectorControl : UserControl
    {
        /// <summary>
        /// 初始化 PatientSelectorControl
        /// </summary>
        public PatientSelectorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化 PatientSelectorControl 并提供 EventAggregator
        /// </summary>
        /// <param name="eventAggregator">事件聚合器</param>
        public PatientSelectorControl(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            DataContext = new PatientSelectorViewModel(eventAggregator);
        }
    }
}
