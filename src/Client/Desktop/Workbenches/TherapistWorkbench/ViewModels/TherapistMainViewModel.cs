using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using System;

namespace LYBT.Desktop.Workbench.Therapist.ViewModels
{
    /// <summary>
    /// 理疗师工作台主视图模型
    /// </summary>
    public class TherapistMainViewModel : ServiceViewModel
    {
        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToTherapyPlanningCommand { get; }
        public DelegateCommand NavigateToTreatmentRecordCommand { get; }
        public DelegateCommand NavigateToRehabilitationManagementCommand { get; }
        public DelegateCommand NavigateToEquipmentManagementCommand { get; }

        public TherapistMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _regionManager = regionManager;

            // 初始化导航命令
            NavigateToTherapyPlanningCommand = new DelegateCommand(() => NavigateTo("TherapyPlanningView"));
            NavigateToTreatmentRecordCommand = new DelegateCommand(() => NavigateTo("TreatmentRecordView"));
            NavigateToRehabilitationManagementCommand = new DelegateCommand(() => NavigateTo("RehabilitationManagementView"));
            NavigateToEquipmentManagementCommand = new DelegateCommand(() => NavigateTo("EquipmentManagementView"));

            // 默认导航到理疗方案
            NavigateTo("TherapyPlanningView");
        }

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate(RegionNames.TherapistContentRegion, viewName);
            }
            catch (Exception)
            {
                // 如果视图不存在，显示占位界面
                // 暂时静默处理，后续可添加日志
            }
        }
    }
}