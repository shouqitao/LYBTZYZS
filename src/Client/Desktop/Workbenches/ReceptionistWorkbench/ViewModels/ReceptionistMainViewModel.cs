using System;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.Workbench.Receptionist.ViewModels
{
    /// <summary>
    /// 前台工作台主视图模型
    /// </summary>
    public class ReceptionistMainViewModel : ServiceViewModel
    {
        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToPatientReceptionCommand { get; }
        public DelegateCommand NavigateToAppointmentManagementCommand { get; }
        public DelegateCommand NavigateToBasicRegistrationCommand { get; }
        public DelegateCommand NavigateToInquiryCommand { get; }

        public ReceptionistMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _regionManager = regionManager;

            // 初始化导航命令
            NavigateToPatientReceptionCommand = new DelegateCommand(() => NavigateTo("PatientReceptionView"));
            NavigateToAppointmentManagementCommand = new DelegateCommand(() => NavigateTo("AppointmentManagementView"));
            NavigateToBasicRegistrationCommand = new DelegateCommand(() => NavigateTo("BasicRegistrationView"));
            NavigateToInquiryCommand = new DelegateCommand(() => NavigateTo("InquiryView"));

            // 默认导航到患者接待
            NavigateTo("PatientReceptionView");
        }

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate(RegionNames.ReceptionistContentRegion, viewName);
            }
            catch (Exception)
            {
                // 如果视图不存在，显示占位界面
                // 暂时静默处理，后续可添加日志
            }
        }
    }
}
