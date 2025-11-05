using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 系统设置视图模型 - 占位实现
    /// </summary>
    public class SystemSettingsViewModel : BindableBase, INavigationAware
    {
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

        private string _statusMessage = "系统设置（功能开发中）";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand ResetCommand { get; private set; }
        public DelegateCommand BrowseBackupPathCommand { get; private set; }

        #endregion

        #region 构造函数

        public SystemSettingsViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            SaveCommand = new DelegateCommand(ExecuteSave);
            ResetCommand = new DelegateCommand(ExecuteReset);
            BrowseBackupPathCommand = new DelegateCommand(ExecuteBrowseBackupPath);
        }

        #endregion

        #region 命令实现

        private void ExecuteSave()
        {
            StatusMessage = "保存系统设置（功能开发中）";
        }

        private void ExecuteReset()
        {
            SystemName = "中医诊疗系统";
            HospitalName = string.Empty;
            ContactPhone = string.Empty;
            AutoBackupEnabled = false;
            BackupPath = string.Empty;
            StatusMessage = "设置已重置";
        }

        private void ExecuteBrowseBackupPath()
        {
            StatusMessage = "浏览备份路径（功能开发中）";
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            StatusMessage = "系统设置（功能开发中）";
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        #endregion
    }
}
