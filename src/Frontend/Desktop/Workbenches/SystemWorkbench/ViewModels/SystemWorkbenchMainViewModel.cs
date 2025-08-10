using System;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Events;
using LYBT.WPF.Client.Workbenches.Core;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.BusinessModules.Shared;

namespace LYBT.WPF.Client.Workbenches.SystemWorkbench.ViewModels
{
    /// <summary>
    /// 系统管理工作台主视图模型
    /// </summary>
    public class SystemWorkbenchMainViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IWorkbenchRouter _workbenchRouter;
        private readonly ISharedPatientService _sharedPatientService;
        private readonly ISharedUserService _sharedUserService;

        private ObservableCollection<NavigationItem> _navigationItems;
        private string _currentViewTitle = "仪表板";
        private NavigationItem _selectedNavigationItem;

        public SystemWorkbenchMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IWorkbenchRouter workbenchRouter,
            ISharedPatientService sharedPatientService = null,
            ISharedUserService sharedUserService = null)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _workbenchRouter = workbenchRouter;
            _sharedPatientService = sharedPatientService;
            _sharedUserService = sharedUserService;

            InitializeCommands();
            LoadNavigationItems();
            
            // 导航到默认视图
            NavigateToDefaultView();
        }

        #region Properties

        /// <summary>
        /// 导航项列表
        /// </summary>
        public ObservableCollection<NavigationItem> NavigationItems
        {
            get => _navigationItems;
            set => SetProperty(ref _navigationItems, value);
        }

        /// <summary>
        /// 当前视图标题
        /// </summary>
        public string CurrentViewTitle
        {
            get => _currentViewTitle;
            set => SetProperty(ref _currentViewTitle, value);
        }

        /// <summary>
        /// 选中的导航项
        /// </summary>
        public NavigationItem SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set => SetProperty(ref _selectedNavigationItem, value);
        }

        #endregion

        #region Commands

        public DelegateCommand<NavigationItem> NavigateCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
        public DelegateCommand SettingsCommand { get; private set; }

        #endregion

        #region Methods

        private void InitializeCommands()
        {
            NavigateCommand = new DelegateCommand<NavigationItem>(ExecuteNavigate);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            SettingsCommand = new DelegateCommand(ExecuteSettings);
        }

        private void LoadNavigationItems()
        {
            // 从路由器获取管理员的导航项
            var items = _workbenchRouter.GetNavigationItems("管理员");
            NavigationItems = new ObservableCollection<NavigationItem>(items);
        }

        private void NavigateToDefaultView()
        {
            // 导航到默认的仪表板视图
            var defaultItem = NavigationItems.FirstOrDefault(x => x.Id == "dashboard");
            if (defaultItem != null)
            {
                ExecuteNavigate(defaultItem);
            }
        }

        private void ExecuteNavigate(NavigationItem item)
        {
            if (item == null || item.IsSeparator)
                return;

            try
            {
                SelectedNavigationItem = item;
                CurrentViewTitle = item.DisplayName;

                // 导航到指定视图
                var parameters = new NavigationParameters();
                if (item.Parameters != null)
                {
                    foreach (var param in item.Parameters)
                    {
                        parameters.Add(param.Key, param.Value);
                    }
                }

                _regionManager.RequestNavigate("SystemWorkbenchContentRegion", item.ViewName, parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航失败: {ex.Message}");
            }
        }

        private void ExecuteRefresh()
        {
            // 刷新当前视图
            if (SelectedNavigationItem != null)
            {
                ExecuteNavigate(SelectedNavigationItem);
            }
        }

        private void ExecuteSettings()
        {
            // 导航到设置页面
            var settingsItem = NavigationItems.FirstOrDefault(x => x.Id == "settings");
            if (settingsItem != null)
            {
                ExecuteNavigate(settingsItem);
            }
        }

        #endregion

        #region Shared Service Methods

        /// <summary>
        /// 快速创建患者
        /// 演示共享服务的使用
        /// </summary>
        public async void QuickCreatePatient()
        {
            if (_sharedPatientService != null)
            {
                // 使用共享服务创建患者
                var patientDto = new Shared.Models.Contracts.Patients.PatientDetailDto
                {
                    Name = "测试患者",
                    Phone = "13800138000",
                    Gender = Shared.Models.Enums.Gender.Male,
                    Age = 30
                };

                var result = await _sharedPatientService.CreatePatientAsync(patientDto);
                if (result.IsSuccess)
                {
                    // 创建成功，刷新列表
                    ExecuteRefresh();
                }
            }
        }

        #endregion
    }
}