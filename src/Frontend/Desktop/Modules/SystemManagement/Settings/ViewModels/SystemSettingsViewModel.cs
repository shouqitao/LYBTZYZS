using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Settings;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Infrastructure.Configuration.Dtos;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Settings.ViewModels
{
    /// <summary>
    /// 系统设置管理视图模型
    /// </summary>
    public class SystemSettingsViewModel : BindableBase
    {
        private readonly ISystemSettingsApiService _service;

        #region 全局设置属性

        private GlobalSettingInfo _globalSettings = new();
        private bool _isGlobalSettingsLoading = false;
        private bool _isGlobalSettingsChanged = false;

        /// <summary>全局设置</summary>
        public GlobalSettingInfo GlobalSettings
        {
            get => _globalSettings;
            set => SetProperty(ref _globalSettings, value);
        }

        /// <summary>是否正在加载全局设置</summary>
        public bool IsGlobalSettingsLoading
        {
            get => _isGlobalSettingsLoading;
            set => SetProperty(ref _isGlobalSettingsLoading, value);
        }

        /// <summary>全局设置是否已更改</summary>
        public bool IsGlobalSettingsChanged
        {
            get => _isGlobalSettingsChanged;
            set => SetProperty(ref _isGlobalSettingsChanged, value);
        }

        #endregion

        #region 详细设置属性

        private ObservableCollection<SettingInfo> _settings = new();
        private bool _isSettingsLoading = false;
        private string _searchKeyword = string.Empty;
        private string _selectedGroup = "全部分组";
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;
        private int _pageSize = 20;

        /// <summary>设置列表</summary>
        public ObservableCollection<SettingInfo> Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        /// <summary>是否正在加载设置列表</summary>
        public bool IsSettingsLoading
        {
            get => _isSettingsLoading;
            set => SetProperty(ref _isSettingsLoading, value);
        }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的分组</summary>
        public string SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>总页数</summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>每页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {TotalCount} 条记录，第 {CurrentPage}/{TotalPages} 页";

        #endregion

        #region 分组选项

        /// <summary>分组选项列表</summary>
        public List<string> GroupOptions { get; }

        /// <summary>病历共享模式选项</summary>
        public List<KeyValuePair<string, string>> RecordSharingOptions { get; }

        /// <summary>同步模式选项</summary>
        public List<KeyValuePair<string, string>> SyncModeOptions { get; }

        #endregion

        #region 命令

        public DelegateCommand LoadGlobalSettingsCommand { get; }
        public DelegateCommand SaveGlobalSettingsCommand { get; }
        public DelegateCommand ResetGlobalSettingsCommand { get; }
        public DelegateCommand LoadSettingsCommand { get; }
        public DelegateCommand SearchSettingsCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }
        public DelegateCommand<SettingInfo> EditSettingCommand { get; }
        public DelegateCommand<SettingInfo> DeleteSettingCommand { get; }
        public DelegateCommand AddSettingCommand { get; }
        public DelegateCommand RefreshCacheCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        #endregion

        #region 分页属性

        /// <summary>能否转到首页</summary>
        public bool CanGoFirstPage => CurrentPage > 1;

        /// <summary>能否转到上一页</summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>能否转到下一页</summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>能否转到末页</summary>
        public bool CanGoLastPage => CurrentPage < TotalPages;

        #endregion

        public SystemSettingsViewModel(ISystemSettingsApiService service)
        {
            _service = service;

            // 初始化分组选项
            GroupOptions = new List<string>
            {
                "全部分组",
                "系统设置",
                "数据库设置", 
                "性能设置",
                "安全设置",
                "备份设置",
                "日志设置",
                "界面设置",
                "业务设置"
            };

            // 初始化病历共享模式选项
            RecordSharingOptions = new List<KeyValuePair<string, string>>
            {
                new("Private", "私有"),
                new("Public", "公开"),
                new("Selective", "选择性共享")
            };

            // 初始化同步模式选项
            SyncModeOptions = new List<KeyValuePair<string, string>>
            {
                new("Auto", "自动同步"),
                new("Manual", "手动同步"),
                new("Disabled", "禁用同步")
            };

            // 初始化命令
            LoadGlobalSettingsCommand = new DelegateCommand(async () => await LoadGlobalSettingsAsync());
            SaveGlobalSettingsCommand = new DelegateCommand(async () => await SaveGlobalSettingsAsync());
            ResetGlobalSettingsCommand = new DelegateCommand(ExecuteResetGlobalSettings);
            LoadSettingsCommand = new DelegateCommand(async () => await LoadSettingsAsync());
            SearchSettingsCommand = new DelegateCommand(async () => await SearchSettingsAsync());
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch);
            EditSettingCommand = new DelegateCommand<SettingInfo>(ExecuteEditSetting);
            DeleteSettingCommand = new DelegateCommand<SettingInfo>(async (setting) => await ExecuteDeleteSettingAsync(setting));
            AddSettingCommand = new DelegateCommand(ExecuteAddSetting);
            RefreshCacheCommand = new DelegateCommand(async () => await RefreshCacheAsync());
            FirstPageCommand = new DelegateCommand(async () => await ExecuteFirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await ExecuteLastPageAsync());

            // 自动加载数据
            _ = LoadGlobalSettingsAsync();
            _ = LoadSettingsAsync();

            // 监听全局设置变化
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GlobalSettings))
                {
                    IsGlobalSettingsChanged = true;
                }
                if (e.PropertyName == nameof(CurrentPage) || e.PropertyName == nameof(TotalPages))
                {
                    RaisePropertyChanged(nameof(CanGoFirstPage));
                    RaisePropertyChanged(nameof(CanGoPreviousPage));
                    RaisePropertyChanged(nameof(CanGoNextPage));
                    RaisePropertyChanged(nameof(CanGoLastPage));
                    RaisePropertyChanged(nameof(StatusText));
                }
                if (e.PropertyName == nameof(TotalCount))
                {
                    RaisePropertyChanged(nameof(StatusText));
                }
            };
        }

        private async Task LoadGlobalSettingsAsync()
        {
            try
            {
                IsGlobalSettingsLoading = true;
                var response = await _service.GetGlobalSettingsAsync();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    GlobalSettings = ConvertToGlobalSettingInfo(response.Content);
                    IsGlobalSettingsChanged = false;
                }
                else
                {
                    var error = response.Error?.Content ?? "获取全局设置失败";
                    MessageBox.Show($"获取全局设置失败: {error}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载全局设置异常: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGlobalSettingsLoading = false;
            }
        }

        private async Task SaveGlobalSettingsAsync()
        {
            try
            {
                var dto = ConvertToGlobalSettingsDto(GlobalSettings);
                var response = await _service.UpdateGlobalSettingsAsync(dto);

                if (response.IsSuccessStatusCode)
                {
                    IsGlobalSettingsChanged = false;
                    MessageBox.Show("全局设置保存成功", "成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadGlobalSettingsAsync(); // 重新加载获取最新数据
                }
                else
                {
                    var error = response.Error?.Content ?? "保存全局设置失败";
                    MessageBox.Show($"保存全局设置失败: {error}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存全局设置异常: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteResetGlobalSettings()
        {
            var result = MessageBox.Show("确定要重置全局设置为默认值吗？", "确认重置", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                GlobalSettings = new GlobalSettingInfo();
                IsGlobalSettingsChanged = true;
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                IsSettingsLoading = true;
                var group = SelectedGroup == "全部分组" ? null : ConvertGroupToKey(SelectedGroup);
                var response = await _service.GetSettingsAsync(group, SearchKeyword, CurrentPage, PageSize);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    Settings.Clear();
                    foreach (var setting in paginatedResult.Items.Select(ConvertToSettingInfo))
                    {
                        Settings.Add(setting);
                    }

                    TotalCount = paginatedResult.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                }
                else
                {
                    var error = response.Error?.Content ?? "获取设置列表失败";
                    MessageBox.Show($"获取设置列表失败: {error}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置列表异常: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSettingsLoading = false;
            }
        }

        private async Task SearchSettingsAsync()
        {
            CurrentPage = 1;
            await LoadSettingsAsync();
        }

        private void ExecuteClearSearch()
        {
            SearchKeyword = string.Empty;
            SelectedGroup = "全部分组";
            CurrentPage = 1;
            _ = LoadSettingsAsync();
        }

        private void ExecuteEditSetting(SettingInfo setting)
        {
            if (setting == null) return;

            // TODO: 实现设置编辑对话框
            MessageBox.Show($"编辑设置功能开发中...\n设置键: {setting.Key}", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExecuteDeleteSettingAsync(SettingInfo setting)
        {
            if (setting == null) return;
            
            if (setting.IsSystem)
            {
                MessageBox.Show("系统设置不能删除", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除设置 '{setting.Key}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _service.DeleteSettingAsync(setting.Key);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("设置删除成功", "成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadSettingsAsync();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "删除设置失败";
                        MessageBox.Show($"删除设置失败: {error}", "错误", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除设置异常: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteAddSetting()
        {
            // TODO: 实现添加设置对话框
            MessageBox.Show("添加设置功能开发中...", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task RefreshCacheAsync()
        {
            try
            {
                var response = await _service.RefreshAllCacheAsync();
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("缓存刷新成功", "成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var error = response.Error?.Content ?? "刷新缓存失败";
                    MessageBox.Show($"刷新缓存失败: {error}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新缓存异常: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 分页命令实现

        private async Task ExecuteFirstPageAsync()
        {
            if (CanGoFirstPage)
            {
                CurrentPage = 1;
                await LoadSettingsAsync();
            }
        }

        private async Task ExecutePreviousPageAsync()
        {
            if (CanGoPreviousPage)
            {
                CurrentPage--;
                await LoadSettingsAsync();
            }
        }

        private async Task ExecuteNextPageAsync()
        {
            if (CanGoNextPage)
            {
                CurrentPage++;
                await LoadSettingsAsync();
            }
        }

        private async Task ExecuteLastPageAsync()
        {
            if (CanGoLastPage)
            {
                CurrentPage = TotalPages;
                await LoadSettingsAsync();
            }
        }

        #endregion

        #region 转换方法

        private GlobalSettingInfo ConvertToGlobalSettingInfo(GlobalSettingsDto dto)
        {
            return new GlobalSettingInfo
            {
                Id = dto.Id,
                SystemName = dto.SystemName,
                SystemVersion = dto.SystemVersion,
                SystemLogo = dto.SystemLogo,
                DefaultRecordSharing = dto.DefaultRecordSharing,
                SyncMode = dto.SyncMode,
                BackupInterval = dto.BackupInterval,
                LogRetentionDays = dto.LogRetentionDays,
                SessionTimeoutMinutes = dto.SessionTimeoutMinutes,
                MaxFileUploadSizeMB = dto.MaxFileUploadSizeMB,
                EnableAuditLog = dto.EnableAuditLog,
                EnablePerformanceMonitoring = dto.EnablePerformanceMonitoring,
                LastUpdated = dto.LastUpdated,
                UpdatedByName = dto.UpdatedByName
            };
        }

        private GlobalSettingsDto ConvertToGlobalSettingsDto(GlobalSettingInfo info)
        {
            return new GlobalSettingsDto
            {
                Id = info.Id,
                SystemName = info.SystemName,
                SystemVersion = info.SystemVersion,
                SystemLogo = info.SystemLogo,
                DefaultRecordSharing = info.DefaultRecordSharing,
                SyncMode = info.SyncMode,
                BackupInterval = info.BackupInterval,
                LogRetentionDays = info.LogRetentionDays,
                SessionTimeoutMinutes = info.SessionTimeoutMinutes,
                MaxFileUploadSizeMB = info.MaxFileUploadSizeMB,
                EnableAuditLog = info.EnableAuditLog,
                EnablePerformanceMonitoring = info.EnablePerformanceMonitoring
            };
        }

        private SettingInfo ConvertToSettingInfo(SettingsDto dto)
        {
            return new SettingInfo
            {
                Id = dto.Id,
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description,
                ValueType = dto.ValueType,
                Group = dto.Group,
                SortOrder = dto.SortOrder,
                IsSystem = dto.IsSystem,
                IsEnabled = dto.IsEnabled,
                UpdateTime = dto.UpdateTime,
                Remark = dto.Remark
            };
        }

        private string ConvertGroupToKey(string groupDisplay)
        {
            return groupDisplay switch
            {
                "系统设置" => "System",
                "数据库设置" => "Database",
                "性能设置" => "Performance",
                "安全设置" => "Security",
                "备份设置" => "Backup",
                "日志设置" => "Log",
                "界面设置" => "UI",
                "业务设置" => "Business",
                _ => groupDisplay
            };
        }

        #endregion
    }
}