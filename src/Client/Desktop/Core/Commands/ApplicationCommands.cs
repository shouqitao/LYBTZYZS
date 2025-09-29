using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Core.Commands
{
    /// <summary>
    /// 全局应用程序命令实现
    /// 提供跨模块的命令协调能力
    /// </summary>
    public class ApplicationCommands : IApplicationCommands
    {
        private readonly ILogger<ApplicationCommands> _logger;

        public CompositeCommand SaveAllCommand { get; }
        public CompositeCommand RefreshAllCommand { get; }
        public CompositeCommand ValidateAllCommand { get; }
        public CompositeCommand PrintCommand { get; }
        public CompositeCommand ExportCommand { get; }
        public CompositeCommand SwitchWorkbenchCommand { get; }
        public CompositeCommand CloseAllCommand { get; }
        public CompositeCommand UndoCommand { get; }
        public CompositeCommand RedoCommand { get; }

        public ApplicationCommands(ILogger<ApplicationCommands> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化所有CompositeCommand
            SaveAllCommand = new CompositeCommand();
            RefreshAllCommand = new CompositeCommand();
            ValidateAllCommand = new CompositeCommand();
            PrintCommand = new CompositeCommand();
            ExportCommand = new CompositeCommand();
            SwitchWorkbenchCommand = new CompositeCommand();
            CloseAllCommand = new CompositeCommand();
            UndoCommand = new CompositeCommand();
            RedoCommand = new CompositeCommand();

            // 监听命令执行（用于日志记录）
            RegisterCommandMonitoring();

            _logger.LogInformation("全局命令系统初始化完成");
        }

        /// <summary>
        /// 注册命令监控，用于日志记录和调试
        /// </summary>
        private void RegisterCommandMonitoring()
        {
            // 监控保存命令
            SaveAllCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局保存命令被触发");
            }));

            // 监控刷新命令
            RefreshAllCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局刷新命令被触发");
            }));

            // 监控验证命令
            ValidateAllCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局验证命令被触发");
            }));

            // 监控打印命令
            PrintCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局打印命令被触发");
            }));

            // 监控导出命令
            ExportCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局导出命令被触发");
            }));

            // 监控工作台切换命令
            SwitchWorkbenchCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("工作台切换命令被触发");
            }));

            // 监控关闭命令
            CloseAllCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局关闭命令被触发");
            }));

            // 监控撤销命令
            UndoCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局撤销命令被触发");
            }));

            // 监控重做命令
            RedoCommand.RegisterCommand(new DelegateCommand(() =>
            {
                _logger.LogDebug("全局重做命令被触发");
            }));
        }
    }
}