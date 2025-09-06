using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using Prism.Events;

namespace LYBT.Desktop.Shell.ViewModels {
    // 以下是占位视图模型实现，待后续完善

    /// <summary>
    /// 患者列表视图模型
    /// </summary>
    public class PatientListViewModel : ServiceViewModel {

        public PatientListViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
        }
    }

    /// <summary>
    /// 患者详情视图模型
    /// </summary>
    public class PatientDetailViewModel : ServiceViewModel {

        public PatientDetailViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
        }
    }

    /// <summary>
    /// 处方视图模型
    /// </summary>
    public class PrescriptionViewModel : ServiceViewModel {

        public PrescriptionViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
        }
    }

    /// <summary>
    /// 诊疗视图模型
    /// </summary>
    public class ConsultationViewModel : ServiceViewModel {

        public ConsultationViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
        }
    }

    // TestHomeViewModel 和 DiagnosticHomeViewModel 已在单独的文件中定义
}
