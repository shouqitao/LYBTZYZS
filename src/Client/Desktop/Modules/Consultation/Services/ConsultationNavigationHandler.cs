using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using Prism.Mvvm;
using CoreWorkflowStep = LYBT.Desktop.Core.Models.Consultation.WorkflowStep;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗导航处理器 - 专门负责工作流中的页面导航和内容切换
    /// UltraThink重构：从ConsultationWorkflowViewModel中提取导航处理职责
    /// </summary>
    public class ConsultationNavigationHandler : BindableBase
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ConsultationNavigationHandler> _logger;

        #endregion

        #region 导航状态属性

        private object? _currentStepContent;
        public object? CurrentStepContent
        {
            get => _currentStepContent;
            set => SetProperty(ref _currentStepContent, value);
        }

        private string _currentStepViewName = "";
        public string CurrentStepViewName
        {
            get => _currentStepViewName;
            set => SetProperty(ref _currentStepViewName, value);
        }

        private bool _isNavigating;
        public bool IsNavigating
        {
            get => _isNavigating;
            set => SetProperty(ref _isNavigating, value);
        }

        #endregion

        #region 构造函数

        public ConsultationNavigationHandler(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILogger<ConsultationNavigationHandler> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 导航到指定工作流步骤
        /// </summary>
        public async Task<bool> NavigateToStepAsync(CoreWorkflowStep step, NavigationParameters? parameters = null)
        {
            try
            {
                IsNavigating = true;

                var viewName = GetViewNameForStep(step);
                if (string.IsNullOrEmpty(viewName))
                {
                    _logger.LogWarning("未找到步骤对应的视图: {Step}", step);
                    return false;
                }

                await NavigateToViewAsync(viewName, parameters);
                
                CurrentStepViewName = viewName;
                _logger.LogInformation("成功导航到步骤: {Step} -> {ViewName}", step, viewName);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到步骤失败: {Step}", step);
                return false;
            }
            finally
            {
                IsNavigating = false;
            }
        }

        /// <summary>
        /// 加载患者选择视图
        /// </summary>
        public async Task LoadPatientSelectionViewAsync(NavigationParameters? parameters = null)
        {
            await NavigateToViewAsync("PatientSelectionView", parameters);
        }

        /// <summary>
        /// 加载四诊视图
        /// </summary>
        public async Task LoadFourDiagnosisViewAsync(NavigationParameters? parameters = null)
        {
            await NavigateToViewAsync("TCMFourDiagnosisView", parameters);
        }

        /// <summary>
        /// 加载辨证视图
        /// </summary>
        public async Task LoadDifferentiationViewAsync(NavigationParameters? parameters = null)
        {
            await NavigateToViewAsync("DifferentiationView", parameters);
        }

        /// <summary>
        /// 加载处方视图
        /// </summary>
        public async Task LoadPrescriptionViewAsync(NavigationParameters? parameters = null)
        {
            await NavigateToViewAsync("PrescriptionView", parameters);
        }

        /// <summary>
        /// 清除当前内容
        /// </summary>
        public void ClearCurrentContent()
        {
            CurrentStepContent = null;
            CurrentStepViewName = "";
            
            // 清除区域中的视图
            try
            {
                var contentRegion = _regionManager.Regions["ConsultationContentRegion"];
                if (contentRegion != null)
                {
                    contentRegion.RemoveAll();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除区域内容时发生错误");
            }
        }

        /// <summary>
        /// 刷新当前视图
        /// </summary>
        public async Task RefreshCurrentViewAsync()
        {
            if (!string.IsNullOrEmpty(CurrentStepViewName))
            {
                await NavigateToViewAsync(CurrentStepViewName);
            }
        }

        /// <summary>
        /// 检查是否可以离开当前步骤
        /// </summary>
        public async Task<bool> CanLeaveCurrentStepAsync()
        {
            try
            {
                // 发布导航确认事件
                var confirmationEvent = _eventAggregator.GetEvent<NavigationConfirmationRequestEvent>();
                var canLeave = true;
                
                // 这里可以添加具体的确认逻辑
                // 例如检查当前步骤是否有未保存的数据
                
                return await Task.FromResult(canLeave);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查导航权限时发生错误");
                return false;
            }
        }

        #endregion

        #region 私有方法

        private string GetViewNameForStep(CoreWorkflowStep step)
        {
            return step switch
            {
                CoreWorkflowStep.PatientSelection => "PatientSelectionView",
                CoreWorkflowStep.FourDiagnosis => "TCMFourDiagnosisView", 
                CoreWorkflowStep.Differentiation => "DifferentiationView",
                CoreWorkflowStep.Prescription => "PrescriptionView",
                _ => ""
            };
        }

        private async Task NavigateToViewAsync(string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    var contentRegion = _regionManager.Regions["ConsultationContentRegion"];
                    if (contentRegion != null)
                    {
                        var navParams = parameters ?? new NavigationParameters();
                        
                        contentRegion.RequestNavigate(new Uri(viewName, UriKind.Relative), result =>
                        {
                            var success = result != null;
                            if (success)
                            {
                                _logger.LogDebug("成功导航到视图: {ViewName}", viewName);
                                
                                // 发布导航完成事件
                                _eventAggregator.GetEvent<ConsultationNavigationCompletedEvent>()
                                    .Publish(new ConsultationNavigationEventArgs
                                    {
                                        ViewName = viewName,
                                        Parameters = navParams,
                                        Success = true
                                    });
                            }
                            else
                            {
                                var errorMessage = "导航失败";
                                _logger.LogWarning("导航到视图失败: {ViewName}, Error: {Error}", 
                                    viewName, errorMessage);
                                
                                // 发布导航失败事件
                                _eventAggregator.GetEvent<ConsultationNavigationCompletedEvent>()
                                    .Publish(new ConsultationNavigationEventArgs
                                    {
                                        ViewName = viewName,
                                        Parameters = navParams,
                                        Success = false,
                                        ErrorMessage = errorMessage
                                    });
                            }
                        }, null);
                    }
                    else
                    {
                        _logger.LogError("未找到ConsultationContentRegion区域");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行导航时发生错误: {ViewName}", viewName);
                throw;
            }
        }

        #endregion
    }

    #region 事件定义

    /// <summary>
    /// 导航确认请求事件
    /// </summary>
    public class NavigationConfirmationRequestEvent : PubSubEvent<NavigationConfirmationEventArgs>
    {
    }

    /// <summary>
    /// 诊疗导航完成事件
    /// </summary>
    public class ConsultationNavigationCompletedEvent : PubSubEvent<ConsultationNavigationEventArgs>
    {
    }

    /// <summary>
    /// 导航确认事件参数
    /// </summary>
    public class NavigationConfirmationEventArgs
    {
        public string FromView { get; set; } = "";
        public string ToView { get; set; } = "";
        public bool CanNavigate { get; set; } = true;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 诊疗导航事件参数
    /// </summary>
    public class ConsultationNavigationEventArgs
    {
        public string ViewName { get; set; } = "";
        public NavigationParameters? Parameters { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }


    /// <summary>
    /// 导航参数类（简化版）
    /// </summary>
    public class NavigationParameters : System.Collections.Generic.Dictionary<string, object>
    {
        public NavigationParameters() { }

        public NavigationParameters(string key, object value)
        {
            Add(key, value);
        }
    }

    #endregion
}