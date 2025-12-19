using LYBT.Desktop.Infrastructure.Interfaces.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Components
{
    /// <summary>
    /// 命令处理器基类
    /// OpenSpec: optimize-desktop-code-reuse Phase 2 - 提取通用的Register/Execute/CanExecute逻辑
    ///
    /// 职责:
    /// - 命令注册管理
    /// - 统一的命令执行框架
    /// - 异常处理标准化
    /// - 可执行状态检查
    /// </summary>
    public abstract class CommandHandlerBase : ICommandHandler
    {
        #region 字段

        protected readonly ILogger Logger;
        private readonly Dictionary<string, Func<object?, Task<bool>>> _commands = new();
        private readonly Dictionary<string, Func<bool>> _canExecuteHandlers = new();

        #endregion

        #region 构造函数

        protected CommandHandlerBase(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RegisterCommands();
        }

        #endregion

        #region ICommandHandler实现

        /// <summary>
        /// 执行命令
        /// </summary>
        public async Task<bool> ExecuteAsync(string commandName, object? parameter = null)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                Logger.LogWarning("{HandlerName} 命令名称不能为空", GetType().Name);
                return false;
            }

            if (!_commands.TryGetValue(commandName, out var handler))
            {
                Logger.LogWarning("{HandlerName} 未找到命令: {CommandName}", GetType().Name, commandName);
                return false;
            }

            try
            {
                Logger.LogDebug("{HandlerName} 开始执行命令: {CommandName}", GetType().Name, commandName);

                var result = await handler(parameter);

                Logger.LogDebug("{HandlerName} 命令执行完成: {CommandName}, 结果: {Result}",
                    GetType().Name, commandName, result);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{HandlerName} 执行命令失败: {CommandName}", GetType().Name, commandName);
                return false;
            }
        }

        /// <summary>
        /// 检查命令是否可执行
        /// </summary>
        public bool CanExecute(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return false;
            }

            // 检查命令是否注册
            if (!_commands.ContainsKey(commandName))
            {
                return false;
            }

            // 如果有自定义的CanExecute处理器，使用它
            if (_canExecuteHandlers.TryGetValue(commandName, out var canExecuteHandler))
            {
                try
                {
                    return canExecuteHandler();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "{HandlerName} CanExecute检查失败: {CommandName}", GetType().Name, commandName);
                    return false;
                }
            }

            // 默认可执行
            return true;
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 注册命令（子类必须实现）
        /// 在此方法中调用RegisterCommand注册所有支持的命令
        /// </summary>
        protected abstract void RegisterCommands();

        #endregion

        #region 命令注册方法

        /// <summary>
        /// 注册命令
        /// </summary>
        protected void RegisterCommand(string commandName, Func<object?, Task<bool>> handler)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                throw new ArgumentException("命令名称不能为空", nameof(commandName));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _commands[commandName] = handler;
            Logger.LogTrace("{HandlerName} 注册命令: {CommandName}", GetType().Name, commandName);
        }

        /// <summary>
        /// 注册命令及其CanExecute处理器
        /// </summary>
        protected void RegisterCommand(string commandName, Func<object?, Task<bool>> handler, Func<bool> canExecute)
        {
            RegisterCommand(commandName, handler);

            if (canExecute != null)
            {
                _canExecuteHandlers[commandName] = canExecute;
            }
        }

        /// <summary>
        /// 注册同步命令（会自动包装为异步）
        /// </summary>
        protected void RegisterCommand(string commandName, Func<object?, bool> handler)
        {
            RegisterCommand(commandName, param => Task.FromResult(handler(param)));
        }

        /// <summary>
        /// 注册无参数命令
        /// </summary>
        protected void RegisterCommand(string commandName, Func<Task<bool>> handler)
        {
            RegisterCommand(commandName, _ => handler());
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取已注册的命令列表
        /// </summary>
        public IReadOnlyCollection<string> GetRegisteredCommands()
        {
            return _commands.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// 检查命令是否已注册
        /// </summary>
        public bool IsCommandRegistered(string commandName)
        {
            return _commands.ContainsKey(commandName);
        }

        #endregion
    }
}
