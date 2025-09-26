using AutoMapper;
using FormulaEntity = LYBT.Entities.Formula.Formula;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaRepository repository,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dto = new PagedResult<FormulaDto>
                {
                    Items = _mapper.Map<List<FormulaDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<FormulaDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方列表失败");
                return ServiceResult<PagedResult<FormulaDto>>.Failure("获取验方列表失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                var dto = _mapper.Map<FormulaDto>(entity);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败");
                return ServiceResult<FormulaDto>.Failure("获取验方详情失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<FormulaEntity>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                return ServiceResult<FormulaDto>.Failure("创建验方失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败");
                return ServiceResult<FormulaDto>.Failure("更新验方失败");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败");
                return ServiceResult.Failure("删除验方失败");
            }
        }
    }
}