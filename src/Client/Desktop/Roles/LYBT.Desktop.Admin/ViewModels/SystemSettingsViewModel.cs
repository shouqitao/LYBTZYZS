using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 系统设置视图模型
    /// D2: 增加诊所配置化功能 (clinic-settings.json 热更新)
    /// </summary>
    public partial class SystemSettingsViewModel : NavigableViewModelBase
    {
        #region 服务依赖

        private readonly ISystemSettingsService _settingsService;
        private readonly IClinicSettingsService _clinicSettingsService;

        #endregion

        #region 系统设置属性

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

        #region 诊所配置属性 (D2)

        private string _clinicName = string.Empty;
        public string ClinicName
        {
            get => _clinicName;
            set => SetProperty(ref _clinicName, value);
        }

        private string _clinicAddress = string.Empty;
        public string ClinicAddress
        {
            get => _clinicAddress;
            set => SetProperty(ref _clinicAddress, value);
        }

        private string _clinicPhone = string.Empty;
        public string ClinicPhone
        {
            get => _clinicPhone;
            set => SetProperty(ref _clinicPhone, value);
        }

        private string _department = "中医科";
        public string ClinicDepartment
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private string _licenseNumber = string.Empty;
        public string LicenseNumber
        {
            get => _licenseNumber;
            set => SetProperty(ref _licenseNumber, value);
        }

        private string _email = string.Empty;
        public string ClinicEmail
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        #endregion

        #region 构造函数

        public SystemSettingsViewModel(
            IViewModelServices services,
            ISystemSettingsService settingsService,
            IClinicSettingsService clinicSettingsService)
            : base(services)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _clinicSettingsService = clinicSettingsService ?? throw new ArgumentNullException(nameof(clinicSettingsService));

            PageTitle = "系统设置";
        }

        #endregion

        #region 初始化

        protected override Task InitializeAsync(NavigationContext context)
        {
            Logger.LogInformation("加载系统设置");

            try
            {
                // 系统设置
                SystemName = _settingsService.SystemName;
                HospitalName = _settingsService.HospitalName;
                ContactPhone = _settingsService.ContactPhone;
                AutoBackupEnabled = _settingsService.AutoBackupEnabled;
                BackupPath = _settingsService.BackupPath;

                // D2: 诊所配置
                LoadClinicSettings();

                Logger.LogInformation("系统设置加载成功: {SystemName}", SystemName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载系统设置失败");
                SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载系统设置", ex));
            }

            return Task.CompletedTask;
        }

        private void LoadClinicSettings()
        {
            var clinicSettings = _clinicSettingsService.GetSettings();
            ClinicName = clinicSettings.Name;
            ClinicAddress = clinicSettings.Address;
            ClinicPhone = clinicSettings.Phone;
            ClinicDepartment = clinicSettings.Department;
            LicenseNumber = clinicSettings.LicenseNumber;
            ClinicEmail = clinicSettings.Email;
        }

        #endregion

        #region 命令实现

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                Logger.LogInformation("保存系统设置");
                SetBusy(true, "正在保存设置...");

                // 保存系统设置
                _settingsService.SystemName = SystemName;
                _settingsService.HospitalName = HospitalName;
                _settingsService.ContactPhone = ContactPhone;
                _settingsService.AutoBackupEnabled = AutoBackupEnabled;
                _settingsService.BackupPath = BackupPath;
                _settingsService.Save();

                // D2: 保存诊所配置
                var clinicSettings = new ClinicSettingsOptions
                {
                    Name = ClinicName,
                    Address = ClinicAddress,
                    Phone = ClinicPhone,
                    Department = ClinicDepartment,
                    LicenseNumber = LicenseNumber,
                    Email = ClinicEmail
                };
                var clinicSaved = await _clinicSettingsService.SaveSettingsAsync(clinicSettings);
                if (!clinicSaved)
                {
                    await ShowErrorMessageAsync("诊所配置保存失败，请检查文件权限");
                    return;
                }

                Logger.LogInformation("系统设置保存成功");
                await ShowSuccessMessageAsync("设置保存成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存系统设置失败");
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存系统设置", ex));
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        private async Task ResetAsync()
        {
            try
            {
                var confirmed = await ShowConfirmMessageAsync("确定要重置为默认设置吗？所有自定义配置将丢失。", "确认重置");
                if (!confirmed) return;

                Logger.LogInformation("重置系统设置为默认值");
                SetBusy(true, "正在重置设置...");

                // 重置系统设置
                _settingsService.ResetToDefaults();
                SystemName = _settingsService.SystemName;
                HospitalName = _settingsService.HospitalName;
                ContactPhone = _settingsService.ContactPhone;
                AutoBackupEnabled = _settingsService.AutoBackupEnabled;
                BackupPath = _settingsService.BackupPath;

                // D2: 重置诊所配置
                var defaultClinic = new ClinicSettingsOptions();
                await _clinicSettingsService.SaveSettingsAsync(defaultClinic);
                LoadClinicSettings();

                Logger.LogInformation("系统设置已重置");
                await ShowSuccessMessageAsync("设置已重置为默认值");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "重置系统设置失败");
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置系统设置", ex));
            }
            finally
            {
                SetBusy(false);
            }
        }

        [RelayCommand]
        private async Task BrowseBackupPathAsync()
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
