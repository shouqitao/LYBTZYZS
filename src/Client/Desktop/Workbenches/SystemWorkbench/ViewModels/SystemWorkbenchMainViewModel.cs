using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Workbench.Core;
using LYBT.Shared.Interfaces.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Workbench.Admin.ViewModels
{

    /// <summary>
    /// 系统管理工作台主视图模型
    /// </summary>
    public class SystemWorkbenchMainViewModel : ServiceViewModel
    {
        private readonly IRegionManager _regionManager;
        private readonly IWorkbenchRouter _workbenchRouter;
        private readonly IPatientService? _patientService;
        private readonly IUserService? _userService;

        private ObservableCollection<NavigationItem> _navigationItems = null!;
        private string _currentViewTitle = "仪表板";
        private NavigationItem _selectedNavigationItem = null!;

        public SystemWorkbenchMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IWorkbenchRouter workbenchRouter,
            IErrorHandlingService errorHandlingService,
            IPatientService? patientService = null,
            IUserService? userService = null)
            : base(eventAggregator, errorHandlingService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎯 SystemWorkbenchMainViewModel构造函数开始");

                _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
                _workbenchRouter = workbenchRouter ?? throw new ArgumentNullException(nameof(workbenchRouter));
                _patientService = patientService;
                _userService = userService;

                System.Diagnostics.Debug.WriteLine("✅ 依赖注入参数验证成功");

                InitializeCommands();
                System.Diagnostics.Debug.WriteLine("✅ 命令初始化完成");

                LoadNavigationItems();
                System.Diagnostics.Debug.WriteLine("✅ 导航项加载完成");

                // 导航到默认视图
                NavigateToDefaultView();
                System.Diagnostics.Debug.WriteLine("✅ 默认视图导航完成");

                System.Diagnostics.Debug.WriteLine("🎯 SystemWorkbenchMainViewModel构造函数完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SystemWorkbenchMainViewModel构造失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                throw;
            }
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

        #endregion Properties

        #region Commands

        public DelegateCommand<NavigationItem> NavigateCommand { get; private set; } = null!;
        public new DelegateCommand RefreshCommand { get; private set; } = null!;
        public DelegateCommand SettingsCommand { get; private set; } = null!;

        #endregion Commands

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

            // 诊断信息
            var diagnosticPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_SystemWorkbench_Debug.txt");
            var diagnosticInfo = $@"=== SystemWorkbench导航诊断 ===
时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
请求角色: 管理员
获取的导航项数量: {items.Count()}
NavigationItems属性数量: {NavigationItems?.Count ?? 0}
CurrentViewTitle: {CurrentViewTitle}
ViewModel实例化成功: True
导航项详情:
{string.Join(Environment.NewLine, items.Select((item, index) => $"  {index + 1}. {item.DisplayName} - {item.ViewName} (Module: {item.Module})"))}
=== 诊断结束 ===";
            File.AppendAllText(diagnosticPath, diagnosticInfo + Environment.NewLine);

            // 强制触发属性变更通知
            RaisePropertyChanged(nameof(NavigationItems));
            RaisePropertyChanged(nameof(CurrentViewTitle));

            System.Diagnostics.Debug.WriteLine($"[SystemWorkbench] LoadNavigationItems完成 - 项目数: {NavigationItems?.Count ?? 0}");
        }

        private void NavigateToDefaultView()
        {
            var diagnosticPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_Navigation_Debug.txt");

            // UltraThink v2.0: 导航到第一个可用的导航项（用户管理）
            var defaultItem = NavigationItems.FirstOrDefault(x => !x.IsSeparator && !string.IsNullOrEmpty(x.ViewName));
            if (defaultItem != null)
            {
                var message = $"🎯 NavigateToDefaultView: 找到默认项 {defaultItem.DisplayName} -> {defaultItem.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(message);
                File.AppendAllText(diagnosticPath, message + Environment.NewLine);

                ExecuteNavigate(defaultItem);

                var completeMessage = $"✅ NavigateToDefaultView: 默认导航执行完成 [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(completeMessage);
                File.AppendAllText(diagnosticPath, completeMessage + Environment.NewLine);
            }
            else
            {
                // 如果没有导航项，显示欢迎页面
                var noItemMessage = $"⚠️ NavigateToDefaultView: 没有找到可用的导航项 [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(noItemMessage);
                File.AppendAllText(diagnosticPath, noItemMessage + Environment.NewLine);
                CurrentViewTitle = "系统管理工作台";
            }
        }

        private void ExecuteNavigate(NavigationItem item)
        {
            if (item == null || item.IsSeparator)
            {
                return;
            }

            try
            {
                var diagnosticPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_Navigation_Debug.txt");
                var logMessage = $"🚀 ExecuteNavigate开始: {item.DisplayName} -> {item.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(logMessage);
                File.AppendAllText(diagnosticPath, logMessage + Environment.NewLine);

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

                var navigationMessage = $"🎯 请求导航到区域: SystemWorkbenchContentRegion, 视图: {item.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(navigationMessage);
                File.AppendAllText(diagnosticPath, navigationMessage + Environment.NewLine);

                // 检查区域是否存在，如果不存在则等待后重试
                if (!_regionManager.Regions.ContainsRegionWithName(RegionNames.SystemWorkbenchContentRegion))
                {
                    var waitMessage = $"⏳ SystemWorkbenchContentRegion区域不存在，等待100ms后重试 [{DateTime.Now:HH:mm:ss.fff}]";
                    System.Diagnostics.Debug.WriteLine(waitMessage);
                    File.AppendAllText(diagnosticPath, waitMessage + Environment.NewLine);

                    // 使用Dispatcher延迟执行，让UI有时间完全加载
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        new Action(() => RetryNavigate(item, parameters, diagnosticPath)));
                    return;
                }

                // 区域存在，执行导航
                PerformNavigation(item, parameters, diagnosticPath);
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ ExecuteNavigate异常: {ex.Message}{Environment.NewLine}   StackTrace: {ex.StackTrace} [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                var diagnosticPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_Navigation_Debug.txt");
                File.AppendAllText(diagnosticPath, errorMessage + Environment.NewLine);
            }
        }

        private void RetryNavigate(NavigationItem item, NavigationParameters parameters, string diagnosticPath)
        {
            try
            {
                if (_regionManager.Regions.ContainsRegionWithName(RegionNames.SystemWorkbenchContentRegion))
                {
                    var retryMessage = $"✅ 重试成功：SystemWorkbenchContentRegion区域现已存在 [{DateTime.Now:HH:mm:ss.fff}]";
                    System.Diagnostics.Debug.WriteLine(retryMessage);
                    File.AppendAllText(diagnosticPath, retryMessage + Environment.NewLine);
                    PerformNavigation(item, parameters, diagnosticPath);
                }
                else
                {
                    var failMessage = $"❌ 重试失败：SystemWorkbenchContentRegion区域仍不存在 [{DateTime.Now:HH:mm:ss.fff}]";
                    System.Diagnostics.Debug.WriteLine(failMessage);
                    File.AppendAllText(diagnosticPath, failMessage + Environment.NewLine);

                    // 列出当前所有区域进行调试
                    var allRegions = string.Join(", ", _regionManager.Regions.Select(r => r.Name));
                    var regionInfo = $"📋 当前所有区域: {allRegions} [{DateTime.Now:HH:mm:ss.fff}]";
                    System.Diagnostics.Debug.WriteLine(regionInfo);
                    File.AppendAllText(diagnosticPath, regionInfo + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                var retryError = $"❌ 重试导航异常: {ex.Message} [{DateTime.Now:HH:mm:ss.fff}]";
                System.Diagnostics.Debug.WriteLine(retryError);
                File.AppendAllText(diagnosticPath, retryError + Environment.NewLine);
            }
        }

        private void PerformNavigation(NavigationItem item, NavigationParameters parameters, string diagnosticPath)
        {
            var region = _regionManager.Regions[RegionNames.SystemWorkbenchContentRegion];

            // UltraThink Fix: 智能清理现有视图 - 只有在导航到不同视图时才清理
            if (region.Views.Any())
            {
                // 检查当前活动视图是否与要导航的视图不同
                var activeView = region.ActiveViews.FirstOrDefault();
                var shouldClearViews = activeView == null ||
                                     !activeView.GetType().Name.Equals($"{item.ViewName}", StringComparison.OrdinalIgnoreCase);

                if (shouldClearViews)
                {
                    var existingViews = region.Views.ToList();
                    foreach (var view in existingViews)
                    {
                        region.Remove(view);
                    }

                    var clearMessage = $"🧹 已清除 {existingViews.Count} 个现有视图，准备加载 {item.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                    System.Diagnostics.Debug.WriteLine(clearMessage);
                    File.AppendAllText(diagnosticPath, clearMessage + Environment.NewLine);
                }
                else
                {
                    var skipMessage = $"⚡ 跳过视图清理 - 当前视图已经是 {item.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                    System.Diagnostics.Debug.WriteLine(skipMessage);
                    File.AppendAllText(diagnosticPath, skipMessage + Environment.NewLine);
                }
            }

            var regionInfo = $"✅ SystemWorkbenchContentRegion区域存在，当前视图数量: {region.Views.Count()} [{DateTime.Now:HH:mm:ss.fff}]";
            System.Diagnostics.Debug.WriteLine(regionInfo);
            File.AppendAllText(diagnosticPath, regionInfo + Environment.NewLine);

            _regionManager.RequestNavigate(RegionNames.SystemWorkbenchContentRegion, item.ViewName, navigationResult =>
            {
                var resultMessage = string.Empty;
                if (navigationResult.Result == true)
                {
                    resultMessage = $"✅ 导航成功: {item.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                }
                else
                {
                    resultMessage = $"❌ 导航失败: {item.ViewName} [{DateTime.Now:HH:mm:ss.fff}]";
                    if (navigationResult.Error != null)
                    {
                        resultMessage += $"{Environment.NewLine}   错误类型: {navigationResult.Error.GetType().Name}";
                        resultMessage += $"{Environment.NewLine}   错误信息: {navigationResult.Error.Message}";
                        if (navigationResult.Error.InnerException != null)
                        {
                            resultMessage += $"{Environment.NewLine}   内部异常: {navigationResult.Error.InnerException.Message}";
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(resultMessage);
                File.AppendAllText(diagnosticPath, resultMessage + Environment.NewLine);
            }, parameters);
        }

        private new void ExecuteRefresh()
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

        #endregion Methods

        #region Shared Service Methods

        /// <summary>
        /// 快速创建患者
        /// 演示共享服务的使用
        /// </summary>
        public async Task QuickCreatePatientAsync()
        {
            try
            {
                if (_patientService != null)
                {
                    // 使用患者服务创建患者（UltraThink：使用正确的创建DTO类型）
                    var patientDto = new LYBT.Shared.Models.Contracts.Patients.PatientCreateDto
                    {
                        Name = "测试患者",
                        PhoneNumber = "13800138000",
                        Gender = LYBT.Shared.Models.Enums.Gender.Male,
                        BirthDate = DateTime.Now.AddYears(-30)
                    };

                    var result = await _patientService.CreateAsync(patientDto);
                    if (result.IsSuccess)
                    {
                        // 创建成功，刷新列表（需要在UI线程执行）
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ExecuteRefresh();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常
                System.Diagnostics.Debug.WriteLine($"快速创建患者失败: {ex.Message}");
            }
        }

        #endregion Shared Service Methods
    }
}
