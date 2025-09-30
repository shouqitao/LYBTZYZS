using LYBT.Desktop.ClinicalWorkstation.Navigation;
using Prism.Regions;

namespace LYBT.Desktop.ClinicalWorkstation.Services
{
    /// <summary>
    /// 诊疗工作台导航服务实现
    /// 为医生提供专业的诊疗相关功能导航
    /// </summary>
    public class ClinicalNavigator : IClinicalNavigator
    {
        private readonly IRegionManager _regionManager;
        private const string ContentRegion = "ClinicalContentRegion";

        public ClinicalNavigator(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        /// <inheritdoc/>
        public void NavigateToPatients()
        {
            NavigateToView("PatientManagementView");
        }

        /// <inheritdoc/>
        public void NavigateToConsultations()
        {
            NavigateToView("ConsultationManagementView");
        }

        /// <inheritdoc/>
        public void NavigateToMedicalCases()
        {
            NavigateToView("MedicalCaseManagementView");
        }

        /// <inheritdoc/>
        public void NavigateToPrescriptions()
        {
            NavigateToView("PrescriptionManagementView");
        }

        /// <inheritdoc/>
        public void NavigateToFormulas()
        {
            NavigateToView("FormulaManagementView");
        }

        /// <inheritdoc/>
        public void NavigateToPersonalSettings()
        {
            // TODO: PersonalSettingsView 暂未实现，显示提示
            System.Diagnostics.Debug.WriteLine("个人设置功能开发中");
        }

        /// <inheritdoc/>
        public void NavigateToView(string viewName)
        {
            _regionManager.RequestNavigate(ContentRegion, viewName);
        }

        /// <inheritdoc/>
        public void NavigateToView(string regionName, string viewName)
        {
            _regionManager.RequestNavigate(regionName, viewName);
        }
    }
}
