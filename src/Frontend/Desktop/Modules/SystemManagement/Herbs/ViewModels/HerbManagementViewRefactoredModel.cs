using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理页面视图模型（重构版）
    /// </summary>
    public class HerbManagementViewRefactoredModel : BindableBase
    {
        private string _pageTitle = "中药材管理";

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public HerbManagementViewRefactoredModel()
        {
            // 初始化
        }
    }
}