using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// 服务基类 - UltraThink v2.0架构标准
    /// 提供通用的CRUD操作模式、ServiceResult包装和异常处理
    /// 符合当前系统的Helper模式和ServiceResult模式
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TDto">DTO类型</typeparam>
    /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
    /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
    public abstract class BaseService<TEntity, TDto, TCreateDto, TUpdateDto>
        where TEntity : class
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        protected readonly AppDbContext _context;
        protected readonly IMapper _mapper;
        protected readonly ILogger _logger;

        /// <summary>
        /// 实体名称（用于日志和错误消息）
        /// </summary>
        protected abstract string EntityName { get; }

        protected BaseService(AppDbContext context, IMapper mapper, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 通用CRUD操作 - ServiceResult模式

        /// <summary>
        /// 通用的获取详情方法
        /// </summary>
        protected async Task<ServiceResult<TDto>> GetByIdCoreAsync(Guid id, Func<Task<TEntity?>> getEntityFunc)
        {
            try
            {
                var entity = await getEntityFunc();
                if (entity == null)
                    return ServiceResult<TDto>.Failure($"{EntityName}不存在");

                var dto = _mapper.Map<TDto>(entity);
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取{EntityName}详情失败: {Id}", EntityName, id);
                return ServiceResult<TDto>.Failure($"获取{EntityName}详情失败", ex);
            }
        }

        /// <summary>
        /// 通用的创建方法
        /// </summary>
        protected async Task<ServiceResult<TDto>> CreateCoreAsync(TCreateDto createDto, Func<TCreateDto, TEntity> createEntityFunc)
        {
            try
            {
                var entity = createEntityFunc(createDto);
                _context.Set<TEntity>().Add(entity);
                await _context.SaveChangesAsync();

                var dto = _mapper.Map<TDto>(entity);
                _logger.LogInformation("创建{EntityName}成功: {EntityId}", EntityName, GetEntityId(entity));
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建{EntityName}失败", EntityName);
                return ServiceResult<TDto>.Failure($"创建{EntityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的更新方法
        /// </summary>
        protected async Task<ServiceResult<TDto>> UpdateCoreAsync(Guid id, TUpdateDto updateDto, 
            Func<Task<TEntity?>> getEntityFunc, Action<TEntity, TUpdateDto> updateEntityFunc)
        {
            try
            {
                var entity = await getEntityFunc();
                if (entity == null)
                    return ServiceResult<TDto>.Failure($"{EntityName}不存在");

                updateEntityFunc(entity, updateDto);
                await _context.SaveChangesAsync();

                var dto = _mapper.Map<TDto>(entity);
                _logger.LogInformation("更新{EntityName}成功: {Id}", EntityName, id);
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新{EntityName}失败: {Id}", EntityName, id);
                return ServiceResult<TDto>.Failure($"更新{EntityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的软删除方法（适用于有Status属性的实体）
        /// </summary>
        protected async Task<ServiceResult<bool>> SoftDeleteCoreAsync(Guid id, Func<Task<TEntity?>> getEntityFunc, Action<TEntity> disableEntityFunc)
        {
            try
            {
                var entity = await getEntityFunc();
                if (entity == null)
                    return ServiceResult<bool>.Failure($"{EntityName}不存在");

                disableEntityFunc(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("删除{EntityName}成功: {Id}", EntityName, id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除{EntityName}失败: {Id}", EntityName, id);
                return ServiceResult<bool>.Failure($"删除{EntityName}失败", ex);
            }
        }

        #endregion

        #region 通用查询操作

        /// <summary>
        /// 通用的分页查询方法
        /// </summary>
        protected async Task<ServiceResult<PagedResult<TDto>>> GetPagedCoreAsync<TQueryDto>(
            TQueryDto query, Func<TQueryDto, Task<(List<TEntity> items, int totalCount)>> getPagedFunc)
            where TQueryDto : PagedQueryBaseDto
        {
            try
            {
                var (items, totalCount) = await getPagedFunc(query);
                var dtos = _mapper.Map<List<TDto>>(items);

                var result = new PagedResult<TDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<TDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询{EntityName}失败", EntityName);
                return ServiceResult<PagedResult<TDto>>.Failure($"分页查询{EntityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的列表查询方法
        /// </summary>
        protected async Task<ServiceResult<List<TDto>>> GetListCoreAsync(Func<Task<List<TEntity>>> getListFunc)
        {
            try
            {
                var entities = await getListFunc();
                var dtos = _mapper.Map<List<TDto>>(entities);
                return ServiceResult<List<TDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询{EntityName}列表失败", EntityName);
                return ServiceResult<List<TDto>>.Failure($"查询{EntityName}列表失败", ex);
            }
        }

        /// <summary>
        /// 通用的搜索方法
        /// </summary>
        protected async Task<ServiceResult<List<TDto>>> SearchCoreAsync(string keyword, Func<string, Task<List<TEntity>>> searchFunc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<TDto>>.Success(new List<TDto>());

                var entities = await searchFunc(keyword);
                var dtos = _mapper.Map<List<TDto>>(entities);
                return ServiceResult<List<TDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索{EntityName}失败: {Keyword}", EntityName, keyword);
                return ServiceResult<List<TDto>>.Failure($"搜索{EntityName}失败", ex);
            }
        }

        #endregion

        #region 通用业务操作

        /// <summary>
        /// 通用的启用/禁用操作
        /// </summary>
        protected async Task<ServiceResult<bool>> ToggleStatusCoreAsync(Guid id, CommonStatus newStatus, 
            Func<Task<TEntity?>> getEntityFunc, Action<TEntity, CommonStatus> setStatusFunc)
        {
            try
            {
                var entity = await getEntityFunc();
                if (entity == null)
                    return ServiceResult<bool>.Failure($"{EntityName}不存在");

                setStatusFunc(entity, newStatus);
                await _context.SaveChangesAsync();

                var operation = newStatus == CommonStatus.Enabled ? "启用" : "禁用";
                _logger.LogInformation("{Operation}{EntityName}成功: {Id}", operation, EntityName, id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                var operation = newStatus == CommonStatus.Enabled ? "启用" : "禁用";
                _logger.LogError(ex, "{Operation}{EntityName}失败: {Id}", operation, EntityName, id);
                return ServiceResult<bool>.Failure($"{operation}{EntityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的批量操作方法
        /// </summary>
        protected async Task<ServiceResult<int>> BatchOperationCoreAsync<TId>(List<TId> ids, 
            Func<List<TId>, Task<int>> batchOperationFunc, string operationName)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                    return ServiceResult<int>.Failure("ID列表不能为空");

                var affectedCount = await batchOperationFunc(ids);
                _logger.LogInformation("批量{OperationName}{EntityName}成功: 影响{Count}条记录", operationName, EntityName, affectedCount);
                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量{OperationName}{EntityName}失败", operationName, EntityName);
                return ServiceResult<int>.Failure($"批量{operationName}{EntityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的批量状态更新（使用ExecuteUpdate避免内存加载）
        /// </summary>
        protected async Task<ServiceResult<int>> BatchUpdateStatusCoreAsync(List<Guid> ids, CommonStatus status, 
            Func<List<Guid>, CommonStatus, Task<int>> updateFunc)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                    return ServiceResult<int>.Failure("ID列表不能为空");

                var affectedCount = await updateFunc(ids, status);
                var operation = status == CommonStatus.Enabled ? "启用" : "禁用";
                _logger.LogInformation("批量{Operation}{EntityName}成功: 影响{Count}条记录", operation, EntityName, affectedCount);
                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                var operation = status == CommonStatus.Enabled ? "启用" : "禁用";
                _logger.LogError(ex, "批量{Operation}{EntityName}失败", operation, EntityName);
                return ServiceResult<int>.Failure($"批量{operation}{EntityName}失败", ex);
            }
        }

        #endregion

        #region Helper模式支持

        /// <summary>
        /// 执行安全操作（带日志和异常处理）
        /// 适用于Helper方法调用
        /// </summary>
        protected async Task<ServiceResult<T>> ExecuteSafelyAsync<T>(Func<Task<ServiceResult<T>>> operation, string operationName, object? context = null)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OperationName}失败: {Context}", operationName, context);
                return ServiceResult<T>.Failure($"{operationName}失败", ex);
            }
        }

        /// <summary>
        /// 执行安全操作（无返回值）
        /// </summary>
        protected async Task<ServiceResult<bool>> ExecuteSafelyAsync(Func<Task> operation, string operationName, object? context = null)
        {
            try
            {
                await operation();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OperationName}失败: {Context}", operationName, context);
                return ServiceResult<bool>.Failure($"{operationName}失败", ex);
            }
        }

        #endregion

        #region 抽象方法（子类必须实现）

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        protected abstract object GetEntityId(TEntity entity);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证GUID是否有效
        /// </summary>
        protected bool IsValidGuid(Guid id)
        {
            return id != Guid.Empty;
        }

        /// <summary>
        /// 创建实体的通用查询（启用状态）
        /// </summary>
        protected IQueryable<TEntity> CreateEnabledQuery()
        {
            var query = _context.Set<TEntity>().AsQueryable();
            
            // 如果实体有Status属性，自动过滤已禁用的记录
            if (typeof(TEntity).GetProperty("Status") != null)
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, "Status");
                var constant = System.Linq.Expressions.Expression.Constant(CommonStatus.Enabled);
                var equal = System.Linq.Expressions.Expression.Equal(property, constant);
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
                
                query = query.Where(lambda);
            }
            
            return query;
        }

        #endregion
    }

    /// <summary>
    /// 简化版BaseService - 不需要Update DTO的场景
    /// </summary>
    public abstract class BaseService<TEntity, TDto, TCreateDto> : BaseService<TEntity, TDto, TCreateDto, TDto>
        where TEntity : class
        where TDto : class
        where TCreateDto : class
    {
        protected BaseService(AppDbContext context, IMapper mapper, ILogger logger) 
            : base(context, mapper, logger)
        {
        }
    }

    /// <summary>
    /// 最简化版BaseService - 只有基本查询的场景
    /// </summary>
    public abstract class BaseService<TEntity, TDto> : BaseService<TEntity, TDto, TDto, TDto>
        where TEntity : class
        where TDto : class
    {
        protected BaseService(AppDbContext context, IMapper mapper, ILogger logger) 
            : base(context, mapper, logger)
        {
        }
    }
}