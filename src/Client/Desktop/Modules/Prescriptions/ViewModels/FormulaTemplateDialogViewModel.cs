using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 配方模板对话框视图模型 - 架构重构后简化版本
    /// TODO: 重构完成后重新实现业务逻辑
    /// </summary>
    public class FormulaTemplateDialogViewModel : ModernViewModelBase
    {
        public FormulaTemplateDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
        }
    }
}
