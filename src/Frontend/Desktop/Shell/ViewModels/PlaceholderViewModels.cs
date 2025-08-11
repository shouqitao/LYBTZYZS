using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Core.ViewModels.Base;
using Prism.Events;

namespace LYBT.Desktop.Shell.ViewModels
{
    // 以下是占位视图模型实现，待后续完善
    
    /// <summary>
    /// 患者列表视图模型
    /// </summary>
    public class PatientListViewModel : ServiceViewModel
    {
        public PatientListViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    
    /// <summary>
    /// 患者详情视图模型
    /// </summary>
    public class PatientDetailViewModel : ServiceViewModel
    {
        public PatientDetailViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    
    /// <summary>
    /// 处方视图模型
    /// </summary>
    public class PrescriptionViewModel : ServiceViewModel
    {
        public PrescriptionViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    
    /// <summary>
    /// 诊疗视图模型
    /// </summary>
    public class ConsultationViewModel : ServiceViewModel
    {
        public ConsultationViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
    // TestHomeViewModel 和 DiagnosticHomeViewModel 已在单独的文件中定义
}