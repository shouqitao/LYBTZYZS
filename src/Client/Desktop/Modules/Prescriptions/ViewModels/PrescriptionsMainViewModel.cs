using System;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Prescriptions.Views;
using LYBT.Desktop.Prescriptions.ViewModels;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方模块主视图模型 - UltraThink v2.0
    /// 职责：
    /// 1. 接收导航参数（MedicalCaseId）
    /// 2. 管理工作流模式（处方编辑/历史管理）
    /// 3. 处理模块间通信
    /// </summary>
    public class PrescriptionsMainViewModel : BindableBase, INavigationAware
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<PrescriptionsMainViewModel> _logger;

        #endregion

        #region 属性

        private Guid _currentMedicalCaseId = Guid.Empty;
        private bool _isLoading;
        private string _loadingMessage = "正在加载...";
        private object _currentWorkflowContent;

        /// <summary>当前医疗案例ID</summary>
        public Guid CurrentMedicalCaseId
        {
            get => _currentMedicalCaseId;
            set 
            { 
                if (SetProperty(ref _currentMedicalCaseId, value))
                {
                    RaisePropertyChanged(nameof(HasMedicalCase));
                    LoadWorkflowContent();
                }
            }
        }

        /// <summary>是否有关联的医疗案例</summary>
        public bool HasMedicalCase => CurrentMedicalCaseId != Guid.Empty;

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>加载消息</summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>当前工作流内容</summary>
        public object CurrentWorkflowContent
        {
            get => _currentWorkflowContent;
            set => SetProperty(ref _currentWorkflowContent, value);
        }

        #endregion

        #region 命令

        public ICommand SwitchToManagementCommand { get; }
        public ICommand CreateNewPrescriptionCommand { get; }
        public ICommand ReturnToSourceCommand { get; }

        #endregion

        public PrescriptionsMainViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ICustomDialogService dialogService,
            ILogger<PrescriptionsMainViewModel> logger)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _dialogService = dialogService;
            _logger = logger;

            // 初始化命令
            SwitchToManagementCommand = new DelegateCommand(ExecuteSwitchToManagement);
            CreateNewPrescriptionCommand = new DelegateCommand(ExecuteCreateNewPrescription);
            ReturnToSourceCommand = new DelegateCommand(ExecuteReturnToSource);

            _logger.LogInformation("处方模块主视图模型已初始化");
        }

        #region INavigationAware 实现

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _logger.LogInformation("导航到处方模块，参数: {Parameters}", 
                string.Join(", ", navigationContext.Parameters.Keys.Select(key => $"{key}={navigationContext.Parameters[key]}")));

            // 接收MedicalCaseId参数
            if (navigationContext.Parameters.TryGetValue<object>("MedicalCaseId", out var medicalCaseIdParam))
            {
                if (medicalCaseIdParam is Guid medicalCaseId && medicalCaseId != Guid.Empty)
                {
                    _logger.LogInformation("接收到医疗案例ID: {MedicalCaseId}", medicalCaseId);
                    CurrentMedicalCaseId = medicalCaseId;
                }
                else if (Guid.TryParse(medicalCaseIdParam?.ToString(), out var parsedId) && parsedId != Guid.Empty)
                {
                    _logger.LogInformation("解析医疗案例ID: {MedicalCaseId}", parsedId);
                    CurrentMedicalCaseId = parsedId;
                }
                else
                {
                    _logger.LogWarning("无效的医疗案例ID参数: {Parameter}", medicalCaseIdParam);
                }
            }
            else
            {
                _logger.LogInformation("未接收到医疗案例ID参数，切换到管理模式");
                // 没有医疗案例ID，显示历史管理界面
                LoadManagementWorkflow();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true; // 总是允许导航
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _logger.LogInformation("从处方模块导航离开");
        }

        #endregion

        #region 私有方法

        /// <summary>加载工作流内容</summary>
        private async void LoadWorkflowContent()
        {
            if (!HasMedicalCase)
            {
                CurrentWorkflowContent = null;
                return;
            }

            try
            {
                IsLoading = true;
                LoadingMessage = "正在加载处方编辑器...";

                _logger.LogInformation("开始加载处方工作流，医疗案例ID: {MedicalCaseId}", CurrentMedicalCaseId);

                // 创建处方编辑视图
                var prescriptionView = new PrescriptionView();
                
                // 设置数据上下文（如果ViewModel支持MedicalCaseId参数）
                // Note: Legacy PrescriptionViewModel removed, consider using PrescriptionComposerViewModel
                // if (prescriptionView.DataContext is PrescriptionViewModel prescriptionViewModel)
                // {
                //     prescriptionViewModel.MedicalCaseId = CurrentMedicalCaseId;
                //     _logger.LogInformation("已设置处方视图模型的医疗案例ID: {MedicalCaseId}", CurrentMedicalCaseId);
                // }
                _logger.LogInformation("处方视图已创建，医疗案例ID: {MedicalCaseId}", CurrentMedicalCaseId);

                CurrentWorkflowContent = prescriptionView;
                
                // 发布医疗案例选择事件
                _eventAggregator.GetEvent<MedicalCaseSelectedEvent>()
                    .Publish(new MedicalCaseSelectedEventArgs(CurrentMedicalCaseId));

                _logger.LogInformation("处方工作流加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载处方工作流时发生错误");
                await _dialogService.ShowErrorAsync($"加载处方编辑器失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>加载管理工作流</summary>
        private async void LoadManagementWorkflow()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "正在加载处方管理...";

                _logger.LogInformation("加载处方管理工作流");

                var managementView = new PrescriptionManagementView();
                CurrentWorkflowContent = managementView;

                _logger.LogInformation("处方管理工作流加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载处方管理工作流时发生错误");
                await _dialogService.ShowErrorAsync($"加载处方管理失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 命令实现

        private void ExecuteSwitchToManagement()
        {
            _logger.LogInformation("切换到处方管理模式");
            CurrentMedicalCaseId = Guid.Empty; // 清除医疗案例ID
            LoadManagementWorkflow();
        }

        private async void ExecuteCreateNewPrescription()
        {
            _logger.LogInformation("创建新处方");
            
            try
            {
                // 显示患者选择对话框或直接创建
                var result = await _dialogService.ShowConfirmationAsync(
                    "创建新处方需要选择患者，是否继续？", 
                    "创建新处方");
                
                if (result)
                {
                    // TODO: 实现患者选择逻辑
                    // 这里可以导航到患者选择界面或显示患者选择对话框
                    await _dialogService.ShowInformationAsync(
                        "患者选择功能正在开发中...", 
                        "提示");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建新处方时发生错误");
                await _dialogService.ShowErrorAsync($"创建新处方失败：{ex.Message}", "错误");
            }
        }

        private void ExecuteReturnToSource()
        {
            _logger.LogInformation("返回到源模块");
            
            try
            {
                // 发布返回事件
                _eventAggregator.GetEvent<ModuleNavigationEvent>()
                    .Publish(new ModuleNavigationEventArgs 
                    { 
                        SourceModule = "Prescriptions",
                        TargetModule = "Consultation",
                        Data = CurrentMedicalCaseId 
                    });

                // 导航回看诊模块
                var navigationParameters = new NavigationParameters();
                if (HasMedicalCase)
                {
                    navigationParameters.Add("MedicalCaseId", CurrentMedicalCaseId);
                }

                _regionManager.RequestNavigate("MainContentRegion", "ConsultationMainView", navigationParameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "返回源模块时发生错误: {Error}", ex.Message);
            }
        }

        #endregion
    }

    /// <summary>模块导航事件参数</summary>
    public class ModuleNavigationEventArgs
    {
        public string SourceModule { get; set; } = string.Empty;
        public string TargetModule { get; set; } = string.Empty;
        public object Data { get; set; } = null!;
    }

    /// <summary>模块导航事件</summary>
    public class ModuleNavigationEvent : PubSubEvent<ModuleNavigationEventArgs>
    {
    }
}