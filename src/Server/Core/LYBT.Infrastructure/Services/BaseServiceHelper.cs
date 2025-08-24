using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// BaseService助手类 - UltraThink渐进式重构工具
    /// 为现有Service提供BaseService功能，支持逐步重构而不破坏现有代码
    /// </summary>
    public static class BaseServiceHelper
    {
        /// <summary>
        /// 通用的GetById操作
        /// </summary>
        public static async Task<ServiceResult<TDto>> ExecuteGetByIdAsync<TEntity, TDto>(
            Func<Task<TEntity?>> getEntityFunc,
            IMapper mapper,
            ILogger logger,
            string entityName,
            Guid id)
            where TEntity : class
            where TDto : class
        {
            try
            {
                var entity = await getEntityFunc();
                if (entity == null)
                    return ServiceResult<TDto>.Failure($"{entityName}不存在");

                var dto = mapper.Map<TDto>(entity);
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取{EntityName}详情失败: {Id}", entityName, id);
                return ServiceResult<TDto>.Failure($"获取{entityName}详情失败", ex);
            }
        }

        /// <summary>
        /// 通用的分页查询操作
        /// </summary>
        public static async Task<ServiceResult<PagedResult<TDto>>> ExecuteGetPagedAsync<TEntity, TDto, TQueryDto>(
            TQueryDto query,
            Func<TQueryDto, Task<(List<TEntity> items, int totalCount)>> getPagedFunc,
            IMapper mapper,
            ILogger logger,
            string entityName)
            where TEntity : class
            where TDto : class
            where TQueryDto : PagedQueryBaseDto
        {
            try
            {
                var (items, totalCount) = await getPagedFunc(query);
                var dtos = mapper.Map<List<TDto>>(items);

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
                logger.LogError(ex, "分页查询{EntityName}失败", entityName);
                return ServiceResult<PagedResult<TDto>>.Failure($"分页查询{entityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的创建操作
        /// </summary>
        public static async Task<ServiceResult<TDto>> ExecuteCreateAsync<TEntity, TDto, TCreateDto>(
            TCreateDto createDto,
            Func<TCreateDto, TEntity> createEntityFunc,
            Func<TEntity, Task> saveFunc,
            IMapper mapper,
            ILogger logger,
            string entityName,
            Func<TEntity, object> getEntityIdFunc)
            where TEntity : class
            where TDto : class
            where TCreateDto : class
        {
            try
            {
                var entity = createEntityFunc(createDto);
                await saveFunc(entity);

                var dto = mapper.Map<TDto>(entity);
                logger.LogInformation("创建{EntityName}成功: {EntityId}", entityName, getEntityIdFunc(entity));
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "创建{EntityName}失败", entityName);
                return ServiceResult<TDto>.Failure($"创建{entityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的更新操作
        /// </summary>
        public static async Task<ServiceResult<TDto>> ExecuteUpdateAsync<TEntity, TDto, TUpdateDto>(
            Guid id,
            TUpdateDto updateDto,
            Func<Task<TEntity?>> getEntityFunc,
            Action<TEntity, TUpdateDto> updateEntityFunc,
            Func<Task> saveFunc,
            IMapper mapper,
            ILogger logger,
            string entityName)
            where TEntity : class
            where TDto : class
            where TUpdateDto : class
        {
            try
            {
                var entity = await getEntityFunc();
                if (entity == null)
                    return ServiceResult<TDto>.Failure($"{entityName}不存在");

                updateEntityFunc(entity, updateDto);
                await saveFunc();

                var dto = mapper.Map<TDto>(entity);
                logger.LogInformation("更新{EntityName}成功: {Id}", entityName, id);
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "更新{EntityName}失败: {Id}", entityName, id);
                return ServiceResult<TDto>.Failure($"更新{entityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的搜索操作
        /// </summary>
        public static async Task<ServiceResult<List<TDto>>> ExecuteSearchAsync<TEntity, TDto>(
            string keyword,
            Func<string, Task<List<TEntity>>> searchFunc,
            IMapper mapper,
            ILogger logger,
            string entityName)
            where TEntity : class
            where TDto : class
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<TDto>>.Success(new List<TDto>());

                var entities = await searchFunc(keyword);
                var dtos = mapper.Map<List<TDto>>(entities);
                return ServiceResult<List<TDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "搜索{EntityName}失败: {Keyword}", entityName, keyword);
                return ServiceResult<List<TDto>>.Failure($"搜索{entityName}失败", ex);
            }
        }

        /// <summary>
        /// 通用的批量状态更新操作（使用ExecuteUpdate）
        /// </summary>
        public static async Task<ServiceResult<int>> ExecuteBatchUpdateStatusAsync<TEntity>(
            List<Guid> ids,
            CommonStatus status,
            Func<List<Guid>, CommonStatus, Task<int>> updateFunc,
            ILogger logger,
            string entityName)
            where TEntity : class
        {
            try
            {
                if (ids == null || ids.Count == 0)
                    return ServiceResult<int>.Failure("ID列表不能为空");

                var affectedCount = await updateFunc(ids, status);
                var operation = status == CommonStatus.Enabled ? "启用" : "禁用";
                logger.LogInformation("批量{Operation}{EntityName}成功: 影响{Count}条记录", operation, entityName, affectedCount);
                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                var operation = status == CommonStatus.Enabled ? "启用" : "禁用";
                logger.LogError(ex, "批量{Operation}{EntityName}失败", operation, entityName);
                return ServiceResult<int>.Failure($"批量{operation}{entityName}失败", ex);
            }
        }

        /// <summary>
        /// 安全执行Helper方法调用
        /// </summary>
        public static async Task<ServiceResult<T>> ExecuteHelperMethodAsync<T>(
            Func<Task<ServiceResult<T>>> helperMethod,
            ILogger logger,
            string operationName,
            object? context = null)
        {
            try
            {
                return await helperMethod();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{OperationName}失败: {Context}", operationName, context);
                return ServiceResult<T>.Failure($"{operationName}失败", ex);
            }
        }

        /// <summary>
        /// 验证GUID是否有效
        /// </summary>
        public static bool IsValidGuid(Guid id)
        {
            return id != Guid.Empty;
        }

        /// <summary>
        /// 标准化错误消息
        /// </summary>
        public static string FormatErrorMessage(string entityName, string operation, Exception? ex = null)
        {
            var baseMessage = $"{operation}{entityName}失败";
            return ex != null ? $"{baseMessage}: {ex.Message}" : baseMessage;
        }
    }
}