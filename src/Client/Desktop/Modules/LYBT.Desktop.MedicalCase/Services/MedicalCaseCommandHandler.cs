using LYBT.Desktop.Contracts.Components;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 病案命令处理器 - 业务逻辑协调者
    /// Issue #1778: MedicalCase模块组件化改造
    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[HDL]前缀
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

        private readonly MedicalCaseService _dataManager;
        private readonly MedicalCaseValidator _validator;
        private readonly ILogger<MedicalCaseCommandHandler> _logger;
        private readonly IRegionManager _regionManager;
        private readonly Dictionary<string, Func<object?, Task<bool>>> _commands;
        private readonly Dictionary<string, Func<bool>> _canExecuteHandlers;

        #endregion

        #region 构造函数

        public MedicalCaseCommandHandler(
            MedicalCaseService dataManager,
            MedicalCaseValidator validator,
            ILogger<MedicalCaseCommandHandler> logger,
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
            _logger.LogDebug("[HDL] MedicalCase.RegisterCommand - Name={CommandName}", commandName);
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
            _logger.LogDebug("[HDL] MedicalCase.RegisterCanExecute - Name={CommandName}", commandName);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public async Task<bool> ExecuteAsync(string commandName, object? parameter = null)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                _logger.LogWarning("[HDL] MedicalCase.Execute → EmptyCommandName");
                return false;
            }

            if (!_commands.ContainsKey(commandName))
            {
                _logger.LogWarning("[HDL] MedicalCase.Execute → CommandNotFound - Name={CommandName}", commandName);
                return false;
            }

            try
            {
                _logger.LogDebug("[HDL] MedicalCase.Execute started - Name={CommandName}", commandName);
                var result = await _commands[commandName](parameter);
                _logger.LogDebug("[HDL] MedicalCase.Execute completed - Name={CommandName} Result={Result}", commandName, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] MedicalCase.Execute failed - Name={CommandName}", commandName);
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
                _logger.LogInformation("[HDL] MedicalCase.Save started - Validate={ValidateBeforeSave}", validateBeforeSave);

                // 1. 可选验证
                if (validateBeforeSave)
                {
                    if (!_validator.IsValid(out var errorMessage))
                    {
                        _logger.LogWarning("[HDL] MedicalCase.Save → ValidationFailed - Error={ErrorMessage}", errorMessage);
                        return false;
                    }
                }

                // 2. 保存数据
                var result = await _dataManager.SaveAsync();
                if (result)
                {
                    _logger.LogInformation("[HDL] MedicalCase.Save completed");
                }
                else
                {
                    _logger.LogWarning("[HDL] MedicalCase.Save → Failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] MedicalCase.Save failed");
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
                _logger.LogInformation("[HDL] MedicalCase.Delete started");
                var result = await _dataManager.DeleteAsync();

                if (result)
                {
                    _logger.LogInformation("[HDL] MedicalCase.Delete completed");
                }
                else
                {
                    _logger.LogWarning("[HDL] MedicalCase.Delete → Failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] MedicalCase.Delete failed");
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
                _logger.LogDebug("[HDL] MedicalCase.Reload started");
                await _dataManager.ReloadAsync();
                _logger.LogDebug("[HDL] MedicalCase.Reload completed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] MedicalCase.Reload failed");
                return false;
            }
        }

        #endregion

        // [已移除] 三步流程步骤验证命令 (CanCompleteStep1, CanMarkForPrescription, CanCreatePrescription, ValidateStepAsync)

        #region 处方管理命令

        /// <summary>
        /// 创建处方
        /// </summary>
        /// <param name="createDto">处方创建DTO</param>
        /// <returns>是否创建成功</returns>
        public async Task<bool> CreatePrescriptionAsync(PrescriptionInputDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    _logger.LogWarning("[HDL] Prescription.Create → NullDto");
                    return false;
                }

                _logger.LogInformation("[HDL] Prescription.Create started");

                var prescription = await _dataManager.CreatePrescriptionAsync(createDto);
                if (prescription != null)
                {
                    _logger.LogInformation("[HDL] Prescription.Create completed - PrescriptionId={PrescriptionId}", prescription.Id);
                    return true;
                }
                else
                {
                    _logger.LogWarning("[HDL] Prescription.Create → Failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] Prescription.Create failed");
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
                _logger.LogInformation("[HDL] Prescription.Update started");

                if (_dataManager.CurrentPrescription == null)
                {
                    _logger.LogWarning("[HDL] Prescription.Update → NoPrescription");
                    return false;
                }

                // 处方更新通过SaveAsync实现
                var result = await SaveAsync(validateBeforeSave: true);
                if (result)
                {
                    _logger.LogInformation("[HDL] Prescription.Update completed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] Prescription.Update failed");
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
                _logger.LogInformation("[HDL] Prescription.Delete started");

                var result = await _dataManager.DeletePrescriptionAsync();
                if (result)
                {
                    _logger.LogInformation("[HDL] Prescription.Delete completed");
                }
                else
                {
                    _logger.LogWarning("[HDL] Prescription.Delete → Failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] Prescription.Delete failed");
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
                _logger.LogDebug("[HDL] MedicalCase.NavigateToPatientHistory - PatientId={PatientId}", patientId);

                var parameters = new NavigationParameters
                {
                    { "PatientId", patientId }
                };

                // OpenSpec: refactor-medicalcase-management - 使用新的Master-Detail视图
                _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.MedicalCaseMasterDetail, parameters);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] MedicalCase.NavigateToPatientHistory failed - PatientId={PatientId}", patientId);
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
                _logger.LogDebug("[HDL] MedicalCase.NavigateToList");
                // OpenSpec: refactor-medicalcase-management - 使用新的Master-Detail视图
                _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.MedicalCaseMasterDetail);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HDL] MedicalCase.NavigateToList failed");
                return false;
            }
        }

        #endregion
    }
}
