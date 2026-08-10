using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.Catalog.Services
{
    /// <summary>
    /// 验方Remote Service实现
    /// 通过 IFormulaRepository 调用远程API
    /// D1: 对齐 RemoteHerbService 模式——继承 CrudServiceBase 泛型基类（统一 try-log-ExecuteAsync 模板），
    /// 特有业务操作（复制/批量/导入导出/验方非空校验）保留在本派生类。
    /// </summary>
    public class RemoteFormulaService : CrudServiceBase<FormulaListDto, FormulaDetailDto, FormulaInputDto>, IFormulaService
    {
        private readonly IFormulaRepository _formulaRepository;

        public RemoteFormulaService(
            IFormulaRepository formulaRepository,
            ILogger<RemoteFormulaService> logger)
            : base(logger, "Formula")
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
        }

        #region Core 实现

        protected override async Task<FormulaDetailDto> CreateCoreAsync(FormulaInputDto input, CancellationToken ct)
            => await _formulaRepository.CreateAsync(input);

        protected override async Task<FormulaDetailDto> UpdateCoreAsync(FormulaInputDto input, CancellationToken ct)
            => await _formulaRepository.UpdateAsync(input);

        protected override async Task DeleteCoreAsync(Guid id, CancellationToken ct)
            => await _formulaRepository.DeleteAsync(id);

        protected override async Task<FormulaDetailDto?> GetByIdCoreAsync(Guid id, CancellationToken ct)
            => await _formulaRepository.GetByIdAsync(id);

        protected override async Task<PagedResult<FormulaListDto>> GetPagedCoreAsync(int page, int pageSize, string? keyword, CancellationToken ct)
            => await _formulaRepository.GetPagedAsync(page, pageSize, keyword);

        protected override async Task<List<FormulaListDto>> SearchCoreAsync(string keyword, CancellationToken ct)
            => await _formulaRepository.SearchAsync(keyword);

        protected override async Task<FormulaDetailDto?> ToggleStatusCoreAsync(Guid id, CancellationToken ct)
            => await _formulaRepository.ToggleStatusAsync(id);

        #endregion

        #region 创建/更新（保留验方非空业务校验，对齐原 FormulaService 文案）

        /// <inheritdoc/>
        public override async Task<CommandResult<FormulaDetailDto>> CreateAsync(FormulaInputDto input, CancellationToken ct = default)
        {
            if (input.Herbs == null || input.Herbs.Count == 0)
                return CommandResult<FormulaDetailDto>.Failed("验方必须包含至少一味中药材");

            return await base.CreateAsync(input, ct);
        }

        /// <inheritdoc/>
        public override async Task<CommandResult<FormulaDetailDto>> UpdateAsync(FormulaInputDto input, CancellationToken ct = default)
        {
            if (input.Herbs == null || input.Herbs.Count == 0)
                return CommandResult<FormulaDetailDto>.Failed("验方必须包含至少一味中药材");

            return await base.UpdateAsync(input, ct);
        }

        #endregion

        #region 复制操作

        public async Task<CommandResult<FormulaDetailDto>> CopyFormulaAsync(FormulaDetailDto sourceFormula, CancellationToken ct = default)
        {
            return await ExecuteAsync<FormulaDetailDto>("Formula.Copy", async () =>
            {
                var createDto = new FormulaInputDto
                {
                    Name = $"{sourceFormula.Name}_副本",
                    Effect = sourceFormula.Effect!,
                    Usage = sourceFormula.Usage!,
                    Remark = sourceFormula.Remark!,
                    IsShared = false,
                    Herbs = sourceFormula.Herbs?.Select(h => new FormulaHerbItemInputDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder,
                        DecocteMethod = h.DecocteMethod
                    }).ToList() ?? new List<FormulaHerbItemInputDto>()
                };

                var newFormula = await _formulaRepository.CreateAsync(createDto);
                return CommandResult<FormulaDetailDto>.Succeeded(newFormula);
            });
        }

        #endregion

        #region 批量操作

        public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> formulaIds, CancellationToken ct = default)
        {
            return await ExecuteAsync<BatchOperationResultDto>("Formula.BatchDelete", async () =>
            {
                var result = await _formulaRepository.BatchDeleteAsync(formulaIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量删除验方返回空结果");
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            });
        }

        #endregion

        #region 批量导入/导出

        public async Task<CommandResult<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
        {
            return await ExecuteAsync<FormulaBatchImportResultDto>("Formula.BatchImport", async () =>
            {
                var result = await _formulaRepository.BatchImportAsync(request, ct);
                if (result == null)
                    return CommandResult<FormulaBatchImportResultDto>.Failed("批量导入操作失败");
                return CommandResult<FormulaBatchImportResultDto>.Succeeded(result);
            });
        }

        public async Task<CommandResult<byte[]>> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
        {
            return await ExecuteAsync<byte[]>("Formula.ExportFormulas", async () =>
            {
                var data = await _formulaRepository.ExportFormulasAsync(category, ct);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出验方数据操作失败");
                return CommandResult<byte[]>.Succeeded(data);
            });
        }

        public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync<byte[]>("Formula.ExportTemplate", async () =>
            {
                var data = await _formulaRepository.ExportTemplateAsync(ct);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出模板操作失败");
                return CommandResult<byte[]>.Succeeded(data);
            });
        }

        #endregion
    }
}
