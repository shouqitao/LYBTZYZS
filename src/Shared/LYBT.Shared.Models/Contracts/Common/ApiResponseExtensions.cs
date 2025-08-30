using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Models.Common;

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

        /// <summary>
        /// 从 PagedResult 创建分页响应
        /// </summary>
        /// <typeparam name="T">数据项类型</typeparam>
        /// <param name="pagedResult">分页结果</param>
        /// <param name="message">响应消息</param>
        /// <returns>统一的分页API响应</returns>
        public static ApiResponse<PagedResult<T>> CreatePagedSuccess<T>(
            PagedResult<T> pagedResult,
            string message = "查询成功")
        {
            return CreatePagedSuccess(
                pagedResult.Items,
                pagedResult.TotalCount,
                pagedResult.CurrentPage,
                pagedResult.PageSize,
                message
            );
        }

        /// <summary>
        /// 创建空数据的分页响应
        /// </summary>
        /// <typeparam name="T">数据项类型</typeparam>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="message">响应消息</param>
        /// <returns>空数据的分页响应</returns>
        public static ApiResponse<PagedResult<T>> CreateEmptyPagedSuccess<T>(
            int currentPage = 1,
            int pageSize = 10,
            string message = "查询成功，暂无数据")
        {
            return CreatePagedSuccess<T>(
                new List<T>(),
                0,
                currentPage,
                pageSize,
                message
            );
        }

        /// <summary>
        /// 从 ServiceResult 转换为 ApiResponse
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="serviceResult">服务层结果</param>
        /// <param name="successMessage">成功消息</param>
        /// <returns>API响应</returns>
        public static ApiResponse<T> ToApiResponse<T>(
            this ServiceResult<T> serviceResult, 
            string successMessage = "操作成功")
        {
            if (serviceResult.IsSuccess)
            {
                return ApiResponse<T>.CreateSuccess(serviceResult.Data, successMessage);
            }
            else
            {
                return ApiResponse<T>.CreateFail(
                    serviceResult.ErrorMessage ?? "操作失败", 
                    serviceResult.Exception?.Message
                );
            }
        }

        /// <summary>
        /// 从 ServiceResult 转换为 ApiResponse (无数据版本)
        /// </summary>
        /// <param name="serviceResult">服务层结果</param>
        /// <param name="successMessage">成功消息</param>
        /// <returns>API响应</returns>
        public static ApiResponse ToApiResponse(
            this ServiceResult serviceResult,
            string successMessage = "操作成功")
        {
            if (serviceResult.IsSuccess)
            {
                return ApiResponse.CreateSuccess(null, successMessage);
            }
            else
            {
                return ApiResponse.CreateFail(
                    serviceResult.ErrorMessage ?? "操作失败",
                    serviceResult.Exception?.Message
                );
            }
        }
    }
}