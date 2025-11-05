using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 系统设置视图模型
    /// </summary>
    public class SystemSettingsViewModel : BindableBase, INavigationAware
    {
        private readonly ILogger<SystemSettingsViewModel> _logger;
        private readonly IRegionManager _regionManager;

        #region 属性

        private string _systemName = "中医诊疗系统";
        public string SystemName
        {
            get => _systemName;
            set => SetProperty(ref _systemName, value);
        }

        private string _hospitalName = string.Empty;
        public string HospitalName
        {
            get => _hospitalName;
            set => SetProperty(ref _hospitalName, value);
        }

        private string _contactPhone = string.Empty;
        public string ContactPhone
        {
            get => _contactPhone;
            set => SetProperty(ref _contactPhone, value);
        }

        private bool _autoBackupEnabled;
        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set => SetProperty(ref _autoBackupEnabled, value);
        }

        private string _backupPath = string.Empty;
        public string BackupPath
        {
            get => _backupPath;
            set => SetProperty(ref _backupPath, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand ResetCommand { get; private set; }
        public DelegateCommand BrowseBackupPathCommand { get; private set; }

        #endregion

        #region 构造函数

        public SystemSettingsViewModel(
            ILogger<SystemSettingsViewModel> logger,
            IRegionManager regionManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            SaveCommand = new DelegateCommand(ExecuteSave);
            ResetCommand = new DelegateCommand(ExecuteReset);
            BrowseBackupPathCommand = new DelegateCommand(ExecuteBrowseBackupPath);

            _logger.LogDebug("系统设置ViewModel已初始化");
        }

        #endregion

        #region 命令实现

        private void ExecuteSave()
        {
            _logger.LogInformation("保存系统设置");
            // TODO: 实现保存逻辑
        }

        private void ExecuteReset()
        {
            _logger.LogInformation("重置系统设置");
            SystemName = "中医诊疗系统";
            HospitalName = string.Empty;
            ContactPhone = string.Empty;
            AutoBackupEnabled = false;
            BackupPath = string.Empty;
        }

        private void ExecuteBrowseBackupPath()
        {
            _logger.LogInformation("浏览备份路径");
            // TODO: 打开文件夹选择对话框
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _logger.LogDebug("导航到系统设置页面");
            // TODO: 加载现有设置
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _logger.LogDebug("离开系统设置页面");
        }

        #endregion
    }
}
