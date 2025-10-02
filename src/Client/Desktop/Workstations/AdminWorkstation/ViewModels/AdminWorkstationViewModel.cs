using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.AdminWorkstation.ViewModels
{
    /// <summary>
    /// ��������̨��ͼģ��
    /// </summary>
    public class AdminWorkstationViewModel : UnifiedViewModelBase
    {
        private readonly IRegionManager _regionManager;
        private string _currentUserName = string.Empty;
        private bool _isInitialized = false;

        // ����ѡ��״̬
        private bool _isUserManagementSelected = true;
        private bool _isHerbManagementSelected;
        private bool _isPatientManagementSelected;
        private bool _isFormulaManagementSelected;
        private bool _isMedicalCaseManagementSelected;
        private bool _isSystemSettingsSelected;

        public AdminWorkstationViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, null, userNotificationService)
        {
            _regionManager = regionManager;

            // ��ʼ������
            NavigateCommand = new DelegateCommand<string>(ExecuteNavigate);
            LogoutCommand = new DelegateCommand(ExecuteLogout);

            // ���ĵ�¼�ɹ��¼�
            EventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
        }

        #region Properties

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public bool IsUserManagementSelected
        {
            get => _isUserManagementSelected;
            set => SetProperty(ref _isUserManagementSelected, value);
        }

        public bool IsHerbManagementSelected
        {
            get => _isHerbManagementSelected;
            set => SetProperty(ref _isHerbManagementSelected, value);
        }

        public bool IsPatientManagementSelected
        {
            get => _isPatientManagementSelected;
            set => SetProperty(ref _isPatientManagementSelected, value);
        }

        public bool IsFormulaManagementSelected
        {
            get => _isFormulaManagementSelected;
            set => SetProperty(ref _isFormulaManagementSelected, value);
        }

        public bool IsMedicalCaseManagementSelected
        {
            get => _isMedicalCaseManagementSelected;
            set => SetProperty(ref _isMedicalCaseManagementSelected, value);
        }

        public bool IsSystemSettingsSelected
        {
            get => _isSystemSettingsSelected;
            set => SetProperty(ref _isSystemSettingsSelected, value);
        }

        #endregion

        #region Commands

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion

        #region Methods

        private void ExecuteNavigate(string targetView)
        {
            try
            {
                Logger.LogInformation($"����������ģ�飺{targetView}");

                // ����ѡ��״̬
                UpdateSelectionState(targetView);

                // ��������Ӧ����ͼ
                string viewName = targetView switch
                {
                    "UserManagement" => "UserManagementView",
                    "HerbManagement" => "HerbManagementView",
                    "PatientManagement" => "PatientManagementView",
                    "FormulaManagement" => "FormulaManagementView",
                    "MedicalCaseManagement" => "MedicalCaseManagementView",
                    "SystemSettings" => "SystemSettingsView",
                    _ => "UserManagementView"
                };

                _regionManager.RequestNavigate("AdminContentRegion", viewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"������{targetView}ʧ��");
                ShowErrorMessage($"����ʧ�ܣ�{ex.Message}");
            }
        }

        private void UpdateSelectionState(string selectedModule)
        {
            // ��������ѡ��״̬
            IsUserManagementSelected = false;
            IsHerbManagementSelected = false;
            IsPatientManagementSelected = false;
            IsFormulaManagementSelected = false;
            IsMedicalCaseManagementSelected = false;
            IsSystemSettingsSelected = false;

            // ����ѡ��״̬
            switch (selectedModule)
            {
                case "UserManagement":
                    IsUserManagementSelected = true;
                    break;
                case "HerbManagement":
                    IsHerbManagementSelected = true;
                    break;
                case "PatientManagement":
                    IsPatientManagementSelected = true;
                    break;
                case "FormulaManagement":
                    IsFormulaManagementSelected = true;
                    break;
                case "MedicalCaseManagement":
                    IsMedicalCaseManagementSelected = true;
                    break;
                case "SystemSettings":
                    IsSystemSettingsSelected = true;
                    break;
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                Logger.LogInformation("�û������˳���¼");

                // �����ǳ��¼�
                EventAggregator.GetEvent<UserLoggedOutEvent>().Publish();

                // �����ص�¼����
                _regionManager.RequestNavigate("ContentRegion", "LoginView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "�˳���¼ʧ��");
                ShowErrorMessage($"�˳���¼ʧ�ܣ�{ex.Message}");
            }
        }

        private void OnUserLoggedIn(UserLoggedInEventArgs args)
        {
            CurrentUserName = args.Username;
            Logger.LogInformation($"����Ա {args.Username} �ѵ�¼");
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // UltraThink�޸���ʹ�� Dispatcher �ӳٵ���ȷ�� Region ��ȫע��
            if (!_isInitialized)
            {
                _isInitialized = true;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ExecuteNavigate("UserManagement");
                }, DispatcherPriority.Loaded);
            }
        }

        #endregion
    }
}
