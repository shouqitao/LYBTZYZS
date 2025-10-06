using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using SharedInterfaces = LYBT.Shared.Interfaces.Services;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// </summary>
    public class HerbService : Interfaces.IHerbService, SharedInterfaces.IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbService> _logger;

        public HerbService(
            IHerbRepository repository,
            IMapper mapper,
            ILogger<HerbService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dto = new PagedResult<HerbDto>
                {
                    Items = _mapper.Map<List<HerbDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<HerbDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                return ServiceResult<PagedResult<HerbDto>>.Failure("获取药材列表失败");
            }
        }

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                var dto = _mapper.Map<HerbDto>(entity);
                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败");
                return ServiceResult<HerbDto>.Failure("获取药材详情失败");
            }
        }

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<Herb>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<HerbDto>(result);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return ServiceResult<HerbDto>.Failure("创建药材失败");
            }
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<HerbDto>(result);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败");
                return ServiceResult<HerbDto>.Failure("更新药材失败");
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
                _logger.LogError(ex, "删除药材失败");
                return ServiceResult.Failure("删除药材失败");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                var entities = await _repository.FindAsync(h =>
                    h.Name.Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
                var dtos = _mapper.Map<List<HerbDto>>(entities);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败: {Keyword}", keyword);
                return ServiceResult<List<HerbDto>>.Failure("搜索药材失败");
            }
        }
    }
}
