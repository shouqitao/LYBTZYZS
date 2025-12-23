using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Repositories
{
    /// <summary>
    /// Client端Repository统一基类，标准化HTTP API调用包装
    /// RESTful设计：List返回轻量DTO，Detail返回完整DTO
    /// </summary>
    /// <typeparam name="TDetailDto">详情DTO类型（用于单项查询、创建、更新返回）</typeparam>
    /// <typeparam name="TListDto">列表DTO类型（用于分页查询返回）</typeparam>
    /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
    /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
    /// <typeparam name="TApi">Refit API接口类型</typeparam>
    public abstract class RepositoryBase<TDetailDto, TListDto, TCreateDto, TUpdateDto, TApi>
        where TApi : class
        where TDetailDto : class
        where TListDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        protected readonly TApi _api;
        protected readonly ILogger _logger;

        /// <summary>
        /// 初始化RepositoryBase实例
        /// </summary>
        /// <param name="api">Refit API接口实例</param>
        /// <param name="logger">日志记录器实例</param>
        protected RepositoryBase(TApi api, ILogger logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 标准CRUD操作

        /// <summary>
        /// 根据ID获取实体详情
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>实体详情DTO</returns>
        public virtual async Task<TDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await CallApiGetByIdAsync(id);
                return response.Data ?? throw new InvalidOperationException($"获取失败：ID {id} 的实体不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体详情失败，ID: {Id}, 类型: {EntityType}", id, typeof(TDetailDto).Name);
                throw;
            }
        }

        /// <summary>
        /// 分页查询实体列表（返回轻量级ListDto）
        /// </summary>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">搜索关键词（可选）</param>
        /// <returns>分页结果</returns>
        public virtual async Task<PagedResult<TListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await CallApiGetPagedAsync(page, pageSize, keyword);
                return response.Data ?? new PagedResult<TListDto>
                {
                    Items = new List<TListDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询失败，Page: {Page}, PageSize: {PageSize}, Keyword: {Keyword}, 类型: {EntityType}",
                    page, pageSize, keyword, typeof(TListDto).Name);
                throw;
            }
        }

        /// <summary>
        /// 创建新实体
        /// </summary>
        /// <param name="dto">创建DTO</param>
        /// <returns>创建的实体详情</returns>
        public virtual async Task<TDetailDto> CreateAsync(TCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await CallApiCreateAsync(dto);

                if (!response.Success)
                {
                    throw new InvalidOperationException($"创建失败：{response.Message ?? "未知错误"}");
                }

                return response.Data ?? throw new InvalidOperationException("创建失败：服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建实体失败，类型: {EntityType}", typeof(TDetailDto).Name);
                throw;
            }
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="dto">更新DTO</param>
        /// <returns>更新后的实体详情</returns>
        public virtual async Task<TDetailDto> UpdateAsync(TUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var id = GetIdFromUpdateDto(dto);
            if (id == null || id == Guid.Empty)
            {
                _logger.LogError("更新实体失败：无效的ID，类型: {EntityType}", typeof(TDetailDto).Name);
                throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));
            }

            try
            {
                var response = await CallApiUpdateAsync(id.Value, dto);

                if (!response.Success)
                {
                    throw new InvalidOperationException($"更新失败：{response.Message ?? "未知错误"}");
                }

                return response.Data ?? throw new InvalidOperationException($"更新失败：ID {id.Value} 的实体不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新实体失败，ID: {Id}, 类型: {EntityType}", id, typeof(TDetailDto).Name);
                throw;
            }
        }

        /// <summary>
        /// 删除实体（软删除）
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>删除是否成功</returns>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await CallApiDeleteAsync(id);
                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除实体失败，ID: {Id}, 类型: {EntityType}", id, typeof(TDetailDto).Name);
                return false;
            }
        }

        /// <summary>
        /// 搜索实体（基于关键词）
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的实体列表</returns>
        public virtual async Task<List<TListDto>> SearchAsync(string keyword)
        {
            try
            {
                var response = await CallApiGetPagedAsync(1, 100, keyword);
                return response.Data?.Items ?? new List<TListDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索实体失败，Keyword: {Keyword}, 类型: {EntityType}", keyword, typeof(TListDto).Name);
                throw;
            }
        }

        #endregion

        #region 抽象方法 - 子类必须实现具体的API调用

        /// <summary>
        /// 调用API根据ID获取实体详情
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>API响应（DetailDto）</returns>
        protected abstract Task<ApiResponse<TDetailDto>> CallApiGetByIdAsync(Guid id);

        /// <summary>
        /// 调用API分页查询实体列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>API响应（ListDto）</returns>
        protected abstract Task<ApiResponse<PagedResult<TListDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword);

        /// <summary>
        /// 调用API创建实体
        /// </summary>
        /// <param name="dto">创建DTO</param>
        /// <returns>API响应（DetailDto）</returns>
        protected abstract Task<ApiResponse<TDetailDto>> CallApiCreateAsync(TCreateDto dto);

        /// <summary>
        /// 调用API更新实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="dto">更新DTO</param>
        /// <returns>API响应（DetailDto）</returns>
        protected abstract Task<ApiResponse<TDetailDto>> CallApiUpdateAsync(Guid id, TUpdateDto dto);

        /// <summary>
        /// 调用API删除实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>API响应</returns>
        protected abstract Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id);

        /// <summary>
        /// 从更新DTO中提取ID
        /// </summary>
        /// <param name="dto">更新DTO</param>
        /// <returns>实体ID</returns>
        protected abstract Guid? GetIdFromUpdateDto(TUpdateDto dto);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证分页参数
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        protected virtual void ValidatePagingParameters(int page, int pageSize)
        {
            if (page < 1)
                throw new ArgumentException("页码必须大于0", nameof(page));

            if (pageSize < 1 || pageSize > 1000)
                throw new ArgumentException("每页大小必须在1-1000之间", nameof(pageSize));
        }

        /// <summary>
        /// 验证GUID参数
        /// </summary>
        /// <param name="id">GUID</param>
        /// <param name="parameterName">参数名称</param>
        protected virtual void ValidateGuid(Guid id, string parameterName = "id")
        {
            if (id == Guid.Empty)
                throw new ArgumentException($"{parameterName}不能为空GUID", parameterName);
        }

        /// <summary>
        /// 记录操作成功日志
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <param name="entityId">实体ID</param>
        protected virtual void LogOperationSuccess(string operation, Guid entityId)
        {
            _logger.LogInformation("{Operation}成功，ID: {Id}, 类型: {EntityType}",
                operation, entityId, typeof(TDetailDto).Name);
        }

        /// <summary>
        /// 记录操作失败日志
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="exception">异常信息</param>
        protected virtual void LogOperationFailure(string operation, Guid entityId, Exception exception)
        {
            _logger.LogError(exception, "{Operation}失败，ID: {Id}, 类型: {EntityType}",
                operation, entityId, typeof(TDetailDto).Name);
        }

        #endregion
    }
}
