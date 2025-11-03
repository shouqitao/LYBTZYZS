using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.Components
{
    /// <summary>
    /// 病案命令处理器 - 业务逻辑协调者
    /// Issue #1778: MedicalCase模块组件化改造
    ///
    /// 职责:
    /// - 协调DataManager和Validator执行业务操作
    /// - 提供通用CRUD命令（保存、删除、重新加载）
    /// - 处方管理命令（创建、更新、删除）
    /// - 导航命令
    /// </summary>
    public class MedicalCaseCommandHandler : ICommandHandler
    {
        #region 字段

        private readonly MedicalCaseDataManager _dataManager;
        private readonly MedicalCaseValidator _validator;
        private readonly ILogger<MedicalCaseCommandHandler> _logger;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, Func<object?, Task<bool>>> _commands;
        private readonly Dictionary<string, Func<bool>> _canExecuteHandlers;

        #endregion

        #region 构造函数

        public MedicalCaseCommandHandler(
            MedicalCaseDataManager dataManager,
            MedicalCaseValidator validator,
            ILogger<MedicalCaseCommandHandler> logger,
            IRegionManager regionManager,
            IEventAggregator eventAggregator)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

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
                _logger.LogInformation("开始执行命令: {CommandName}", commandName);
                var result = await _commands[commandName](parameter);
                _logger.LogInformation("命令执行完成: {CommandName}, 结果: {Result}", commandName, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "命令执行失败: {CommandName}", commandName);
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

            // 如果注册了CanExecute处理器,则使用它
            if (_canExecuteHandlers.ContainsKey(commandName))
            {
                return _canExecuteHandlers[commandName]();
            }

            // 默认可执行
            return true;
        }

        #endregion

        #region 通用数据操作命令

        /// <summary>
        /// 保存病案聚合根数据（病案+诊疗+处方）
        /// </summary>
        /// <param name="validateBeforeSave">保存前是否验证</param>
        /// <returns>是否保存成功</returns>
        public async Task<bool> SaveAsync(bool validateBeforeSave = true)
        {
            try
            {
                _logger.LogInformation("开始保存病案聚合根数据, 保存前验证: {ValidateBeforeSave}", validateBeforeSave);

                // 1. 可选验证
                if (validateBeforeSave)
                {
                    if (!_validator.IsValid(out var errorMessage))
                    {
                        _logger.LogWarning("病案数据验证失败: {ErrorMessage}", errorMessage);
                        return false;
                    }
                }

                // 2. 保存数据
                var result = await _dataManager.SaveAsync();
                if (result)
                {
                    _logger.LogInformation("病案聚合根数据保存成功");
                }
                else
                {
                    _logger.LogWarning("病案聚合根数据保存失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存病案聚合根数据失败");
                return false;
            }
        }

        /// <summary>
        /// 删除病案数据
        /// </summary>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteAsync()
        {
            try
            {
                _logger.LogInformation("开始删除病案数据");
                var result = await _dataManager.DeleteAsync();

                if (result)
                {
                    _logger.LogInformation("病案数据删除成功");
                }
                else
                {
                    _logger.LogWarning("病案数据删除失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除病案数据失败");
                return false;
            }
        }

        /// <summary>
        /// 重新加载病案数据
        /// </summary>
        /// <returns>是否重新加载成功</returns>
        public async Task<bool> ReloadAsync()
        {
            try
            {
                _logger.LogInformation("开始重新加载病案数据");
                await _dataManager.ReloadAsync();
                _logger.LogInformation("病案数据重新加载成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新加载病案数据失败");
                return false;
            }
        }

        #endregion

        #region 工作流步骤验证命令

        /// <summary>
        /// 验证是否可以完成Step1（辨证）
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否可以完成</returns>
        public bool CanCompleteStep1(out string errorMessage)
        {
            return _validator.CanCompleteStep1(out errorMessage);
        }

        /// <summary>
        /// 验证是否可以开方标记（Step2）
        /// </summary>
        /// <param name="currentStep">当前步骤</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否可以开方</returns>
        public bool CanMarkForPrescription(ConsultationStep currentStep, out string errorMessage)
        {
            return _validator.CanMarkForPrescription(currentStep, out errorMessage);
        }

        /// <summary>
        /// 验证是否可以创建处方（Step3）
        /// </summary>
        /// <param name="currentStep">当前步骤</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否可以创建处方</returns>
        public bool CanCreatePrescription(ConsultationStep currentStep, out string errorMessage)
        {
            return _validator.CanCreatePrescription(currentStep, out errorMessage);
        }

        /// <summary>
        /// 验证当前步骤
        /// </summary>
        /// <param name="currentStep">当前步骤</param>
        /// <returns>验证是否通过</returns>
        public async Task<bool> ValidateStepAsync(ConsultationStep currentStep)
        {
            return await _validator.ValidateStepAsync(currentStep);
        }

        #endregion

        #region 处方管理命令

        /// <summary>
        /// 创建处方
        /// </summary>
        /// <param name="createDto">处方创建DTO</param>
        /// <returns>是否创建成功</returns>
        public async Task<bool> CreatePrescriptionAsync(PrescriptionCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    _logger.LogWarning("创建处方失败：DTO为空");
                    return false;
                }

                _logger.LogInformation("开始创建处方");

                var prescription = await _dataManager.CreatePrescriptionAsync(createDto);
                if (prescription != null)
                {
                    _logger.LogInformation("处方创建成功: {PrescriptionId}", prescription.Id);
                    return true;
                }
                else
                {
                    _logger.LogWarning("处方创建失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                return false;
            }
        }

        /// <summary>
        /// 更新处方（实际通过Save实现）
        /// </summary>
        /// <returns>是否更新成功</returns>
        public async Task<bool> UpdatePrescriptionAsync()
        {
            try
            {
                _logger.LogInformation("开始更新处方");

                if (_dataManager.CurrentPrescription == null)
                {
                    _logger.LogWarning("当前处方为空，无法更新");
                    return false;
                }

                // 处方更新通过SaveAsync实现
                var result = await SaveAsync(validateBeforeSave: true);
                if (result)
                {
                    _logger.LogInformation("处方更新成功");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败");
                return false;
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeletePrescriptionAsync()
        {
            try
            {
                _logger.LogInformation("开始删除处方");

                var result = await _dataManager.DeletePrescriptionAsync();
                if (result)
                {
                    _logger.LogInformation("处方删除成功");
                }
                else
                {
                    _logger.LogWarning("处方删除失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败");
                return false;
            }
        }

        #endregion

        #region 导航命令

        /// <summary>
        /// 导航到患者病历历史
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> NavigateToPatientHistoryAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("导航到患者病历历史: {PatientId}", patientId);

                var parameters = new NavigationParameters
                {
                    { "PatientId", patientId }
                };

                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseListView", parameters);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到患者病历历史失败: {PatientId}", patientId);
                return false;
            }
        }

        /// <summary>
        /// 导航到病案列表
        /// </summary>
        /// <returns>是否成功</returns>
        public async Task<bool> NavigateToMedicalCaseListAsync()
        {
            try
            {
                _logger.LogInformation("导航到病案列表");
                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseListView");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到病案列表失败");
                return false;
            }
        }

        #endregion
    }
}
