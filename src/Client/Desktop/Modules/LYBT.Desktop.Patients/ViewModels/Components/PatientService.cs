using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者Service - 业务逻辑处理
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// 负责处理患者相关的业务命令
    /// </summary>
    public class PatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientService> _logger;
        private readonly IRegionManager _regionManager;

        #region 事件定义

        /// <summary>
        /// 患者保存成功事件
        /// </summary>
        public event Action? OnPatientSaved;

        /// <summary>
        /// 患者删除成功事件
        /// </summary>
        public event Action? OnPatientDeleted;

        /// <summary>
        /// 患者编辑启用事件
        /// </summary>
        public event Action? OnEditEnabled;

        /// <summary>
        /// 患者编辑取消事件
        /// </summary>
        public event Action? OnEditCancelled;

        #endregion

        #region 命令定义

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        public ICommand EditCommand { get; private set; }

        /// <summary>
        /// 取消编辑命令
        /// </summary>
        public ICommand CancelEditCommand { get; private set; }

        /// <summary>
        /// 删除命令
        /// </summary>
        public ICommand DeleteCommand { get; private set; }

        /// <summary>
        /// 查看病历历史命令
        /// </summary>
        public ICommand ViewMedicalHistoryCommand { get; private set; }

        /// <summary>
        /// 返回命令
        /// </summary>
        public ICommand BackCommand { get; private set; }

        #endregion

        #region 依赖字段

        private PatientStateManager? _dataManager;

        #endregion

        public PatientService(
            IPatientRepository patientRepository,
            ILogger<PatientService> logger,
            IRegionManager regionManager)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            // OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm RelayCommand
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit, CanExecuteCancelEdit);
            DeleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, CanExecuteDelete);
            ViewMedicalHistoryCommand = new RelayCommand(ExecuteViewMedicalHistory);
            BackCommand = new RelayCommand(ExecuteBack);
        }

        /// <summary>
        /// 设置依赖
        /// </summary>
        public void SetDependencies(PatientStateManager dataManager)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        }

        #region 患者CRUD操作

        /// <summary>
        /// 创建患者
        /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
        /// </summary>
        public async Task<CommandResult<PatientDetailDto>> CreatePatientAsync(PatientInputDto inputDto)
        {
            try
            {
                _logger.LogInformation("[SVC] Patient.Create started - Name={PatientName}", inputDto.Name);

                var patient = await _patientRepository.CreateAsync(inputDto);
                _logger.LogInformation("[SVC] Patient.Create completed - PatientId={PatientId}", patient.Id);
                return CommandResult<PatientDetailDto>.Success(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Create failed - Name={PatientName}", inputDto.Name);
                return CommandResult<PatientDetailDto>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建患者", ex));
            }
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        public async Task<CommandResult<PatientDetailDto>> UpdatePatientAsync(PatientInputDto inputDto)
        {
            try
            {
                _logger.LogInformation("[SVC] Patient.Update started - PatientId={PatientId}", inputDto.Id);

                var patient = await _patientRepository.UpdateAsync(inputDto);
                _logger.LogInformation("[SVC] Patient.Update completed - PatientId={PatientId}", patient.Id);
                return CommandResult<PatientDetailDto>.Success(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Update failed - PatientId={PatientId}", inputDto.Id);
                return CommandResult<PatientDetailDto>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新患者", ex));
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        public async Task<CommandResult<bool>> DeletePatientAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("[SVC] Patient.Delete started - PatientId={PatientId}", patientId);

                await _patientRepository.DeleteAsync(patientId);
                _logger.LogInformation("[SVC] Patient.Delete completed - PatientId={PatientId}", patientId);
                return CommandResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Delete failed - PatientId={PatientId}", patientId);
                return CommandResult<bool>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除患者", ex));
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行保存
        /// </summary>
        private Task ExecuteSaveAsync()
        {
            _logger.LogDebug("[SVC] Patient.ExecuteSave");
            OnPatientSaved?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行编辑
        /// </summary>
        private void ExecuteEdit()
        {
            _logger.LogDebug("[SVC] Patient.ExecuteEdit");
            OnEditEnabled?.Invoke();
        }

        /// <summary>
        /// 执行取消编辑
        /// </summary>
        private void ExecuteCancelEdit()
        {
            _logger.LogDebug("[SVC] Patient.ExecuteCancelEdit");
            OnEditCancelled?.Invoke();
        }

        /// <summary>
        /// 执行删除
        /// </summary>
        private Task ExecuteDeleteAsync()
        {
            _logger.LogDebug("[SVC] Patient.ExecuteDelete");
            OnPatientDeleted?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行查看病历历史
        /// </summary>
        private void ExecuteViewMedicalHistory()
        {
            if (_dataManager == null || _dataManager.PatientId == Guid.Empty)
            {
                _logger.LogWarning("[SVC] Patient.ViewMedicalHistory → InvalidId");
                return;
            }

            _logger.LogDebug("[SVC] Patient.ViewMedicalHistory - PatientId={PatientId}", _dataManager.PatientId);

            var parameters = new NavigationParameters
            {
                { "PatientId", _dataManager.PatientId }
            };

            _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.MedicalCaseList, parameters);
        }

        /// <summary>
        /// 执行返回
        /// </summary>
        private void ExecuteBack()
        {
            _logger.LogDebug("[SVC] Patient.ExecuteBack");
            _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.PatientMasterDetail);
        }

        /// <summary>
        /// 可以执行保存
        /// </summary>
        private bool CanExecuteSave() => _dataManager != null && !_dataManager.IsReadOnly && !_dataManager.IsLoading;

        /// <summary>
        /// 可以执行编辑
        /// </summary>
        private bool CanExecuteEdit() => _dataManager != null && _dataManager.IsReadOnly && !_dataManager.IsLoading;

        /// <summary>
        /// 可以执行取消编辑
        /// </summary>
        private bool CanExecuteCancelEdit() => _dataManager != null && !_dataManager.IsReadOnly && !_dataManager.IsLoading;

        /// <summary>
        /// 可以执行删除
        /// </summary>
        private bool CanExecuteDelete() => _dataManager != null && !_dataManager.IsNewPatient && !_dataManager.IsLoading;

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除患者
        /// OpenSpec: optimize-batch-operations Phase 2 - 使用单次批量API调用
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds)
        {
            try
            {
                var ids = patientIds?.ToList() ?? new List<Guid>();
                _logger.LogInformation("[SVC] Patient.BatchDelete started - Count={Count}", ids.Count);

                if (!ids.Any())
                {
                    return CommandResult<BatchOperationResultDto>.Failure("没有选择要删除的患者");
                }

                // OpenSpec: optimize-batch-operations - 使用单次批量API调用替代N+1模式
                var result = await _patientRepository.BatchDeleteAsync(ids);
                if (result == null)
                {
                    _logger.LogWarning("[SVC] Patient.BatchDelete failed");
                    return CommandResult<BatchOperationResultDto>.Failure("批量删除患者失败");
                }

                if (result.FailureCount == 0)
                {
                    _logger.LogInformation("[SVC] Patient.BatchDelete completed - Success={SuccessCount}", result.SuccessCount);
                }
                else
                {
                    _logger.LogWarning("[SVC] Patient.BatchDelete partial - Success={SuccessCount} Failure={FailureCount}",
                        result.SuccessCount, result.FailureCount);
                }

                return CommandResult<BatchOperationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.BatchDelete failed");
                return CommandResult<BatchOperationResultDto>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量删除患者", ex));
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<CommandResult<IEnumerable<PatientListDto>>> SearchPatientsAsync(string keyword)
        {
            try
            {
                _logger.LogDebug("[SVC] Patient.Search started - Keyword={Keyword}", keyword);

                var patients = await _patientRepository.SearchAsync(keyword);
                _logger.LogDebug("[SVC] Patient.Search completed - Count={Count}", patients.Count);
                return CommandResult<IEnumerable<PatientListDto>>.Success(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Search failed - Keyword={Keyword}", keyword);
                return CommandResult<IEnumerable<PatientListDto>>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("搜索患者", ex));
            }
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<CommandResult<PagedResult<PatientListDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("[SVC] Patient.GetPaged started - Page={Page} PageSize={PageSize}", page, pageSize);

                var result = await _patientRepository.GetPagedAsync(page, pageSize, keyword);
                _logger.LogDebug("[SVC] Patient.GetPaged completed - Count={Count}", result.Items.Count);
                return CommandResult<PagedResult<PatientListDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.GetPaged failed");
                return CommandResult<PagedResult<PatientListDto>>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("查询患者列表", ex));
            }
        }

        /// <summary>
        /// 根据ID获取患者（Issue #1788: 支持单个患者查询）
        /// </summary>
        public async Task<CommandResult<PatientDetailDto>> GetByIdAsync(Guid patientId)
        {
            try
            {
                _logger.LogDebug("[SVC] Patient.GetById started - PatientId={PatientId}", patientId);

                var patient = await _patientRepository.GetByIdAsync(patientId);

                if (patient == null)
                {
                    _logger.LogWarning("[SVC] Patient.GetById → NotFound - PatientId={PatientId}", patientId);
                    return CommandResult<PatientDetailDto>.Failure("患者不存在");
                }

                _logger.LogDebug("[SVC] Patient.GetById completed - Name={PatientName}", patient.Name);
                return CommandResult<PatientDetailDto>.Success(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.GetById failed - PatientId={PatientId}", patientId);
                return CommandResult<PatientDetailDto>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("查询患者", ex));
            }
        }

        #endregion
    }

    /// <summary>
    /// 命令执行结果
    /// </summary>
    public class CommandResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }

        private CommandResult(bool isSuccess, T? data, string? errorMessage)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static CommandResult<T> Success(T data)
        {
            return new CommandResult<T>(true, data, null);
        }

        public static CommandResult<T> Failure(string errorMessage)
        {
            return new CommandResult<T>(false, default, errorMessage);
        }
    }
}
