using LYBT.Desktop.Infrastructure.Interfaces.Components;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.Components
{
    /// <summary>
    /// 用户命令处理器 - 业务逻辑协调者
    /// Issue #1779: Users模块组件化改造
    ///
    /// 职责:
    /// - 协调DataManager和Validator执行业务操作
    /// - 编辑、重置密码、切换状态命令
    /// - 导航命令（返回列表、编辑页面）
    /// </summary>
    public class UserCommandHandler : ICommandHandler
    {
        #region 字段

        private readonly UserDataManager _dataManager;
        private readonly UserValidator _validator;
        private readonly ILogger<UserCommandHandler> _logger;
        private readonly IRegionManager _regionManager;
        private readonly Dictionary<string, Func<object?, Task<bool>>> _commands;
        private readonly Dictionary<string, Func<bool>> _canExecuteHandlers;

        #endregion

        #region 构造函数

        public UserCommandHandler(
            UserDataManager dataManager,
            UserValidator validator,
            ILogger<UserCommandHandler> logger,
            IRegionManager regionManager)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            _commands = new Dictionary<string, Func<object?, Task<bool>>>();
            _canExecuteHandlers = new Dictionary<string, Func<bool>>();
        }

        #endregion

        #region ICommandHandler实现

        /// <summary>
        /// 注册命令处理器
        /// </summary>
        public void RegisterCommand(string commandName, Func<object?, Task<bool>> handler)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentException("命令名称不能为空", nameof(commandName));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _commands[commandName] = handler;
            _logger.LogDebug("命令已注册: {CommandName}", commandName);
        }

        /// <summary>
        /// 注册命令可执行条件处理器
        /// </summary>
        public void RegisterCanExecute(string commandName, Func<bool> canExecute)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentException("命令名称不能为空", nameof(commandName));

            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));

            _canExecuteHandlers[commandName] = canExecute;
            _logger.LogDebug("命令可执行条件已注册: {CommandName}", commandName);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public async Task<bool> ExecuteAsync(string commandName, object? parameter = null)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                _logger.LogWarning("命令名称为空,无法执行");
                return false;
            }

            if (!_commands.ContainsKey(commandName))
            {
                _logger.LogWarning("未找到命令: {CommandName}", commandName);
                return false;
            }

            try
            {
                _logger.LogDebug("执行命令: {CommandName}", commandName);
                return await _commands[commandName](parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行命令失败: {CommandName}", commandName);
                return false;
            }
        }

        /// <summary>
        /// 检查命令是否可执行
        /// </summary>
        public bool CanExecute(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return false;

            if (!_canExecuteHandlers.ContainsKey(commandName))
                return true; // 默认可执行

            return _canExecuteHandlers[commandName]();
        }

        #endregion

        #region 通用命令方法

        /// <summary>
        /// 重新加载用户数据
        /// </summary>
        public async Task<bool> ReloadAsync()
        {
            try
            {
                _logger.LogInformation("重新加载用户数据");
                await _dataManager.ReloadAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新加载用户数据失败");
                return false;
            }
        }

        #endregion

        #region 专用命令方法

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        public async Task<bool> ToggleStatusAsync()
        {
            try
            {
                _logger.LogInformation("开始切换用户状态");

                if (!_validator.CanToggleStatus(out var errorMessage))
                {
                    _logger.LogWarning("切换状态验证失败: {ErrorMessage}", errorMessage);
                    return false;
                }

                return await _dataManager.ToggleStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换用户状态失败");
                return false;
            }
        }

        /// <summary>
        /// 重置密码（占位符，未来实现）
        /// </summary>
        public async Task<bool> ResetPasswordAsync()
        {
            try
            {
                _logger.LogInformation("重置密码功能开发中...");

                if (!_validator.CanResetPassword(out var errorMessage))
                {
                    _logger.LogWarning("重置密码验证失败: {ErrorMessage}", errorMessage);
                    return false;
                }

                // TODO: 实现重置密码逻辑
                // 1. 打开 ResetPasswordDialog
                // 2. 调用API重置密码
                // 3. 更新UI状态

                await Task.CompletedTask;
                return false; // 暂未实现
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码失败");
                return false;
            }
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到用户列表页
        /// </summary>
        public async Task<bool> NavigateToUserListAsync()
        {
            try
            {
                _logger.LogInformation("导航到用户列表页");
                _regionManager.RequestNavigate("AdminContentRegion", "UserListView");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到用户列表页失败");
                return false;
            }
        }

        /// <summary>
        /// 导航到用户编辑页
        /// </summary>
        public async Task<bool> NavigateToUserEditAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("导航到用户编辑页, UserId={UserId}", userId);

                var parameters = new NavigationParameters
                {
                    { "UserId", userId }
                };

                _regionManager.RequestNavigate("AdminContentRegion", "UserEditView", parameters);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到用户编辑页失败");
                return false;
            }
        }

        /// <summary>
        /// 返回上一页（通用）
        /// </summary>
        public async Task<bool> GoBackAsync(string regionName = "AdminContentRegion")
        {
            try
            {
                _logger.LogInformation("返回上一页, Region={Region}", regionName);

                var journal = _regionManager.Regions[regionName]?.NavigationService?.Journal;
                if (journal != null && journal.CanGoBack)
                {
                    journal.GoBack();
                }

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "返回上一页失败");
                return false;
            }
        }

        #endregion
    }
}
