using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaApi _formulaApi;
        private readonly ILogger<FormulaService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public FormulaService(
            IFormulaApi formulaApi,
            ILogger<FormulaService> logger,
            IExceptionHandler exceptionHandler)
        {
            _formulaApi = formulaApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<FormulaDto>>(async () =>
            {
                var response = await _formulaApi.GetFormulasAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<FormulaDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<FormulaDto>(async () =>
            {
                var response = await _formulaApi.GetFormulaByIdAsync(id);
                return ServiceResult<FormulaDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            return await _exceptionHandler.HandleException<FormulaDto>(async () =>
            {
                var response = await _formulaApi.CreateFormulaAsync(dto);
                return ServiceResult<FormulaDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<FormulaDto>(async () =>
            {
                var response = await _formulaApi.UpdateFormulaAsync(id, dto);
                return ServiceResult<FormulaDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _formulaApi.DeleteFormulaAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        // 移除过度抽象的方法，简化服务实现
        // SearchAsync 功能已由 GetPagedAsync 的 keyword 参数实现
        // CloneFormulaAsync 可通过业务层组合 GetByIdAsync + CreateAsync 实现
    }
}