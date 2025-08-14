using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Extensions;
using LYBT.Desktop.Core.Models;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Examples
{
    /// <summary>
    /// SmartLoadingManager使用示例ViewModel
    /// 展示各种实际应用场景的最佳实践
    /// </summary>
    public class SmartLoadingExampleViewModel : BindableBase
    {
        #region 私有字段

        private readonly ISmartLoadingManager _loadingManager;
        private readonly ILogger<SmartLoadingExampleViewModel> _logger;
        private CancellationTokenSource? _currentOperationCancellation;

        #endregion

        #region 构造函数

        public SmartLoadingExampleViewModel(
            ISmartLoadingManager loadingManager,
            ILogger<SmartLoadingExampleViewModel> logger)
        {
            _loadingManager = loadingManager ?? throw new ArgumentNullException(nameof(loadingManager));
            _logger = logger;

            InitializeCommands();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 加载管理器（供UI绑定）
        /// </summary>
        public ISmartLoadingManager LoadingManager => _loadingManager;

        #endregion

        #region 命令

        public ICommand SimpleLoadingCommand { get; private set; } = null!;
        public ICommand ProgressLoadingCommand { get; private set; } = null!;
        public ICommand BatchProcessingCommand { get; private set; } = null!;
        public ICommand MultiStepOperationCommand { get; private set; } = null!;
        public ICommand LayeredLoadingCommand { get; private set; } = null!;
        public ICommand CancelOperationCommand { get; private set; } = null!;

        #endregion

        #region 私有方法

        private void InitializeCommands()
        {
            SimpleLoadingCommand = new RelayCommand(async () => await ExecuteSimpleLoading(), () => !_loadingManager.IsGlobalLoading);
            ProgressLoadingCommand = new RelayCommand(async () => await ExecuteProgressLoading(), () => !_loadingManager.IsGlobalLoading);
            BatchProcessingCommand = new RelayCommand(async () => await ExecuteBatchProcessing(), () => !_loadingManager.IsGlobalLoading);
            MultiStepOperationCommand = new RelayCommand(async () => await ExecuteMultiStepOperation(), () => !_loadingManager.IsGlobalLoading);
            LayeredLoadingCommand = new RelayCommand(async () => await ExecuteLayeredLoading(), () => !_loadingManager.IsGlobalLoading);
            CancelOperationCommand = new RelayCommand(() => CancelCurrentOperation(), () => _loadingManager.IsGlobalLoading);
        }

        #endregion

        #region 示例操作

        /// <summary>
        /// 示例1：简单加载操作
        /// </summary>
        private async Task ExecuteSimpleLoading()
        {
            _currentOperationCancellation = new CancellationTokenSource();

            try
            {
                var result = await _loadingManager.ExecuteWithLoadingAsync(
                    "simple_operation",
                    async (cancellationToken) =>
                    {
                        // 模拟API调用
                        await Task.Delay(2000, cancellationToken);
                        return "操作完成！";
                    },
                    "正在执行简单操作...",
                    layer: 1,
                    _currentOperationCancellation.Token
                );

                _logger.LogInformation("简单操作完成: {Result}", result);
                // 这里可以更新UI或显示结果
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("简单操作被用户取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "简单操作执行失败");
                // 这里可以显示错误消息
            }
        }

        /// <summary>
        /// 示例2：带进度的加载操作
        /// </summary>
        private async Task ExecuteProgressLoading()
        {
            _currentOperationCancellation = new CancellationTokenSource();

            try
            {
                var result = await _loadingManager.ExecuteWithProgressAsync(
                    "progress_operation",
                    async (progress, cancellationToken) =>
                    {
                        // 模拟分阶段处理
                        for (int i = 1; i <= 10; i++)
                        {
                            await Task.Delay(300, cancellationToken);
                            
                            var percentage = i * 10;
                            progress.Report(new ProgressInfo(percentage, $"处理步骤 {i}/10"));
                        }

                        return "带进度的操作完成！";
                    },
                    "正在执行带进度操作...",
                    layer: 1,
                    _currentOperationCancellation.Token
                );

                _logger.LogInformation("带进度操作完成: {Result}", result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("带进度操作被用户取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "带进度操作执行失败");
            }
        }

        /// <summary>
        /// 示例3：批量处理操作
        /// </summary>
        private async Task ExecuteBatchProcessing()
        {
            _currentOperationCancellation = new CancellationTokenSource();

            try
            {
                // 模拟要处理的数据项
                var items = new[] { "项目1", "项目2", "项目3", "项目4", "项目5" };

                var results = await _loadingManager.ExecuteBatchWithProgressAsync(
                    "batch_operation",
                    items,
                    async (item, cancellationToken) =>
                    {
                        // 模拟处理单个项目
                        await Task.Delay(800, cancellationToken);
                        return $"{item} 处理完成";
                    },
                    "批量处理项目中...",
                    layer: 1,
                    _currentOperationCancellation.Token
                );

                _logger.LogInformation("批量处理完成，共处理 {Count} 个项目", results.Length);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("批量处理被用户取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量处理执行失败");
            }
        }

        /// <summary>
        /// 示例4：多步骤操作
        /// </summary>
        private async Task ExecuteMultiStepOperation()
        {
            _currentOperationCancellation = new CancellationTokenSource();

            try
            {
                var steps = new[]
                {
                    new MultiStepOperation("初始化数据", async (ct) => 
                    {
                        await Task.Delay(1000, ct);
                        return "数据初始化完成";
                    }),
                    new MultiStepOperation("验证权限", async (ct) => 
                    {
                        await Task.Delay(800, ct);
                        return "权限验证通过";
                    }),
                    new MultiStepOperation("处理业务逻辑", async (ct) => 
                    {
                        await Task.Delay(1500, ct);
                        return "业务逻辑处理完成";
                    }),
                    new MultiStepOperation("保存结果", async (ct) => 
                    {
                        await Task.Delay(600, ct);
                        return "结果保存成功";
                    })
                };

                var results = await _loadingManager.ExecuteMultiStepAsync(
                    "multi_step_operation",
                    steps,
                    "执行复杂业务流程...",
                    layer: 1,
                    _currentOperationCancellation.Token
                );

                _logger.LogInformation("多步骤操作完成，共执行 {Count} 个步骤", results.Length);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("多步骤操作被用户取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "多步骤操作执行失败");
            }
        }

        /// <summary>
        /// 示例5：分层加载操作（模拟嵌套场景）
        /// </summary>
        private async Task ExecuteLayeredLoading()
        {
            _currentOperationCancellation = new CancellationTokenSource();

            try
            {
                // 主操作（层级1）
                await _loadingManager.ExecuteWithLoadingAsync(
                    "main_operation",
                    async (cancellationToken) =>
                    {
                        // 在主操作中执行子操作（层级2）
                        await _loadingManager.ExecuteWithLoadingAsync(
                            "sub_operation_1",
                            async (subCt) =>
                            {
                                await Task.Delay(1000, subCt);
                            },
                            "执行子操作1...",
                            layer: 2,
                            cancellationToken
                        );

                        // 另一个子操作（层级2）
                        await _loadingManager.ExecuteWithLoadingAsync(
                            "sub_operation_2",
                            async (subCt) =>
                            {
                                await Task.Delay(1200, subCt);
                            },
                            "执行子操作2...",
                            layer: 2,
                            cancellationToken
                        );

                        await Task.Delay(500, cancellationToken);
                    },
                    "执行主操作（包含子操作）...",
                    layer: 1,
                    _currentOperationCancellation.Token
                );

                _logger.LogInformation("分层加载操作完成");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("分层加载操作被用户取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分层加载操作执行失败");
            }
        }

        /// <summary>
        /// 取消当前操作
        /// </summary>
        private void CancelCurrentOperation()
        {
            _currentOperationCancellation?.Cancel();
            _logger.LogInformation("用户请求取消当前操作");
        }

        #endregion

        #region 清理资源

        public void Dispose()
        {
            _currentOperationCancellation?.Cancel();
            _currentOperationCancellation?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 简单的RelayCommand实现（如果项目中没有的话）
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _asyncExecute;
        private readonly Func<bool> _canExecute;
        private readonly Action? _syncExecute;

        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _asyncExecute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _syncExecute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute();

        public async void Execute(object? parameter)
        {
            if (_asyncExecute != null)
            {
                await _asyncExecute();
            }
            else
            {
                _syncExecute?.Invoke();
            }
        }
    }
}