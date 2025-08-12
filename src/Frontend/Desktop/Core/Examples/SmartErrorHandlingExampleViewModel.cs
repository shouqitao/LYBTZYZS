using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;
using Prism.Commands;
using System.Net;

namespace LYBT.Desktop.Core.Examples
{
    /// <summary>
    /// 智能错误处理示例ViewModel - UltraThink Stage 5.2.2 创新演示
    /// 
    /// 展示增强的错误处理系统的核心功能：
    /// 1. 上下文相关的错误分析和处理
    /// 2. 可执行的一键修复建议
    /// 3. 智能自动错误恢复机制
    /// 4. 与SmartLoadingManager的深度集成
    /// 5. 处方管理业务特定错误处理
    /// </summary>
    public class SmartErrorHandlingExampleViewModel : BindableBase
    {
        #region 私有字段

        private readonly IUserFriendlyErrorService _errorService;
        private readonly ISmartLoadingManager _loadingManager;
        private readonly ILogger<SmartErrorHandlingExampleViewModel> _logger;

        private EnhancedUserFriendlyError? _currentError;
        private string _operationResult = string.Empty;
        private bool _isAutoRecoveryEnabled = true;

        #endregion

        #region 构造函数

        public SmartErrorHandlingExampleViewModel(
            IUserFriendlyErrorService errorService,
            ISmartLoadingManager loadingManager,
            ILogger<SmartErrorHandlingExampleViewModel> logger)
        {
            _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
            _loadingManager = loadingManager ?? throw new ArgumentNullException(nameof(loadingManager));
            _logger = logger;

            InitializeCommands();
            InitializeErrorHandlingDemos();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 加载管理器（供UI绑定）
        /// </summary>
        public ISmartLoadingManager LoadingManager => _loadingManager;

        /// <summary>
        /// 当前错误信息
        /// </summary>
        public EnhancedUserFriendlyError? CurrentError
        {
            get => _currentError;
            private set => SetProperty(ref _currentError, value);
        }

        /// <summary>
        /// 操作结果
        /// </summary>
        public string OperationResult
        {
            get => _operationResult;
            private set => SetProperty(ref _operationResult, value);
        }

        /// <summary>
        /// 是否启用自动恢复
        /// </summary>
        public bool IsAutoRecoveryEnabled
        {
            get => _isAutoRecoveryEnabled;
            set => SetProperty(ref _isAutoRecoveryEnabled, value);
        }

        /// <summary>
        /// 错误历史记录
        /// </summary>
        public ObservableCollection<EnhancedUserFriendlyError> ErrorHistory { get; } = new();

        #endregion

        #region 命令

        public ICommand SimulateNetworkErrorCommand { get; private set; } = null!;
        public ICommand SimulateTimeoutErrorCommand { get; private set; } = null!;
        public ICommand SimulateValidationErrorCommand { get; private set; } = null!;
        public ICommand SimulateUnauthorizedErrorCommand { get; private set; } = null!;
        public ICommand SimulatePrescriptionErrorCommand { get; private set; } = null!;
        public ICommand ExecuteSmartFixCommand { get; private set; } = null!;
        public ICommand TryAutoRecoverCommand { get; private set; } = null!;
        public ICommand ClearErrorHistoryCommand { get; private set; } = null!;

        #endregion

        #region 初始化

        private void InitializeCommands()
        {
            SimulateNetworkErrorCommand = new DelegateCommand(async () => await SimulateNetworkErrorAsync());
            SimulateTimeoutErrorCommand = new DelegateCommand(async () => await SimulateTimeoutErrorAsync());
            SimulateValidationErrorCommand = new DelegateCommand(async () => await SimulateValidationErrorAsync());
            SimulateUnauthorizedErrorCommand = new DelegateCommand(async () => await SimulateUnauthorizedErrorAsync());
            SimulatePrescriptionErrorCommand = new DelegateCommand(async () => await SimulatePrescriptionErrorAsync());
            ExecuteSmartFixCommand = new DelegateCommand<SmartFixAction>(async (action) => await ExecuteSmartFixAsync(action));
            TryAutoRecoverCommand = new DelegateCommand(async () => await TryAutoRecoverAsync());
            ClearErrorHistoryCommand = new DelegateCommand(() => ErrorHistory.Clear());
        }

        private void InitializeErrorHandlingDemos()
        {
            // 注册自定义错误恢复策略
            _errorService.RegisterRecoveryStrategy("prescription_validation", async (ex, ctx) =>
            {
                _logger.LogInformation("尝试修复处方验证错误");
                await Task.Delay(1000); // 模拟修复过程
                
                // 模拟修复成功率80%
                var success = new Random().NextDouble() > 0.2;
                return success;
            });

            // 注册药材相关错误恢复
            _errorService.RegisterRecoveryStrategy("herb_availability", async (ex, ctx) =>
            {
                _logger.LogInformation("检查药材可用性并尝试替代方案");
                await Task.Delay(1500); // 模拟检查过程
                return true; // 假设总能找到替代方案
            });
        }

        #endregion

        #region 错误模拟方法

        /// <summary>
        /// 模拟网络连接错误
        /// </summary>
        private async Task SimulateNetworkErrorAsync()
        {
            using var operation = _loadingManager.StartLoading("network_test", "测试网络连接...", layer: 1);
            
            try
            {
                await Task.Delay(1000); // 模拟操作
                var ex = new ApiCallException("网络连接失败")
                {
                    OperationName = "CheckNetworkConnection",
                    StatusCode = HttpStatusCode.ServiceUnavailable
                };
                throw ex;
            }
            catch (Exception ex)
            {
                await HandleErrorWithContextAsync(ex, new ErrorContext
                {
                    OperationName = "网络连接测试",
                    ModuleName = "Network",
                    EntityType = "Connection"
                });
            }
        }

        /// <summary>
        /// 模拟超时错误
        /// </summary>
        private async Task SimulateTimeoutErrorAsync()
        {
            using var operation = _loadingManager.StartLoading("timeout_test", "执行长时间操作...", layer: 1);
            
            try
            {
                await Task.Delay(2000); // 模拟长时间操作
                throw new TimeoutException("操作超时：服务器响应时间过长");
            }
            catch (Exception ex)
            {
                await HandleErrorWithContextAsync(ex, new ErrorContext
                {
                    OperationName = "数据同步",
                    ModuleName = "Synchronization",
                    RetryCount = 0
                });
            }
        }

        /// <summary>
        /// 模拟数据验证错误
        /// </summary>
        private async Task SimulateValidationErrorAsync()
        {
            using var operation = _loadingManager.StartLoading("validation_test", "验证数据格式...", layer: 1);
            
            try
            {
                await Task.Delay(500);
                throw new ArgumentException("患者姓名不能为空，联系方式格式不正确");
            }
            catch (Exception ex)
            {
                await HandleErrorWithContextAsync(ex, new ErrorContext
                {
                    OperationName = "患者信息保存",
                    ModuleName = "Patients",
                    EntityType = "Patient",
                    EntityId = Guid.NewGuid()
                });
            }
        }

        /// <summary>
        /// 模拟权限错误
        /// </summary>
        private async Task SimulateUnauthorizedErrorAsync()
        {
            using var operation = _loadingManager.StartLoading("auth_test", "检查访问权限...", layer: 1);
            
            try
            {
                await Task.Delay(800);
                var ex = new ApiCallException("访问被拒绝")
                {
                    OperationName = "DeletePrescription",
                    StatusCode = HttpStatusCode.Forbidden
                };
                throw ex;
            }
            catch (Exception ex)
            {
                await HandleErrorWithContextAsync(ex, new ErrorContext
                {
                    OperationName = "删除处方",
                    ModuleName = "Prescriptions",
                    UserId = Guid.NewGuid(),
                    EntityId = Guid.NewGuid(),
                    EntityType = "Prescription"
                });
            }
        }

        /// <summary>
        /// 模拟处方业务特定错误
        /// </summary>
        private async Task SimulatePrescriptionErrorAsync()
        {
            using var operation = _loadingManager.StartLoading("prescription_test", "处理处方信息...", layer: 1);
            
            try
            {
                await Task.Delay(1200);
                
                // 随机选择不同的处方错误类型
                var errorType = new Random().Next(1, 4);
                switch (errorType)
                {
                    case 1:
                        throw new InvalidOperationException("所选药材'黄连'当前库存不足，无法开出所需剂量");
                    case 2:
                        throw new ArgumentException("处方总重量超过单次可配药限制(500g)，请调整药材用量");
                    case 3:
                        throw new InvalidOperationException("检测到药材配伍禁忌：'甘草'与'大戟'不宜同用");
                    default:
                        throw new Exception("处方验证失败：缺少患者诊断信息");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorWithContextAsync(ex, new ErrorContext
                {
                    OperationName = "处方开具",
                    ModuleName = "Prescriptions",
                    EntityType = "Prescription",
                    EntityId = Guid.NewGuid(),
                    Parameters = new Dictionary<string, object>
                    {
                        { "PatientId", Guid.NewGuid() },
                        { "DoctorId", Guid.NewGuid() },
                        { "HerbCount", 8 },
                        { "TotalWeight", "520g" }
                    }
                });
            }
        }

        #endregion

        #region 错误处理方法

        /// <summary>
        /// 使用上下文处理错误
        /// </summary>
        private async Task HandleErrorWithContextAsync(Exception exception, ErrorContext context)
        {
            try
            {
                // 获取增强的错误信息
                var enhancedError = _errorService.GetContextualError(exception, context);
                CurrentError = enhancedError;
                
                // 添加到历史记录
                ErrorHistory.Insert(0, enhancedError);
                if (ErrorHistory.Count > 10) // 限制历史记录数量
                {
                    ErrorHistory.RemoveAt(ErrorHistory.Count - 1);
                }

                // 设置智能修复命令
                foreach (var fixAction in enhancedError.SmartFixActions)
                {
                    fixAction.Command = new DelegateCommand(async () => await ExecuteSmartFixAsync(fixAction));
                }

                _logger.LogWarning("处理增强错误 - 操作: {Operation}, 可自动恢复: {CanRecover}, 修复建议数: {FixCount}",
                    context.OperationName, enhancedError.CanAutoRecover, enhancedError.SmartFixActions.Count);

                // 如果可以自动恢复且启用了自动恢复
                if (IsAutoRecoveryEnabled && enhancedError.CanAutoRecover)
                {
                    OperationResult = $"检测到可恢复错误，{enhancedError.EstimatedRecoveryTime.TotalSeconds:F0}秒后尝试自动恢复...";
                    
                    // 延迟后尝试自动恢复
                    await Task.Delay(enhancedError.EstimatedRecoveryTime);
                    await TryAutoRecoverAsync();
                }
                else
                {
                    OperationResult = $"错误已记录，提供 {enhancedError.SmartFixActions.Count} 个修复建议";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理错误时发生异常");
                OperationResult = "错误处理过程中发生异常";
            }
        }

        /// <summary>
        /// 执行智能修复操作
        /// </summary>
        private async Task ExecuteSmartFixAsync(SmartFixAction? fixAction)
        {
            if (fixAction == null || CurrentError?.ErrorContext == null) return;

            using var operation = _loadingManager.StartLoading("smart_fix", $"执行修复操作：{fixAction.Title}", layer: 2);
            
            try
            {
                _logger.LogInformation("执行智能修复 - 类型: {ActionType}, 标题: {Title}", fixAction.ActionType, fixAction.Title);

                // 模拟修复操作执行时间
                await Task.Delay(fixAction.EstimatedDuration);

                switch (fixAction.ActionType)
                {
                    case FixActionType.Retry:
                        OperationResult = "已重试操作，请检查结果";
                        break;
                    
                    case FixActionType.NetworkCheck:
                        OperationResult = "网络连接检查完成，连接正常";
                        break;
                    
                    case FixActionType.RetryWithTimeout:
                        OperationResult = "使用更长超时时间重试完成";
                        break;
                    
                    case FixActionType.OpenEditor:
                        OperationResult = "已打开编辑界面，请检查并修正数据";
                        break;
                    
                    case FixActionType.Relogin:
                        OperationResult = "正在跳转到登录页面...";
                        break;
                    
                    case FixActionType.ClearCache:
                        OperationResult = "缓存已清理，请重新尝试操作";
                        break;
                    
                    default:
                        OperationResult = $"修复操作 {fixAction.Title} 已执行";
                        break;
                }

                // 修复成功后清除当前错误
                if (new Random().NextDouble() > 0.3) // 70% 成功率
                {
                    CurrentError = null;
                    OperationResult += " - 修复成功！";
                }
                else
                {
                    OperationResult += " - 修复未完全成功，可能需要其他操作";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行智能修复时发生错误");
                OperationResult = $"修复操作失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 尝试自动恢复
        /// </summary>
        private async Task TryAutoRecoverAsync()
        {
            if (CurrentError?.ErrorContext == null) return;

            try
            {
                var recoveryResult = await _errorService.TryAutoRecoverAsync(
                    new Exception(CurrentError.TechnicalDetails ?? "Unknown error"), 
                    CurrentError.ErrorContext);

                if (recoveryResult.IsSuccessful)
                {
                    OperationResult = $"自动恢复成功：{recoveryResult.Message}";
                    CurrentError = null;
                    _logger.LogInformation("自动错误恢复成功 - 策略: {Strategy}", recoveryResult.RecoveryStrategy);
                }
                else
                {
                    OperationResult = $"自动恢复失败：{recoveryResult.Message}";
                    _logger.LogWarning("自动错误恢复失败 - 原因: {Message}", recoveryResult.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动恢复过程中发生异常");
                OperationResult = "自动恢复过程中发生异常";
            }
        }

        #endregion
    }
}