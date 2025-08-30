using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Base
{
    /// <summary>
    /// 通用查询助手基类 - UltraThink Helper重构
    /// 抽取各模块QueryHelper中的通用查询逻辑
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TDto">DTO类型</typeparam>
    /// <typeparam name="TRepository">仓储接口类型</typeparam>
    public abstract class BaseQueryHelper<TEntity, TDto, TRepository>
        where TEntity : class
        where TDto : class
        where TRepository : class
    {
        protected readonly TRepository Repository;
        protected readonly IMapper Mapper;
        protected readonly ILogger Logger;

        protected BaseQueryHelper(
            TRepository repository,
            IMapper mapper,
            ILogger logger)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础查询模板

        /// <summary>
        /// 通用ID查询模板
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="getEntityFunc">获取实体的函数</param>
        /// <param name="entityName">实体名称（用于日志）</param>
        /// <returns>查询结果</returns>
        protected async Task<ServiceResult<TDto>> GetByIdAsync(
            Guid id,
            Func<Guid, Task<TEntity>> getEntityFunc,
            string entityName = "记录")
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<TDto>.Failure($"{entityName}ID不能为空");
                }

                var entity = await getEntityFunc(id);
                if (entity == null)
                {
                    return ServiceResult<TDto>.Failure($"{entityName}不存在");
                }

                var dto = Mapper.Map<TDto>(entity);
                return ServiceResult<TDto>.Success(dto);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取{EntityName}详情失败 - ID: {Id}", entityName, id);
                return ServiceResult<TDto>.Failure($"获取{entityName}详情失败");
            }
        }

        /// <summary>
        /// 通用分页查询模板
        /// </summary>
        /// <param name="request">分页查询请求</param>
        /// <param name="getEntitiesFunc">获取实体列表的函数</param>
        /// <param name="countFunc">获取总数的函数</param>
        /// <param name="entityName">实体名称（用于日志）</param>
        /// <returns>分页查询结果</returns>
        protected async Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(
            PagedQueryDto request,
            Func<PagedQueryDto, Task<IEnumerable<TEntity>>> getEntitiesFunc,
            Func<PagedQueryDto, Task<int>> countFunc,
            string entityName = "记录")
        {
            try
            {
                // 验证分页参数
                var validationResult = ValidatePagedRequest(request);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PagedResult<TDto>>.Failure(validationResult.ErrorMessage);
                }

                // 获取数据和总数
                var entities = await getEntitiesFunc(request);
                var totalCount = await countFunc(request);

                // 映射为DTO
                var dtoList = Mapper.Map<List<TDto>>(entities);

                var result = new PagedResult<TDto>
                {
                    Items = dtoList,
                    TotalCount = totalCount,
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };

                return ServiceResult<PagedResult<TDto>>.Success(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "分页查询{EntityName}失败 - 页码: {PageIndex}, 页大小: {PageSize}", 
                    entityName, request.PageIndex, request.PageSize);
                return ServiceResult<PagedResult<TDto>>.Failure($"查询{entityName}列表失败");
            }
        }

        /// <summary>
        /// 通用列表查询模板
        /// </summary>
        /// <param name="getEntitiesFunc">获取实体列表的函数</param>
        /// <param name="entityName">实体名称（用于日志）</param>
        /// <returns>列表查询结果</returns>
        protected async Task<ServiceResult<List<TDto>>> GetListAsync(
            Func<Task<IEnumerable<TEntity>>> getEntitiesFunc,
            string entityName = "记录")
        {
            try
            {
                var entities = await getEntitiesFunc();
                var dtoList = Mapper.Map<List<TDto>>(entities);
                
                Logger.LogInformation("查询{EntityName}列表成功 - 返回{Count}条记录", entityName, dtoList.Count);
                return ServiceResult<List<TDto>>.Success(dtoList);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查询{EntityName}列表失败", entityName);
                return ServiceResult<List<TDto>>.Failure($"查询{entityName}列表失败");
            }
        }

        /// <summary>
        /// 通用搜索查询模板
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="searchFunc">搜索函数</param>
        /// <param name="entityName">实体名称（用于日志）</param>
        /// <returns>搜索结果</returns>
        protected async Task<ServiceResult<List<TDto>>> SearchAsync(
            string keyword,
            Func<string, Task<IEnumerable<TEntity>>> searchFunc,
            string entityName = "记录")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<TDto>>.Success(new List<TDto>());
                }

                var entities = await searchFunc(keyword.Trim());
                var dtoList = Mapper.Map<List<TDto>>(entities);
                
                Logger.LogInformation("搜索{EntityName}成功 - 关键词: {Keyword}, 返回{Count}条记录", 
                    entityName, keyword, dtoList.Count);
                return ServiceResult<List<TDto>>.Success(dtoList);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索{EntityName}失败 - 关键词: {Keyword}", entityName, keyword);
                return ServiceResult<List<TDto>>.Failure($"搜索{entityName}失败");
            }
        }

        #endregion

        #region 通用统计模板

        /// <summary>
        /// 通用计数查询模板
        /// </summary>
        /// <param name="countFunc">计数函数</param>
        /// <param name="entityName">实体名称（用于日志）</param>
        /// <returns>计数结果</returns>
        protected async Task<ServiceResult<int>> GetCountAsync(
            Func<Task<int>> countFunc,
            string entityName = "记录")
        {
            try
            {
                var count = await countFunc();
                Logger.LogInformation("统计{EntityName}数量成功 - 总数: {Count}", entityName, count);
                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "统计{EntityName}数量失败", entityName);
                return ServiceResult<int>.Failure($"统计{entityName}数量失败");
            }
        }

        /// <summary>
        /// 通用存在性检查模板
        /// </summary>
        /// <param name="checkFunc">检查函数</param>
        /// <param name="description">检查描述</param>
        /// <returns>检查结果</returns>
        protected async Task<ServiceResult<bool>> ExistsAsync(
            Func<Task<bool>> checkFunc,
            string description)
        {
            try
            {
                var exists = await checkFunc();
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查{Description}失败", description);
                return ServiceResult<bool>.Failure($"检查{description}失败");
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 验证分页请求参数
        /// </summary>
        /// <param name="request">分页请求</param>
        /// <returns>验证结果</returns>
        private static ServiceResult<bool> ValidatePagedRequest(PagedQueryDto request)
        {
            if (request == null)
            {
                return ServiceResult<bool>.Failure("分页请求不能为空");
            }

            if (request.PageIndex < 1)
            {
                return ServiceResult<bool>.Failure("页码必须大于0");
            }

            if (request.PageSize < 1 || request.PageSize > 1000)
            {
                return ServiceResult<bool>.Failure("页大小必须在1-1000之间");
            }

            return ServiceResult<bool>.Success(true);
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 子类需要实现的获取实体名称方法
        /// </summary>
        /// <returns>实体名称</returns>
        protected abstract string GetEntityName();

        #endregion
    }
}