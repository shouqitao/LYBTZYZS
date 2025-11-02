using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者命令处理器 - 组件化架构
    /// 负责处理患者相关的业务命令
    /// Epic #1773 Task 4: Patients模块组件化改造
    /// </summary>
    public class PatientCommandHandler
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientCommandHandler> _logger;
        private readonly ISessionManager _sessionManager;
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

        private PatientDataManager? _dataManager;
        private PatientValidator? _validator;

        #endregion

        public PatientCommandHandler(
            IPatientRepository patientRepository,
            ILogger<PatientCommandHandler> logger,
            ISessionManager sessionManager,
            IRegionManager regionManager)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
            EditCommand = new DelegateCommand(ExecuteEdit, CanExecuteEdit);
            CancelEditCommand = new DelegateCommand(ExecuteCancelEdit, CanExecuteCancelEdit);
            DeleteCommand = new DelegateCommand(async () => await ExecuteDeleteAsync(), CanExecuteDelete);
            ViewMedicalHistoryCommand = new DelegateCommand(ExecuteViewMedicalHistory);
            BackCommand = new DelegateCommand(ExecuteBack);
        }

        /// <summary>
        /// 设置依赖
        /// </summary>
        public void SetDependencies(
            PatientDataManager dataManager,
            PatientValidator validator)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        #region 患者CRUD操作

        /// <summary>
        /// 创建患者
        /// </summary>
        public async Task<CommandResult<PatientDto>> CreatePatientAsync(PatientInputDto inputDto)
        {
            try
            {
                _logger.LogInformation("开始创建患者: {PatientName}", inputDto.Name);

                var patient = await _patientRepository.CreateAsync(inputDto);
                _logger.LogInformation("患者创建成功: {PatientId}", patient.Id);
                return CommandResult<PatientDto>.Success(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者时发生异常: {PatientName}", inputDto.Name);
                return CommandResult<PatientDto>.Failure("创建患者时发生系统错误");
            }
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        public async Task<CommandResult<PatientDto>> UpdatePatientAsync(PatientInputDto inputDto)
        {
            try
            {
                _logger.LogInformation("开始更新患者: {PatientId}", inputDto.Id);

                var patient = await _patientRepository.UpdateAsync(inputDto);
                _logger.LogInformation("患者更新成功: {PatientId}", patient.Id);
                return CommandResult<PatientDto>.Success(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者时发生异常: {PatientId}", inputDto.Id);
                return CommandResult<PatientDto>.Failure("更新患者时发生系统错误");
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        public async Task<CommandResult<bool>> DeletePatientAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("开始删除患者: {PatientId}", patientId);

                await _patientRepository.DeleteAsync(patientId);
                _logger.LogInformation("患者删除成功: {PatientId}", patientId);
                return CommandResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者时发生异常: {PatientId}", patientId);
                return CommandResult<bool>.Failure("删除患者时发生系统错误");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行保存
        /// </summary>
        private Task ExecuteSaveAsync()
        {
            _logger.LogInformation("执行保存患者");
            OnPatientSaved?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行编辑
        /// </summary>
        private void ExecuteEdit()
        {
            _logger.LogInformation("执行编辑患者");
            OnEditEnabled?.Invoke();
        }

        /// <summary>
        /// 执行取消编辑
        /// </summary>
        private void ExecuteCancelEdit()
        {
            _logger.LogInformation("执行取消编辑");
            OnEditCancelled?.Invoke();
        }

        /// <summary>
        /// 执行删除
        /// </summary>
        private Task ExecuteDeleteAsync()
        {
            _logger.LogInformation("执行删除患者");
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
                _logger.LogWarning("无法查看病历历史：患者ID无效");
                return;
            }

            _logger.LogInformation("导航到病历历史，患者ID: {PatientId}", _dataManager.PatientId);

            var parameters = new NavigationParameters
            {
                { "PatientId", _dataManager.PatientId }
            };

            _regionManager.RequestNavigate("ContentRegion", "MedicalCaseListView", parameters);
        }

        /// <summary>
        /// 执行返回
        /// </summary>
        private void ExecuteBack()
        {
            _logger.LogInformation("执行返回");
            _regionManager.RequestNavigate("ContentRegion", "PatientManagementView");
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
        /// </summary>
        public async Task<CommandResult<bool>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds)
        {
            try
            {
                var ids = patientIds?.ToList() ?? new List<Guid>();
                _logger.LogInformation("开始批量删除患者，数量: {Count}", ids.Count);

                if (!ids.Any())
                {
                    return CommandResult<bool>.Failure("没有选择要删除的患者");
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var id in ids)
                {
                    try
                    {
                        await _patientRepository.DeleteAsync(id);
                        successCount++;
                    }
                    catch
                    {
                        failureCount++;
                    }
                }

                if (failureCount == 0)
                {
                    _logger.LogInformation("批量删除患者成功，数量: {Count}", ids.Count);
                    return CommandResult<bool>.Success(true);
                }
                else
                {
                    _logger.LogWarning("批量删除患者部分失败：成功 {SuccessCount} 个，失败 {FailureCount} 个", successCount, failureCount);
                    return CommandResult<bool>.Failure($"批量删除完成：成功 {successCount} 个，失败 {failureCount} 个");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除患者时发生异常");
                return CommandResult<bool>.Failure("批量删除患者时发生系统错误");
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<CommandResult<IEnumerable<PatientDto>>> SearchPatientsAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("开始搜索患者：{Keyword}", keyword);

                var patients = await _patientRepository.SearchAsync(keyword);
                _logger.LogInformation("搜索患者成功，数量: {Count}", patients.Count);
                return CommandResult<IEnumerable<PatientDto>>.Success(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者时发生异常：{Keyword}", keyword);
                return CommandResult<IEnumerable<PatientDto>>.Failure("搜索患者时发生系统错误");
            }
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<CommandResult<PagedResult<PatientDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null)
        {
            try
            {
                _logger.LogInformation("开始分页查询患者：Page={Page}, PageSize={PageSize}", page, pageSize);

                var result = await _patientRepository.GetPagedAsync(page, pageSize, keyword);
                _logger.LogInformation("分页查询患者成功，数量: {Count}", result.Items.Count);
                return CommandResult<PagedResult<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询患者时发生异常");
                return CommandResult<PagedResult<PatientDto>>.Failure("查询患者列表时发生系统错误");
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
