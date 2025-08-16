using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Navigation.Regions;
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
        private const string ContentRegion = "ConsultationWorkbenchContent";

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
            NavigateToView("PersonalSettingsView");
        }

        public void NavigateToView(string viewName, NavigationParameters? parameters = null)
        {
            _regionManager.RequestNavigate(ContentRegion, viewName, parameters);
        }

        public void NavigateToView(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            _regionManager.RequestNavigate(regionName, viewName, parameters);
        }
    }
}