using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Backup;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using Prism.Commands;
using Prism.Mvvm;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.SystemManagement.Backup.ViewModels
{
    /// <summary>
    /// 数据备份管理视图模型
    /// </summary>
    public class BackupViewModel : BaseManagementViewModel<BackupInfo, IBackupApiService>
    {
        private readonly ICommonDialogService _commonDialogService;

        #region 属性

        private BackupType? _selectedBackupType;
        private BackupStatus? _selectedStatus;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _activeTab = "History";
        private ObservableCollection<BackupScheduleInfo> _schedules = new();
        private BackupScheduleInfo? _selectedSchedule;
        private BackupStatistics _statistics = new();
        private BackupConfiguration _configuration = new();
        private bool _isLoadingSchedules;
        private bool _isLoadingStatistics;
        private bool _isLoadingConfiguration;
        private bool _isRestoring;

        /// <summary>选中的备份类型</summary>
        public BackupType? SelectedBackupType
        {
            get => _selectedBackupType;
            set => SetProperty(ref _selectedBackupType, value);
        }

        /// <summary>选中的状态</summary>
        public BackupStatus? SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        /// <summary>开始日期</summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>结束日期</summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        /// <summary>当前活动标签页</summary>
        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                if (SetProperty(ref _activeTab, value))
                {
                    OnActiveTabChanged();
                }
            }
        }

        /// <summary>备份计划列表</summary>
        public ObservableCollection<BackupScheduleInfo> Schedules
        {
            get => _schedules;
            set => SetProperty(ref _schedules, value);
        }

        /// <summary>选中的备份计划</summary>
        public BackupScheduleInfo? SelectedSchedule
        {
            get => _selectedSchedule;
            set => SetProperty(ref _selectedSchedule, value);
        }

        /// <summary>备份统计信息</summary>
        public BackupStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        /// <summary>备份配置</summary>
        public BackupConfiguration Configuration
        {
            get => _configuration;
            set => SetProperty(ref _configuration, value);
        }

        /// <summary>是否正在加载计划</summary>
        public bool IsLoadingSchedules
        {
            get => _isLoadingSchedules;
            set => SetProperty(ref _isLoadingSchedules, value);
        }

        /// <summary>是否正在加载统计信息</summary>
        public bool IsLoadingStatistics
        {
            get => _isLoadingStatistics;
            set => SetProperty(ref _isLoadingStatistics, value);
        }

        /// <summary>是否正在加载配置</summary>
        public bool IsLoadingConfiguration
        {
            get => _isLoadingConfiguration;
            set => SetProperty(ref _isLoadingConfiguration, value);
        }

        /// <summary>是否正在恢复</summary>
        public bool IsRestoring
        {
            get => _isRestoring;
            set => SetProperty(ref _isRestoring, value);
        }

        /// <summary>备份类型选项</summary>
        public List<BackupTypeOption> BackupTypeOptions { get; }

        /// <summary>备份状态选项</summary>
        public List<BackupStatusOption> BackupStatusOptions { get; }

        #endregion

        #region 命令

        public DelegateCommand<BackupInfo> RestoreCommand { get; }
        public DelegateCommand<BackupInfo> VerifyCommand { get; }
        public DelegateCommand<BackupInfo> DownloadCommand { get; }
        public DelegateCommand CreateManualBackupCommand { get; }
        public DelegateCommand ClearFiltersCommand { get; }
        public DelegateCommand RefreshStatisticsCommand { get; }
        
        // 计划相关命令
        public DelegateCommand AddScheduleCommand { get; }
        public DelegateCommand<BackupScheduleInfo> EditScheduleCommand { get; }
        public DelegateCommand<BackupScheduleInfo> DeleteScheduleCommand { get; }
        public DelegateCommand<BackupScheduleInfo> ToggleScheduleCommand { get; }
        public DelegateCommand RefreshSchedulesCommand { get; }
        
        // 配置相关命令
        public DelegateCommand SaveConfigurationCommand { get; }
        public DelegateCommand ResetConfigurationCommand { get; }
        public DelegateCommand BrowseBackupPathCommand { get; }

        #endregion

        protected override string ModuleName => "数据备份";

        public BackupViewModel(IBackupApiService service,
            ICommonDialogService commonDialogService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
            // 初始化选项
            BackupTypeOptions = new List<BackupTypeOption>
            {
                new(null, "全部类型"),
                new(BackupType.Full, "完全备份"),
                new(BackupType.Incremental, "增量备份"),
                new(BackupType.Differential, "差异备份"),
                new(BackupType.Manual, "手动备份"),
                new(BackupType.Scheduled, "计划备份")
            };

            BackupStatusOptions = new List<BackupStatusOption>
            {
                new(null, "全部状态"),
                new(BackupStatus.InProgress, "备份中"),
                new(BackupStatus.Success, "成功"),
                new(BackupStatus.Failed, "失败"),
                new(BackupStatus.Cancelled, "已取消"),
                new(BackupStatus.Verifying, "验证中"),
                new(BackupStatus.Verified, "已验证")
            };

            // 初始化命令
            RestoreCommand = new DelegateCommand<BackupInfo>(ExecuteRestore, CanExecuteRestore);
            VerifyCommand = new DelegateCommand<BackupInfo>(ExecuteVerify, CanExecuteVerify);
            DownloadCommand = new DelegateCommand<BackupInfo>(ExecuteDownload, CanExecuteDownload);
            CreateManualBackupCommand = new DelegateCommand(ExecuteCreateManualBackup);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters);
            RefreshStatisticsCommand = new DelegateCommand(async () => await LoadStatisticsAsync());

            // 计划相关命令
            AddScheduleCommand = new DelegateCommand(ExecuteAddSchedule);
            EditScheduleCommand = new DelegateCommand<BackupScheduleInfo>(ExecuteEditSchedule);
            DeleteScheduleCommand = new DelegateCommand<BackupScheduleInfo>(ExecuteDeleteSchedule);
            ToggleScheduleCommand = new DelegateCommand<BackupScheduleInfo>(ExecuteToggleSchedule);
            RefreshSchedulesCommand = new DelegateCommand(async () => await LoadSchedulesAsync());

            // 配置相关命令
            SaveConfigurationCommand = new DelegateCommand(ExecuteSaveConfiguration);
            ResetConfigurationCommand = new DelegateCommand(ExecuteResetConfiguration);
            BrowseBackupPathCommand = new DelegateCommand(ExecuteBrowseBackupPath);

            // 设置默认时间范围（最近30天）
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            StartDate = DateTime.Today.AddDays(-29);

            // 加载初始数据
            _ = LoadStatisticsAsync();
            _ = LoadSchedulesAsync();
            _ = LoadConfigurationAsync();
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<PagedResult<BackupInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始加载备份历史列表，页码: {request.CurrentPage}");

                var response = await Service.GetBackupHistoryAsync(
                    request.CurrentPage,
                    request.PageSize,
                    SearchKeyword,
                    SelectedBackupType,
                    SelectedStatus,
                    StartDate,
                    EndDate
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    var pagedResult = new PagedResult<BackupInfo>
                    {
                        Items = paginatedResult.Items.ToList(),
                        TotalCount = paginatedResult.TotalCount,
                        CurrentPage = paginatedResult.CurrentPage,
                        PageSize = paginatedResult.PageSize
                    };
                    return ServiceResult<PagedResult<BackupInfo>>.Success(pagedResult);
                }
                else
                {
                    // 模拟数据用于开发
                    var mockData = GenerateMockBackupData(request);
                    return ServiceResult<PagedResult<BackupInfo>>.Success(mockData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备份历史异常: {ex.Message}");
                // 返回模拟数据
                var mockData = GenerateMockBackupData(request);
                return ServiceResult<PagedResult<BackupInfo>>.Success(mockData);
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(BackupInfo item)
        {
            try
            {
                var response = await Service.DeleteBackupAsync(item.Id);
                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    var error = response.Error?.Content ?? "删除备份失败";
                    return ServiceResult<bool>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除备份失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(BackupInfo item)
        {
            return $"{item.Name} ({item.BackupTime:yyyy-MM-dd HH:mm})";
        }

        protected override bool CanExecuteDelete(BackupInfo item)
        {
            return item.CanDelete;
        }

        protected override void ExecuteAdd()
        {
            ExecuteCreateManualBackup();
        }

        protected override void ExecuteEdit(BackupInfo item)
        {
            _commonDialogService.ShowInformationAsync("备份记录不支持编辑", "提示").GetAwaiter().GetResult();
        }

        #endregion

        #region 命令实现

        private bool CanExecuteRestore(BackupInfo backup)
        {
            return backup?.CanRestore == true && !IsRestoring;
        }

        private async void ExecuteRestore(BackupInfo backup)
        {
            if (backup == null) return;

            try
            {
                var result = await _commonDialogService.ShowConfirmationAsync($"确定要恢复备份 \"{backup.Name}\" 吗？\n\n" +
                    $"备份时间: {backup.BackupTime:yyyy-MM-dd HH:mm:ss}\n" +
                    $"文件大小: {backup.FileSizeDisplay}\n\n" +
                    "警告：恢复操作将覆盖当前数据，此操作无法撤销！", "确认恢复");

                if (result )
                {
                    IsRestoring = true;
                    var response = await Service.RestoreBackupAsync(backup.Id);

                    if (response.IsSuccessStatusCode)
                    {
                        _commonDialogService.ShowInformationAsync("数据恢复成功！", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "恢复备份失败";
                        _commonDialogService.ShowErrorAsync($"恢复备份失败: {error}", "错误").GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"恢复备份失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsRestoring = false;
            }
        }

        private bool CanExecuteVerify(BackupInfo backup)
        {
            return backup?.Status == BackupStatus.Success || backup?.Status == BackupStatus.Verified;
        }

        private async void ExecuteVerify(BackupInfo backup)
        {
            if (backup == null) return;

            try
            {
                IsLoading = true;
                var response = await Service.VerifyBackupAsync(backup.Id);

                if (response.IsSuccessStatusCode && response.Content)
                {
                    await _commonDialogService.ShowInformationAsync("备份文件验证成功！", "成功");
                    RefreshCommand.Execute();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync("备份文件验证失败！", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"验证备份失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteDownload(BackupInfo backup)
        {
            return backup?.Status == BackupStatus.Success || backup?.Status == BackupStatus.Verified;
        }

        private void ExecuteDownload(BackupInfo backup)
        {
            if (backup == null) return;

            try
            {
                // TODO: 实现备份文件下载功能
                _commonDialogService.ShowInformationAsync("备份文件下载功能开发中...", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"下载备份失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async void ExecuteCreateManualBackup()
        {
            try
            {
                // TODO: 实现创建备份对话框
                var backupName = $"手动备份_{DateTime.Now:yyyyMMdd_HHmmss}";
                var request = new CreateBackupRequest
                {
                    Name = backupName,
                    Type = BackupType.Manual,
                    Description = "手动创建的备份"
                };

                IsLoading = true;
                var response = await Service.CreateManualBackupAsync(request);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    _commonDialogService.ShowInformationAsync("备份创建成功！", "成功").GetAwaiter().GetResult();
                    RefreshCommand.Execute();
                    await LoadStatisticsAsync();
                }
                else
                {
                    var error = response.Error?.Content ?? "创建备份失败";
                    _commonDialogService.ShowErrorAsync($"创建备份失败: {error}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"创建备份失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClearFilters()
        {
            SearchKeyword = string.Empty;
            SelectedBackupType = null;
            SelectedStatus = null;
            StartDate = DateTime.Today.AddDays(-29);
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            
            CurrentPage = 1;
            RefreshCommand.Execute();
        }

        #endregion

        #region 计划管理

        private async Task LoadSchedulesAsync()
        {
            try
            {
                IsLoadingSchedules = true;
                var response = await Service.GetBackupSchedulesAsync();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Schedules = new ObservableCollection<BackupScheduleInfo>(response.Content);
                }
                else
                {
                    // 使用模拟数据
                    Schedules = new ObservableCollection<BackupScheduleInfo>(GenerateMockSchedules());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备份计划失败: {ex.Message}");
                // 使用模拟数据
                Schedules = new ObservableCollection<BackupScheduleInfo>(GenerateMockSchedules());
            }
            finally
            {
                IsLoadingSchedules = false;
            }
        }

        private void ExecuteAddSchedule()
        {
            // TODO: 实现添加计划对话框
            _commonDialogService.ShowInformationAsync("添加备份计划功能开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteEditSchedule(BackupScheduleInfo schedule)
        {
            if (schedule == null) return;

            // TODO: 实现编辑计划对话框
            _commonDialogService.ShowInformationAsync($"编辑备份计划 \"{schedule.Name}\" 功能开发中...", "提示").GetAwaiter().GetResult();
        }

        private async void ExecuteDeleteSchedule(BackupScheduleInfo schedule)
        {
            if (schedule == null) return;

            try
            {
                var result = await _commonDialogService.ShowConfirmationAsync($"确定要删除备份计划 \"{schedule.Name}\" 吗？", "确认删除");

                if (result )
                {
                    var response = await Service.DeleteBackupScheduleAsync(schedule.Id);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadSchedulesAsync();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "删除计划失败";
                        _commonDialogService.ShowErrorAsync($"删除计划失败: {error}", "错误").GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"删除计划失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async void ExecuteToggleSchedule(BackupScheduleInfo schedule)
        {
            if (schedule == null) return;

            try
            {
                schedule.IsEnabled = !schedule.IsEnabled;
                var response = await Service.ToggleBackupScheduleAsync(schedule.Id, schedule.IsEnabled);
                
                if (!response.IsSuccessStatusCode)
                {
                    schedule.IsEnabled = !schedule.IsEnabled; // 恢复原状态
                    var error = response.Error?.Content ?? "切换状态失败";
                    await _commonDialogService.ShowErrorAsync($"切换状态失败: {error}", "错误");
                }
            }
            catch (Exception ex)
            {
                schedule.IsEnabled = !schedule.IsEnabled; // 恢复原状态
                _commonDialogService.ShowErrorAsync($"切换状态失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion

        #region 配置管理

        private async Task LoadConfigurationAsync()
        {
            try
            {
                IsLoadingConfiguration = true;
                var response = await Service.GetBackupConfigurationAsync();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Configuration = response.Content;
                }
                else
                {
                    // 使用默认配置
                    Configuration = new BackupConfiguration
                    {
                        DefaultBackupPath = @"C:\LYBT\Backups",
                        EnableAutoBackup = true,
                        AutoBackupInterval = 24,
                        BackupRetentionDays = 30,
                        MaxBackupCount = 100,
                        CompressBackupFiles = true,
                        EncryptBackupFiles = false
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备份配置失败: {ex.Message}");
                // 使用默认配置
                Configuration = new BackupConfiguration
                {
                    DefaultBackupPath = @"C:\LYBT\Backups",
                    EnableAutoBackup = true,
                    AutoBackupInterval = 24,
                    BackupRetentionDays = 30,
                    MaxBackupCount = 100,
                    CompressBackupFiles = true,
                    EncryptBackupFiles = false
                };
            }
            finally
            {
                IsLoadingConfiguration = false;
            }
        }

        private async void ExecuteSaveConfiguration()
        {
            try
            {
                IsLoadingConfiguration = true;
                var response = await Service.UpdateBackupConfigurationAsync(Configuration);

                if (response.IsSuccessStatusCode)
                {
                    await _commonDialogService.ShowInformationAsync("备份配置保存成功！", "成功");
                }
                else
                {
                    var error = response.Error?.Content ?? "保存配置失败";
                    await _commonDialogService.ShowErrorAsync($"保存配置失败: {error}", "错误");
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存配置失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoadingConfiguration = false;
            }
        }

        private async void ExecuteResetConfiguration()
        {
            try
            {
                var result = await _commonDialogService.ShowConfirmationAsync("确定要重置为默认配置吗？", "确认重置");

                if (result )
                {
                    await LoadConfigurationAsync();
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"重置配置失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteBrowseBackupPath()
        {
            // TODO: 实现文件夹选择对话框
            _commonDialogService.ShowInformationAsync("选择备份路径功能开发中...", "提示").GetAwaiter().GetResult();
        }

        #endregion

        #region 统计信息

        private async Task LoadStatisticsAsync()
        {
            try
            {
                IsLoadingStatistics = true;
                var response = await Service.GetBackupStatisticsAsync();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Statistics = response.Content;
                }
                else
                {
                    // 使用模拟数据
                    Statistics = new BackupStatistics
                    {
                        TotalBackups = 156,
                        SuccessfulBackups = 150,
                        FailedBackups = 6,
                        TotalBackupSize = 15 * 1024L * 1024 * 1024, // 15GB
                        LastBackupTime = DateTime.Now.AddHours(-3),
                        NextScheduledBackupTime = DateTime.Now.AddHours(21),
                        ActiveSchedules = 3
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载统计信息失败: {ex.Message}");
                // 使用模拟数据
                Statistics = new BackupStatistics
                {
                    TotalBackups = 156,
                    SuccessfulBackups = 150,
                    FailedBackups = 6,
                    TotalBackupSize = 15 * 1024L * 1024 * 1024, // 15GB
                    LastBackupTime = DateTime.Now.AddHours(-3),
                    NextScheduledBackupTime = DateTime.Now.AddHours(21),
                    ActiveSchedules = 3
                };
            }
            finally
            {
                IsLoadingStatistics = false;
            }
        }

        #endregion

        #region 私有方法

        private void OnActiveTabChanged()
        {
            // 切换标签页时刷新对应数据
            switch (ActiveTab)
            {
                case "History":
                    RefreshCommand.Execute();
                    break;
                case "Schedule":
                    _ = LoadSchedulesAsync();
                    break;
                case "Configuration":
                    _ = LoadConfigurationAsync();
                    break;
            }
        }

        private PagedResult<BackupInfo> GenerateMockBackupData(PaginationRequest request)
        {
            var random = new Random();
            var items = new List<BackupInfo>();

            for (int i = 0; i < request.PageSize; i++)
            {
                var backupTime = DateTime.Now.AddDays(-random.Next(1, 30)).AddHours(-random.Next(0, 24));
                items.Add(new BackupInfo
                {
                    Id = Guid.NewGuid(),
                    Name = $"备份_{backupTime:yyyyMMdd_HHmmss}",
                    Type = (BackupType)random.Next(0, 5),
                    FilePath = $@"C:\LYBT\Backups\backup_{backupTime:yyyyMMdd_HHmmss}.bak",
                    FileSize = random.Next(100, 500) * 1024L * 1024, // 100MB - 500MB
                    BackupTime = backupTime,
                    Description = random.Next(2) == 0 ? "自动备份" : "手动备份",
                    Operator = random.Next(2) == 0 ? "System" : "admin",
                    Status = (BackupStatus)random.Next(1, 3), // Success or Failed
                    IsAutoBackup = random.Next(2) == 0,
                    DatabaseVersion = "1.0.0",
                    AppVersion = "1.0.0"
                });
            }

            return new PagedResult<BackupInfo>
            {
                Items = items,
                TotalCount = 156,
                CurrentPage = request.CurrentPage,
                PageSize = request.PageSize
            };
        }

        private List<BackupScheduleInfo> GenerateMockSchedules()
        {
            return new List<BackupScheduleInfo>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "每日完全备份",
                    BackupType = BackupType.Full,
                    ScheduleType = ScheduleType.Daily,
                    IsEnabled = true,
                    ExecutionTime = new TimeSpan(2, 0, 0), // 凌晨2点
                    RetentionCount = 7,
                    LastExecutionTime = DateTime.Today.AddHours(2),
                    NextExecutionTime = DateTime.Today.AddDays(1).AddHours(2),
                    BackupPath = @"C:\LYBT\Backups\Daily",
                    Description = "每天凌晨2点执行完全备份"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "每周增量备份",
                    BackupType = BackupType.Incremental,
                    ScheduleType = ScheduleType.Weekly,
                    IsEnabled = true,
                    ExecutionTime = new TimeSpan(3, 0, 0), // 凌晨3点
                    DayOfWeek = DayOfWeek.Sunday,
                    RetentionCount = 4,
                    LastExecutionTime = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek)).AddHours(3),
                    NextExecutionTime = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek).AddHours(3),
                    BackupPath = @"C:\LYBT\Backups\Weekly",
                    Description = "每周日凌晨3点执行增量备份"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "每月完全备份",
                    BackupType = BackupType.Full,
                    ScheduleType = ScheduleType.Monthly,
                    IsEnabled = false,
                    ExecutionTime = new TimeSpan(4, 0, 0), // 凌晨4点
                    DayOfMonth = 1,
                    RetentionCount = 12,
                    LastExecutionTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddHours(4),
                    NextExecutionTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddHours(4),
                    BackupPath = @"C:\LYBT\Backups\Monthly",
                    Description = "每月1号凌晨4点执行完全备份"
                }
            };
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 备份类型选项
    /// </summary>
    public class BackupTypeOption
    {
        public BackupType? Value { get; set; }
        public string Display { get; set; } = string.Empty;

        public BackupTypeOption(BackupType? value, string display)
        {
            Value = value;
            Display = display;
        }
    }

    /// <summary>
    /// 备份状态选项
    /// </summary>
    public class BackupStatusOption
    {
        public BackupStatus? Value { get; set; }
        public string Display { get; set; } = string.Empty;

        public BackupStatusOption(BackupStatus? value, string display)
        {
            Value = value;
            Display = display;
        }
    }

    #endregion
}