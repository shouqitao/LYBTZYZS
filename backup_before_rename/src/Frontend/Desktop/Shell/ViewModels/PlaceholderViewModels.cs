using LYBT.Desktop.Core.ViewModels;
using Prism.Events;

namespace LYBT.Desktop.Shell.ViewModels
{
    // 以下是占位视图模型实现，待后续完善
    
    /// <summary>
    /// 患者列表视图模型
    /// </summary>
    public class PatientListViewModel : BaseViewModel
    {
        public PatientListViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    
    /// <summary>
    /// 患者详情视图模型
    /// </summary>
    public class PatientDetailViewModel : BaseViewModel
    {
        public PatientDetailViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    
    /// <summary>
    /// 处方视图模型
    /// </summary>
    public class PrescriptionViewModel : BaseViewModel
    {
        public PrescriptionViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    
    /// <summary>
    /// 诊疗视图模型
    /// </summary>
    public class ConsultationViewModel : BaseViewModel
    {
        public ConsultationViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    // TestHomeViewModel 和 DiagnosticHomeViewModel 已在单独的文件中定义
}