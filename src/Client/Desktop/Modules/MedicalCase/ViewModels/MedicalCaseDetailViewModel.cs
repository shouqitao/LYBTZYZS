using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例详情视图模型 - 架构重构后简化版本
    /// TODO: 重构完成后重新实现业务逻辑
    /// </summary>
    public class MedicalCaseDetailViewModel : ModernViewModelBase
    {
        public MedicalCaseDetailViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
        }
    }
}