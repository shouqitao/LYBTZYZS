using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using LYBT.Desktop.Core.Constants;
using System;

namespace LYBT.Desktop.Workbench.Pharmacist.ViewModels
{
    /// <summary>
    /// 药剂师工作台主视图模型
    /// </summary>
    public class PharmacistMainViewModel : ServiceViewModel
    {
        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToDrugPreparationCommand { get; }
        public DelegateCommand NavigateToInventoryManagementCommand { get; }
        public DelegateCommand NavigateToMedicationGuidanceCommand { get; }
        public DelegateCommand NavigateToQualityControlCommand { get; }
        // UltraThink Phase 3.3: 集成增强版中药材管理
        public DelegateCommand NavigateToHerbManagementCommand { get; }

        public PharmacistMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _regionManager = regionManager;

            // 初始化导航命令
            NavigateToDrugPreparationCommand = new DelegateCommand(() => NavigateTo("DrugPreparationView"));
            NavigateToInventoryManagementCommand = new DelegateCommand(() => NavigateTo("InventoryManagementView"));
            NavigateToMedicationGuidanceCommand = new DelegateCommand(() => NavigateTo("MedicationGuidanceView"));
            NavigateToQualityControlCommand = new DelegateCommand(() => NavigateTo("QualityControlView"));
            // UltraThink Phase 3.3: 中药材管理集成
            NavigateToHerbManagementCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));

            // 默认导航到中药材管理 - 展示迁移成果
            NavigateTo("HerbManagementView");
        }

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate(RegionNames.PharmacistContentRegion, viewName);
            }
            catch (Exception)
            {
                // 如果视图不存在，显示占位界面
                // 暂时静默处理，后续可添加日志
            }
        }
    }
}