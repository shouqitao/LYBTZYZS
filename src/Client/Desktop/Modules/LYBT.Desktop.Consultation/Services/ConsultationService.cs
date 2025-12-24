using LYBT.Desktop.Contracts.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊断Service - 业务逻辑协调者
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    ///
    /// 职责:
    /// - 协调DataManager和Validator执行业务操作
    /// - 保存命令（保存草稿）
    /// - 清空表单命令
    /// - 导航命令
    /// </summary>
    public class ConsultationService : ICommandHandler
    {
        #region 字段

        private readonly IMedicalCaseService _dataManager;
        private readonly ConsultationValidator _validator;
        private readonly ILogger<ConsultationService> _logger;
        private readonly IRegionManager _regionManager;
        private readonly Dictionary<string, Func<object?, Task<bool>>> _commands;
        private readonly Dictionary<string, Func<bool>> _canExecuteHandlers;

        #endregion

        #region 构造函数

        public ConsultationService(
            IMedicalCaseService dataManager,
            ConsultationValidator validator,
            ILogger<ConsultationService> logger,
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
        /// 保存诊断数据（带验证）
        /// OpenSpec: simplify-medicalcase-api - 通过聚合根保存
        /// </summary>
        public async Task<bool> SaveAsync(bool validate = true)
        {
            try
            {
                _logger.LogInformation("开始保存诊断数据, 验证={Validate}", validate);

                if (validate)
                {
                    if (!_validator.IsValid(out var errorMessage))
                    {
                        _logger.LogWarning("验证失败: {ErrorMessage}", errorMessage);
                        return false;
                    }
                }

                return await _dataManager.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存诊断数据失败");
                return false;
            }
        }

        /// <summary>
        /// 重新加载诊断数据
        /// OpenSpec: simplify-medicalcase-api - 通过聚合根重新加载
        /// </summary>
        public async Task<bool> ReloadAsync()
        {
            try
            {
                _logger.LogInformation("重新加载诊断数据");
                await _dataManager.ReloadAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新加载诊断数据失败");
                return false;
            }
        }

        #endregion

        #region 专用命令方法

        /// <summary>
        /// 清空表单
        /// OpenSpec: simplify-medicalcase-api - 直接修改聚合根的CurrentConsultation属性
        /// </summary>
        public void ClearForm()
        {
            try
            {
                _logger.LogInformation("执行清空表单");

                var consultation = _dataManager.CurrentConsultation;
                if (consultation == null)
                {
                    _logger.LogWarning("当前诊断数据为空，无法清空");
                    return;
                }

                // 清空所有字段 - 直接修改属性
                // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
                consultation.PresentIllness = null;
                consultation.TongueDiagnosis = null;
                consultation.PulseDiagnosis = null;
                consultation.TCMDiagnosis = string.Empty;

                _logger.LogInformation("表单已清空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空表单失败");
            }
        }

        /// <summary>
        /// 保存草稿
        /// OpenSpec: simplify-medicalcase-api - 通过聚合根保存
        /// </summary>
        public async Task<bool> SaveDraftAsync()
        {
            try
            {
                _logger.LogInformation("开始保存诊断草稿");

                // 通过聚合根保存数据
                return await _dataManager.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存诊断草稿失败");
                return false;
            }
        }

        #endregion
    }
}
