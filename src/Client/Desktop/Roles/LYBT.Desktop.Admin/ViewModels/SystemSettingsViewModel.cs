using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 系统设置视图模型
    /// Issue #1831 UI统一化 - 接入UnifiedViewModelBase
    /// Epic #1832 Phase 2 - 完成真实功能实现
    /// </summary>
    public class SystemSettingsViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly ISystemSettingsService _settingsService;

        #endregion

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
            ISystemSettingsService settingsService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            PageTitle = "系统设置";

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync());
            ResetCommand = new DelegateCommand(async () => await ExecuteResetAsync());
            BrowseBackupPathCommand = new DelegateCommand(async () => await ExecuteBrowseBackupPathAsync());
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 异步初始化 - 加载系统设置
        /// </summary>
        protected override Task InitializeAsync(NavigationParameters parameters)
        {
            Logger.LogInformation("加载系统设置");

            try
            {
                // 从服务加载设置
                SystemName = _settingsService.SystemName;
                HospitalName = _settingsService.HospitalName;
                ContactPhone = _settingsService.ContactPhone;
                AutoBackupEnabled = _settingsService.AutoBackupEnabled;
                BackupPath = _settingsService.BackupPath;

                Logger.LogInformation("系统设置加载成功: {SystemName}", SystemName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载系统设置失败");
                HandleError(ex, "加载系统设置");
            }

            return Task.CompletedTask;
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 执行保存
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            try
            {
                Logger.LogInformation("保存系统设置");
                SetIsBusy(true, "正在保存系统设置...");

                // 保存到服务
                _settingsService.SystemName = SystemName;
                _settingsService.HospitalName = HospitalName;
                _settingsService.ContactPhone = ContactPhone;
                _settingsService.AutoBackupEnabled = AutoBackupEnabled;
                _settingsService.BackupPath = BackupPath;

                _settingsService.Save();

                Logger.LogInformation("系统设置保存成功");
                await ShowSuccessMessageAsync("系统设置保存成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存系统设置失败");
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存系统设置", ex));
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 执行重置
        /// </summary>
        private async Task ExecuteResetAsync()
        {
            try
            {
                var confirmed = await ShowConfirmationAsync("确定要重置为默认设置吗？所有自定义配置将丢失。", "确认重置");
                if (!confirmed)
                {
                    return;
                }

                Logger.LogInformation("重置系统设置为默认值");
                SetIsBusy(true, "正在重置系统设置...");

                // 重置服务
                _settingsService.ResetToDefaults();

                // 重新加载到界面
                SystemName = _settingsService.SystemName;
                HospitalName = _settingsService.HospitalName;
                ContactPhone = _settingsService.ContactPhone;
                AutoBackupEnabled = _settingsService.AutoBackupEnabled;
                BackupPath = _settingsService.BackupPath;

                Logger.LogInformation("系统设置已重置");
                await ShowSuccessMessageAsync("系统设置已重置为默认值");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "重置系统设置失败");
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置系统设置", ex));
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 浏览备份路径
        /// </summary>
        private async Task ExecuteBrowseBackupPathAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择备份路径",
                    Filter = "所有文件 (*.*)|*.*",
                    CheckFileExists = false,
                    CheckPathExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    BackupPath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                    Logger.LogDebug("备份路径已设置为: {BackupPath}", BackupPath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择备份路径失败");
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("选择备份路径", ex));
            }
        }

        #endregion
    }
}
