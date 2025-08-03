using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 患者管理页面视图模型（使用新的基础列表视图）
    /// </summary>
    public class PatientManagementViewNewModel : BindableBase
    {
        private string _pageTitle = "患者管理";

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public PatientManagementViewNewModel()
        {
            // 初始化
        }
    }
}