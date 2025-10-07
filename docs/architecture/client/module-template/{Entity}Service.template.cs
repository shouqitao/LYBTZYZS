using AutoMapper;
using LYBT.Shared.Interfaces.Repositories;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{Module};
using LYBT.Entities.{Module};
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Desktop.Services.Business;

/// <summary>
/// {Entity} 业务服务实现
/// 职责：封装业务逻辑、DTO转换、异常处理
/// </summary>
public class {Entity}Service : I{Entity}Service
{
    #region Fields

    private readonly I{Entity}Repository _repository;
    private readonly ILogger<{Entity}Service> _logger;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IMapper _mapper;

    #endregion

    #region Constructor

    /// <summary>
    /// 构造函数
    /// 依赖注入顺序：Repository → Logger → ExceptionHandler → Mapper
    /// </summary>
    public {Entity}Service(
        I{Entity}Repository repository,
        ILogger<{Entity}Service> logger,
        IExceptionHandler exceptionHandler,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    #endregion

    #region I{Entity}Service Implementation

    /// <summary>
    /// 获取分页 {Entity} 列表
    /// </summary>
    public async Task<ServiceResult<PagedData<{Entity}Dto>>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchText = null)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            _logger.LogInformation("获取 {Entity} 分页列表: page={Page}, pageSize={PageSize}, search={Search}",
                page, pageSize, searchText);

            var entities = await _repository.GetPagedAsync(page, pageSize, searchText);

            var dtos = _mapper.Map<IEnumerable<{Entity}Dto>>(entities.Items);

            var pagedData = new PagedData<{Entity}Dto>
            {
                Items = dtos.ToList(),
                TotalCount = entities.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedData<{Entity}Dto>>.Success(pagedData);

        }, nameof(GetPagedAsync));
    }

    /// <summary>
    /// 根据 ID 获取 {Entity}
    /// </summary>
    public async Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            _logger.LogInformation("获取 {Entity}: id={Id}", id);

            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                return ServiceResult<{Entity}Dto>.Failure("未找到指定的 {Entity}");
            }

            var dto = _mapper.Map<{Entity}Dto>(entity);
            return ServiceResult<{Entity}Dto>.Success(dto);

        }, nameof(GetByIdAsync));
    }

    /// <summary>
    /// 创建 {Entity}
    /// </summary>
    public async Task<ServiceResult<{Entity}Dto>> CreateAsync(Create{Entity}Dto createDto)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            _logger.LogInformation("创建 {Entity}");

            // 使用 AutoMapper 转换 DTO → Entity
            var entity = _mapper.Map<{Entity}>(createDto);

            // 设置审计字段
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // 保存到仓储
            var created = await _repository.CreateAsync(entity);

            // 转换回 DTO
            var dto = _mapper.Map<{Entity}Dto>(created);

            _logger.LogInformation("{Entity} 创建成功: id={Id}", dto.Id);
            return ServiceResult<{Entity}Dto>.Success(dto);

        }, nameof(CreateAsync));
    }

    /// <summary>
    /// 更新 {Entity}
    /// </summary>
    public async Task<ServiceResult<{Entity}Dto>> UpdateAsync(Guid id, Update{Entity}Dto updateDto)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            _logger.LogInformation("更新 {Entity}: id={Id}", id);

            // 获取现有实体
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return ServiceResult<{Entity}Dto>.Failure("未找到指定的 {Entity}");
            }

            // 使用 AutoMapper 更新字段
            _mapper.Map(updateDto, existing);

            // 更新审计字段
            existing.UpdatedAt = DateTime.UtcNow;

            // 保存更改
            var updated = await _repository.UpdateAsync(existing);

            // 转换回 DTO
            var dto = _mapper.Map<{Entity}Dto>(updated);

            _logger.LogInformation("{Entity} 更新成功: id={Id}", id);
            return ServiceResult<{Entity}Dto>.Success(dto);

        }, nameof(UpdateAsync));
    }

    /// <summary>
    /// 删除 {Entity}
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            _logger.LogInformation("删除 {Entity}: id={Id}", id);

            var success = await _repository.DeleteAsync(id);

            if (success)
            {
                _logger.LogInformation("{Entity} 删除成功: id={Id}", id);
                return ServiceResult.Success();
            }
            else
            {
                return ServiceResult.Failure("删除 {Entity} 失败");
            }

        }, nameof(DeleteAsync));
    }

    #endregion
}
