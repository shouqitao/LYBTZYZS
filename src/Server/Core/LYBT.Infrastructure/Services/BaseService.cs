using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Logging;
using LYBT.Shared.Models.Common;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace LYBT.Infrastructure.Services
{

    /// <summary>
    /// 服务层基类 - 统一业务逻辑封装
    /// 提供通用的业务操作、错误处理、日志记录和数据映射
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TDto">数据传输对象类型</typeparam>
    /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
    /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
    public abstract class BaseService<TEntity, TDto, TCreateDto, TUpdateDto> 
        where TEntity : class
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        protected readonly IBaseRepository<TEntity> _repository;
        protected readonly IMapper _mapper;
        protected readonly ILogger _logger;
        // UltraThink重构：删除复杂日志服务，使用标准ILogger

        protected BaseService(
            IBaseRepository<TEntity> repository,
            IMapper mapper,
            ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取实体DTO
        /// </summary>
        public virtual async Task<TDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                return entity != null ? _mapper.Map<TDto>(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取实体失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 获取所有实体DTO列表
        /// </summary>
        public virtual async Task<List<TDto>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return _mapper.Map<List<TDto>>(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有实体失败");
                throw;
            }
        }

        /// <summary>
        /// 分页获取实体DTO列表
        /// </summary>
        public virtual async Task<PaginatedResult<TDto>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? filter = null)
        {
            try
            {
                var result = await _repository.GetPagedAsync(filter, pageNumber, pageSize);
                var dtos = _mapper.Map<List<TDto>>(result.Items);
                
                return new PaginatedResult<TDto>
                {
                    Items = dtos,
                    TotalCount = result.TotalCount,
                    CurrentPage = result.CurrentPage,
                    PageSize = result.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取实体失败，页码: {Page}, 页大小: {PageSize}", pageNumber, pageSize);
                throw;
            }
        }

        /// <summary>
        /// 创建新实体
        /// </summary>
        public virtual async Task<TDto?> CreateAsync(TCreateDto createDto)
        {
            try
            {
                // 验证创建数据
                await ValidateCreateAsync(createDto);

                // 映射到实体
                var entity = _mapper.Map<TEntity>(createDto);
                
                // 预处理
                await PreCreateAsync(entity, createDto);

                // 创建实体
                var result = await _repository.AddAsync(entity);
                if (result == null)
                {
                    _logger.LogWarning("创建实体失败");
                    return null;
                }

                // 后处理
                await PostCreateAsync(result, createDto);

                // 记录操作日志
                await LogOperationAsync("Create", result);

                return _mapper.Map<TDto>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建实体失败");
                throw;
            }
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual async Task<bool> UpdateAsync(Guid id, TUpdateDto updateDto)
        {
            try
            {
                // 获取现有实体
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("更新失败，实体不存在，ID: {Id}", id);
                    return false;
                }

                // 验证更新数据
                await ValidateUpdateAsync(existing, updateDto);

                // 映射更新数据到实体
                _mapper.Map(updateDto, existing);

                // 预处理
                await PreUpdateAsync(existing, updateDto);

                // 更新实体
                var result = await _repository.UpdateAsync(existing);
                if (result == null)
                {
                    _logger.LogWarning("更新实体失败，ID: {Id}", id);
                    return false;
                }

                // 后处理
                await PostUpdateAsync(result, updateDto);

                // 记录操作日志
                await LogOperationAsync("Update", result);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新实体失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                // 获取现有实体
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("删除失败，实体不存在，ID: {Id}", id);
                    return false;
                }

                // 验证删除权限
                await ValidateDeleteAsync(existing);

                // 预处理
                await PreDeleteAsync(existing);

                // 删除实体
                var result = await _repository.DeleteAsync(id);

                if (result)
                {
                    // 后处理
                    await PostDeleteAsync(existing);

                    // 记录操作日志
                    await LogOperationAsync("Delete", existing);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除实体失败，ID: {Id}", id);
                throw;
            }
        }

        #region 虚拟方法 - 供子类重写

        /// <summary>
        /// 验证创建数据
        /// </summary>
        protected virtual Task ValidateCreateAsync(TCreateDto createDto)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 验证更新数据
        /// </summary>
        protected virtual Task ValidateUpdateAsync(TEntity existing, TUpdateDto updateDto)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 验证删除权限
        /// </summary>
        protected virtual Task ValidateDeleteAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 创建前处理
        /// </summary>
        protected virtual Task PreCreateAsync(TEntity entity, TCreateDto createDto)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 创建后处理
        /// </summary>
        protected virtual Task PostCreateAsync(TEntity entity, TCreateDto createDto)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新前处理
        /// </summary>
        protected virtual Task PreUpdateAsync(TEntity entity, TUpdateDto updateDto)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新后处理
        /// </summary>
        protected virtual Task PostUpdateAsync(TEntity entity, TUpdateDto updateDto)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除前处理
        /// </summary>
        protected virtual Task PreDeleteAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除后处理
        /// </summary>
        protected virtual Task PostDeleteAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        protected virtual async Task LogOperationAsync(string operation, TEntity entity)
        {
            // UltraThink重构：使用标准ILogger替代复杂日志服务
            try
            {
                _logger.LogInformation("操作: {Operation}, 实体: {EntityType}", operation, typeof(TEntity).Name);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录操作日志失败，操作: {Operation}", operation);
            }
        }

        #endregion
    }
}