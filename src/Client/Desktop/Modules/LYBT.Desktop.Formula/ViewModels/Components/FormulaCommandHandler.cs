using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 配方命令处理器 - 组件化架构实现
    /// Issue #1153: 负责配方的命令操作（保存、复制、打印等）
    /// Issue #2074: 新增8列DataGrid行操作命令（添加行、删除行、清空）
    /// </summary>
    public class FormulaCommandHandler : BindableBase
    {
        private readonly IFormulaRepository _repository;
        private readonly ILogger<FormulaCommandHandler> _logger;

        #region 私有字段

        private bool _isReadOnly;
        private bool _isLoading;

        #endregion

        #region 状态属性（Issue #2074: 命令CanExecute状态管理）

        /// <summary>
        /// 是否只读模式
        /// </summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令（Issue #2074: 8列DataGrid行操作）

        /// <summary>
        /// 添加药材行命令
        /// </summary>
        public DelegateCommand AddHerbCommand { get; }

        /// <summary>
        /// 删除药材行命令
        /// </summary>
        public DelegateCommand RemoveHerbCommand { get; }

        /// <summary>
        /// 清空药材列表命令
        /// </summary>
        public DelegateCommand ClearAllCommand { get; }

        #endregion

        #region 事件（Issue #2074: 事件通知模式）

        /// <summary>
        /// 添加药材行事件
        /// </summary>
        public event Action? OnHerbAdded;

        /// <summary>
        /// 删除药材行事件
        /// </summary>
        public event Action? OnHerbRemoved;

        /// <summary>
        /// 清空药材列表事件
        /// </summary>
        public event Action? OnHerbsCleared;

        #endregion

        public FormulaCommandHandler(IFormulaRepository repository, ILogger<FormulaCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Issue #2074: 初始化8列DataGrid行操作命令
            AddHerbCommand = new DelegateCommand(
                () =>
                {
                    _logger.LogDebug("执行AddHerbCommand");
                    OnHerbAdded?.Invoke();
                },
                () => !IsReadOnly && !IsLoading)
                .ObservesProperty(() => IsReadOnly)
                .ObservesProperty(() => IsLoading);

            RemoveHerbCommand = new DelegateCommand(
                () =>
                {
                    _logger.LogDebug("执行RemoveHerbCommand");
                    OnHerbRemoved?.Invoke();
                },
                () => !IsReadOnly && !IsLoading)
                .ObservesProperty(() => IsReadOnly)
                .ObservesProperty(() => IsLoading);

            ClearAllCommand = new DelegateCommand(
                () =>
                {
                    _logger.LogDebug("执行ClearAllCommand");
                    OnHerbsCleared?.Invoke();
                },
                () => !IsReadOnly && !IsLoading)
                .ObservesProperty(() => IsReadOnly)
                .ObservesProperty(() => IsLoading);
        }

        #region 保存操作

        /// <summary>
        /// 保存配方
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> SaveFormulaAsync(
            FormulaDto currentFormula,
            string formulaName,
            string? pinYinCode,
            string property,
            string effect,
            string usage,
            string remark,
            bool isShared,
            IEnumerable<FormulaHerbItemDto> herbItems)
        {
            try
            {
                _logger.LogInformation("保存配方: {FormulaId}", currentFormula.Id);

                var updateDto = new FormulaInputDto
                {
                    Id = currentFormula.Id,
                    Name = formulaName.Trim(),
                    PinYinCode = string.IsNullOrWhiteSpace(pinYinCode) ? null : pinYinCode.Trim(),
                    Property = string.IsNullOrWhiteSpace(property) ? null : property.Trim(),
                    Effect = string.IsNullOrWhiteSpace(effect) ? string.Empty : effect.Trim(),
                    Usage = string.IsNullOrWhiteSpace(usage) ? string.Empty : usage.Trim(),
                    Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
                    IsShared = isShared,
                    Status = currentFormula.Status,
                    Herbs = herbItems.Select(h => new FormulaHerbItemInputDto
                    {
                        Id = h.Id,
                        HerbId = h.HerbId,
                        HerbName = h.Herb?.Name ?? h.HerbName ?? string.Empty,
                        Quantity = h.Quantity,
                        Unit = h.Unit ?? "g",
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder
                    }).ToList()
                };

                var updatedFormula = await _repository.UpdateAsync(updateDto);
                return (true, updatedFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配方时发生异常: {FormulaId}", currentFormula.Id);
                return (false, null, "保存配方时发生系统错误，请稍后重试");
            }
        }

        #endregion

        #region 复制操作

        /// <summary>
        /// 复制配方
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> CopyFormulaAsync(FormulaDto sourceFormula)
        {
            try
            {
                _logger.LogInformation("复制配方: {FormulaId} ({FormulaName})", sourceFormula.Id, sourceFormula.Name);

                var createDto = new FormulaInputDto
                {
                    Name = $"{sourceFormula.Name}_副本",
                    Effect = sourceFormula.Effect!,
                    Usage = sourceFormula.Usage!,
                    Remark = sourceFormula.Remark!,
                    IsShared = false, // 副本默认不共享
                    Herbs = sourceFormula.Herbs?.Select(h => new FormulaHerbItemInputDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder
                    }).ToList() ?? new List<FormulaHerbItemInputDto>()
                };

                var newFormula = await _repository.CreateAsync(createDto);
                return (true, newFormula, $"配方复制成功！新配方名称：{newFormula.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制配方时发生异常: {FormulaId}", sourceFormula.Id);
                return (false, null, "复制配方时发生系统错误，请稍后重试");
            }
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 删除配方
        /// </summary>
        public async Task<(bool success, string? errorMessage)> DeleteFormulaAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("删除配方: {FormulaId}", formulaId);

                await _repository.DeleteAsync(formulaId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配方时发生异常: {FormulaId}", formulaId);
                return (false, "删除配方时发生系统错误，请稍后重试");
            }
        }

        /// <summary>
        /// 删除配方（简化版，Issue #1787: 兼容返回bool的调用）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("删除配方: {FormulaId}", formulaId);
                await _repository.DeleteAsync(formulaId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配方时发生异常: {FormulaId}", formulaId);
                return false;
            }
        }

        #endregion

        #region 基本CRUD操作

        /// <summary>
        /// 创建配方（Issue #1787: 支持基本创建操作）
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> CreateAsync(FormulaInputDto createDto)
        {
            try
            {
                _logger.LogInformation("创建配方: {FormulaName}", createDto.Name);

                var createdFormula = await _repository.CreateAsync(createDto);
                _logger.LogInformation("配方创建成功: {FormulaId}", createdFormula.Id);

                return (true, createdFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建配方时发生异常: {FormulaName}", createDto.Name);
                return (false, null, "创建配方时发生系统错误");
            }
        }

        /// <summary>
        /// 更新配方（Issue #1787: 支持基本更新操作）
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> UpdateAsync(FormulaInputDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新配方: {FormulaId}", updateDto.Id);

                var updatedFormula = await _repository.UpdateAsync(updateDto);
                _logger.LogInformation("配方更新成功: {FormulaName}", updatedFormula.Name);

                return (true, updatedFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新配方时发生异常: {FormulaId}", updateDto.Id);
                return (false, null, "更新配方时发生系统错误");
            }
        }

        #endregion

        #region 其他操作

        /// <summary>
        /// 打印配方（占位实现）
        /// </summary>
        public Task<(bool success, string? errorMessage)> PrintFormulaAsync(FormulaDto formula)
        {
            _logger.LogInformation("打印配方: {FormulaId} ({FormulaName})", formula.Id, formula.Name);

            // TODO: 实现打印逻辑
            return Task.FromResult<(bool, string?)>((true, "打印功能开发中"));
        }

        /// <summary>
        /// 分页查询配方（Issue #1787: 支持分页查询）
        /// </summary>
        public async Task<(bool success, PagedResult<FormulaDto>? data, string? errorMessage)> GetPagedAsync(
            int page, int pageSize, string? searchText = null)
        {
            try
            {
                _logger.LogInformation("分页查询配方: Page={Page}, PageSize={PageSize}, SearchText={SearchText}",
                    page, pageSize, searchText);

                var result = await _repository.GetPagedAsync(page, pageSize, searchText);

                _logger.LogInformation("查询成功，共{TotalCount}条数据", result.TotalCount);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询配方时发生异常");
                return (false, null, "查询配方时发生系统错误");
            }
        }

        /// <summary>
        /// 根据ID获取配方（Issue #1787: 支持单个配方查询）
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> GetByIdAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("开始查询配方: FormulaId={FormulaId}", formulaId);

                var formula = await _repository.GetByIdAsync(formulaId);

                if (formula == null)
                {
                    _logger.LogWarning("配方不存在：FormulaId={FormulaId}", formulaId);
                    return (false, null, "配方不存在");
                }

                _logger.LogInformation("查询配方成功：{FormulaName}", formula.Name);
                return (true, formula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询配方时发生异常：FormulaId={FormulaId}", formulaId);
                return (false, null, "查询配方时发生系统错误");
            }
        }

        /// <summary>
        /// 查看使用历史（占位实现）
        /// </summary>
        public Task<(bool success, string? errorMessage)> ViewUsageHistoryAsync(Guid formulaId)
        {
            _logger.LogInformation("查看配方使用历史: {FormulaId}", formulaId);

            // TODO: 实现查看使用历史逻辑
            return Task.FromResult<(bool, string?)>((true, "查看使用历史功能开发中"));
        }

        /// <summary>
        /// 获取待校验的验方列表（Issue #1787: 支持FormulaValidationViewModel）
        /// </summary>
        public async Task<(bool success, List<FormulaDto>? data, string? errorMessage)> GetPendingValidationFormulasAsync()
        {
            try
            {
                _logger.LogInformation("查询待校验验方列表");

                var formulas = await _repository.GetPendingValidationFormulasAsync();

                _logger.LogInformation("查询成功，共{Count}个待校验验方", formulas.Count);
                return (true, formulas, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询待校验验方列表时发生异常");
                return (false, null, "查询待校验验方列表时发生系统错误");
            }
        }

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库（Issue #1787: 支持FormulaValidationViewModel）
        /// </summary>
        public async Task<(bool success, string? errorMessage)> ValidateFormulaHerbAsync(
            Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            try
            {
                _logger.LogInformation(
                    "验证配方药材: FormulaId={FormulaId}, HerbItemId={HerbItemId}, SelectedHerbId={SelectedHerbId}",
                    formulaId, herbItemId, selectedHerbId);

                var result = await _repository.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

                if (result)
                {
                    _logger.LogInformation("配方药材验证成功");
                    return (true, null);
                }
                else
                {
                    _logger.LogWarning("配方药材验证失败");
                    return (false, "配方药材验证失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证配方药材时发生异常");
                return (false, "验证配方药材时发生系统错误");
            }
        }

        #endregion
    }
}
