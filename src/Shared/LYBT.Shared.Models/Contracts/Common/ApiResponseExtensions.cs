namespace LYBT.Shared.Models.Contracts.Common
{

    /// <summary>
    /// ApiResponse 扩展方法 - 统一响应格式创建
    /// UltraThink v2.0 架构标准：统一所有响应格式为 ApiResponse
    /// </summary>
    public static class ApiResponseExtensions
    {

        /// <summary>
        /// 创建成功的分页响应
        /// </summary>
        /// <typeparam name="T">数据项类型</typeparam>
        /// <param name="items">数据项列表</param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="message">响应消息</param>
        /// <returns>统一的分页API响应</returns>
        public static ApiResponse<PagedResult<T>> CreatePagedSuccess<T>(
            IList<T> items,
            int totalCount,
            int currentPage,
            int pageSize,
            string message = "查询成功")
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedData = new PagedResult<T>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                CurrentPage = currentPage,
                PageSize = pageSize

                // TotalPages 是计算属性，无需手动设置
            };

            return ApiResponse<PagedResult<T>>.CreateSuccess(pagedData, message);
        }
    }
}
