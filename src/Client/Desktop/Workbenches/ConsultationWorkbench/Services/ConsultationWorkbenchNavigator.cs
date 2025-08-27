using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Regions;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Workbench.Consultation.Navigation;

namespace LYBT.Desktop.Workbench.Consultation.Services
{
    /// <summary>
    /// 看诊工作台导航服务实现
    /// 为医生提供专业的看诊相关功能导航
    /// </summary>
    public class ConsultationWorkbenchNavigator : IConsultationWorkbenchNavigator
    {
        private readonly IRegionManager _regionManager;
        private const string ContentRegion = RegionNames.ConsultationWorkbenchContentRegion;

        public ConsultationWorkbenchNavigator(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        public void NavigateToPatients()
        {
            NavigateToView("PatientManagementView");
        }

        public void NavigateToConsultations()
        {
            NavigateToView("ConsultationManagementView");
        }

        public void NavigateToMedicalCases()
        {
            NavigateToView("MedicalCaseManagementView");
        }

        public void NavigateToPrescriptions()
        {
            NavigateToView("PrescriptionManagementView");
        }

        public void NavigateToFormulas()
        {
            NavigateToView("FormulaManagementView");
        }

        public void NavigateToPersonalSettings()
        {
            // TODO: PersonalSettingsView 暂未实现，显示提示
            System.Diagnostics.Debug.WriteLine("个人设置功能开发中");
        }

        public void NavigateToView(string viewName)
        {
            _regionManager.RequestNavigate(ContentRegion, viewName);
        }

        public void NavigateToView(string regionName, string viewName)
        {
            _regionManager.RequestNavigate(regionName, viewName);
        }
    }
}