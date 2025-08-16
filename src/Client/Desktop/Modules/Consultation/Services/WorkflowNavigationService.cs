using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Navigation.Regions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Consultation.Interfaces;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗工作流导航服务
    /// 负责处理工作流中的页面导航和区域管理
    /// </summary>
    public class WorkflowNavigationService : IWorkflowNavigationService
    {
        #region 私有字段

        private readonly IRegionManager _regionManager;
        private readonly ILogger<WorkflowNavigationService> _logger;

        #endregion

        #region 构造函数

        public WorkflowNavigationService(
            IRegionManager regionManager,
            ILogger<WorkflowNavigationService> logger)
        {
            _regionManager = regionManager;
            _logger = logger;
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到指定的工作流步骤
        /// </summary>
        public async Task<bool> NavigateToStepAsync(WorkflowStep step, object? parameters = null)
        {
            try
            {
                var viewName = GetViewNameForStep(step);
                var navigationParameters = BuildNavigationParameters(parameters);

                _regionManager.RequestNavigate("WorkflowContentRegion", 
                    $"{viewName}{navigationParameters}");
                
                _logger.LogInformation($"导航到工作流步骤: {step}");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导航到工作流步骤 {step} 失败");
                await Task.CompletedTask;
                return false;
            }
        }

        /// <summary>
        /// 导航到患者选择页面
        /// </summary>
        public async Task NavigateToPatientSelectionAsync(Guid? patientId = null)
        {
            var parameters = patientId.HasValue ? $"?PatientId={patientId}" : "";
            _regionManager.RequestNavigate("WorkflowContentRegion", 
                $"PatientSelectionView{parameters}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 导航到四诊采集页面
        /// </summary>
        public async Task NavigateToFourDiagnosisAsync(Guid medicalCaseId, Guid patientId)
        {
            _regionManager.RequestNavigate("WorkflowContentRegion", 
                $"TCMFourDiagnosisView?MedicalCaseId={medicalCaseId}&PatientId={patientId}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 导航到辨证分析页面
        /// </summary>
        public async Task NavigateToDifferentiationAsync(Guid medicalCaseId, Guid consultationId)
        {
            _regionManager.RequestNavigate("WorkflowContentRegion", 
                $"DifferentiationView?MedicalCaseId={medicalCaseId}&ConsultationId={consultationId}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 导航到处方开具页面
        /// </summary>
        public async Task NavigateToPrescriptionAsync(Guid medicalCaseId, Guid consultationId)
        {
            _regionManager.RequestNavigate("WorkflowContentRegion", 
                $"PrescriptionView?MedicalCaseId={medicalCaseId}&ConsultationId={consultationId}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 导航回主页
        /// </summary>
        public async Task NavigateToHomeAsync()
        {
            _regionManager.RequestNavigate("ContentRegion", "HomeView");
            _logger.LogInformation("导航回主页");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 导航到医疗案例列表
        /// </summary>
        public async Task NavigateToMedicalCaseListAsync()
        {
            _regionManager.RequestNavigate("ContentRegion", "MedicalCaseListView");
            _logger.LogInformation("导航到医疗案例列表");
            await Task.CompletedTask;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据工作流步骤获取对应的视图名称
        /// </summary>
        private string GetViewNameForStep(WorkflowStep step)
        {
            return step switch
            {
                WorkflowStep.PatientSelection => "PatientSelectionView",
                WorkflowStep.FourDiagnosis => "TCMFourDiagnosisView",
                WorkflowStep.Differentiation => "DifferentiationView",
                WorkflowStep.Prescription => "PrescriptionView",
                _ => throw new ArgumentException($"未知的工作流步骤: {step}")
            };
        }

        /// <summary>
        /// 构建导航参数字符串
        /// </summary>
        private string BuildNavigationParameters(object? parameters)
        {
            if (parameters == null) return "";

            // 这里可以根据需要扩展参数构建逻辑
            // 当前简化实现
            return parameters.ToString() ?? "";
        }

        #endregion
    }
}