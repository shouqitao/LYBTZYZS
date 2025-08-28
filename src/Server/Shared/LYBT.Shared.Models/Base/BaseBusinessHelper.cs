using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;

namespace LYBT.Shared.Models.Base
{
    /// <summary>
    /// BusinessHelper基类 - UltraThink通用CRUD逻辑抽取
    /// 基于Prescription、User、Patient三个BusinessHelper重构经验总结
    /// 提供通用的异常处理、日志记录、操作模板等基础功能
    /// 代码行数：约150行，符合500行以下标准
    /// </summary>
    public abstract class BaseBusinessHelper<TLogger> where TLogger : class
    {
        protected readonly ILogger<TLogger> _logger;

        protected BaseBusinessHelper(ILogger<TLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 通用操作模板

        /// <summary>
        /// 执行安全的异步操作（通用模板）
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operation">操作函数</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="logData">日志数据</param>
        /// <param name="successMessage">成功消息模板</param>
        /// <param name="errorMessage">错误消息模板</param>
        /// <returns>操作结果</returns>
        protected async Task<ServiceResult<T>> ExecuteSafeOperationAsync<T>(
            Func<Task<ServiceResult<T>>> operation,
            string operationName,
            object? logData = null,
            string? successMessage = null,
            string? errorMessage = null)
        {
            try
            {
                _logger.LogInformation("开始执行操作: {OperationName}, 参数: {@LogData}", operationName, logData);
                
                var result = await operation();
                
                if (result.IsSuccess)
                {
                    var message = successMessage ?? $"操作成功: {operationName}";
                    _logger.LogInformation(message);
                }
                else
                {
                    var message = $"操作失败: {operationName}, 错误: {result.ErrorMessage}";
                    _logger.LogWarning(message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                var message = errorMessage ?? $"操作异常: {operationName}";
                _logger.LogError(ex, message);
                return ServiceResult<T>.Failure($"{message}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行安全的同步操作（通用模板）
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operation">操作函数</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="logData">日志数据</param>
        /// <param name="successMessage">成功消息模板</param>
        /// <param name="errorMessage">错误消息模板</param>
        /// <returns>操作结果</returns>
        protected ServiceResult<T> ExecuteSafeOperation<T>(
            Func<ServiceResult<T>> operation,
            string operationName,
            object? logData = null,
            string? successMessage = null,
            string? errorMessage = null)
        {
            try
            {
                _logger.LogInformation("开始执行同步操作: {OperationName}, 参数: {@LogData}", operationName, logData);
                
                var result = operation();
                
                if (result.IsSuccess)
                {
                    var message = successMessage ?? $"操作成功: {operationName}";
                    _logger.LogInformation(message);
                }
                else
                {
                    var message = $"操作失败: {operationName}, 错误: {result.ErrorMessage}";
                    _logger.LogWarning(message);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                var message = errorMessage ?? $"操作异常: {operationName}";
                _logger.LogError(ex, message);
                return ServiceResult<T>.Failure($"{message}: {ex.Message}", ex);
            }
        }

        #endregion

        #region 通用CRUD模板

        /// <summary>
        /// 通用创建操作模板
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
        /// <param name="createOperation">创建操作</param>
        /// <param name="dto">创建DTO</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>创建结果</returns>
        protected async Task<ServiceResult<TResult>> ExecuteCreateOperationAsync<TResult, TCreateDto>(
            Func<TCreateDto, Task<ServiceResult<TResult>>> createOperation,
            TCreateDto dto,
            string entityName)
        {
            return await ExecuteSafeOperationAsync(
                () => createOperation(dto),
                $"创建{entityName}",
                dto,
                $"创建{entityName}成功",
                $"创建{entityName}失败"
            );
        }

        /// <summary>
        /// 通用更新操作模板
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
        /// <param name="updateOperation">更新操作</param>
        /// <param name="id">实体ID</param>
        /// <param name="dto">更新DTO</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>更新结果</returns>
        protected async Task<ServiceResult<TResult>> ExecuteUpdateOperationAsync<TResult, TUpdateDto>(
            Func<Guid, TUpdateDto, Task<ServiceResult<TResult>>> updateOperation,
            Guid id,
            TUpdateDto dto,
            string entityName)
        {
            return await ExecuteSafeOperationAsync(
                () => updateOperation(id, dto),
                $"更新{entityName}",
                new { Id = id, Data = dto },
                $"更新{entityName}成功: {id}",
                $"更新{entityName}失败: {id}"
            );
        }

        /// <summary>
        /// 通用删除操作模板
        /// </summary>
        /// <param name="deleteOperation">删除操作</param>
        /// <param name="id">实体ID</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>删除结果</returns>
        protected async Task<ServiceResult<bool>> ExecuteDeleteOperationAsync(
            Func<Guid, Task<ServiceResult<bool>>> deleteOperation,
            Guid id,
            string entityName)
        {
            return await ExecuteSafeOperationAsync(
                () => deleteOperation(id),
                $"删除{entityName}",
                new { Id = id },
                $"删除{entityName}成功: {id}",
                $"删除{entityName}失败: {id}"
            );
        }

        #endregion

        #region 通用状态管理模板

        /// <summary>
        /// 通用启用操作模板
        /// </summary>
        /// <param name="enableOperation">启用操作</param>
        /// <param name="id">实体ID</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>启用结果</returns>
        protected async Task<ServiceResult<bool>> ExecuteEnableOperationAsync(
            Func<Guid, Task<ServiceResult<bool>>> enableOperation,
            Guid id,
            string entityName)
        {
            return await ExecuteSafeOperationAsync(
                () => enableOperation(id),
                $"启用{entityName}",
                new { Id = id },
                $"启用{entityName}成功: {id}",
                $"启用{entityName}失败: {id}"
            );
        }

        /// <summary>
        /// 通用禁用操作模板
        /// </summary>
        /// <param name="disableOperation">禁用操作</param>
        /// <param name="id">实体ID</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>禁用结果</returns>
        protected async Task<ServiceResult<bool>> ExecuteDisableOperationAsync(
            Func<Guid, Task<ServiceResult<bool>>> disableOperation,
            Guid id,
            string entityName)
        {
            return await ExecuteSafeOperationAsync(
                () => disableOperation(id),
                $"禁用{entityName}",
                new { Id = id },
                $"禁用{entityName}成功: {id}",
                $"禁用{entityName}失败: {id}"
            );
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 获取实体名称（子类实现）
        /// </summary>
        protected abstract string GetEntityName();

        #endregion
    }

    /// <summary>
    /// UltraThink BusinessHelper基类设计报告
    /// 
    /// 设计理念：
    /// 基于Prescription、User、Patient三个BusinessHelper重构经验，
    /// 抽取通用的操作模板和异常处理逻辑，减少重复代码。
    /// 
    /// 核心模式：
    /// 1. 安全操作模板 - 统一的try-catch异常处理
    /// 2. CRUD操作模板 - 标准化的增删改查操作
    /// 3. 状态管理模板 - 启用/禁用状态操作
    /// 4. 日志记录标准 - 一致的日志格式和级别
    /// 
    /// 使用示例：
    /// public class ExampleBusinessHelper : BaseBusinessHelper<ExampleBusinessHelper>
    /// {
    ///     public async Task<ServiceResult<ExampleDto>> CreateAsync(ExampleCreateDto dto)
    ///     {
    ///         return await ExecuteCreateOperationAsync(_service.CreateAsync, dto, GetEntityName());
    ///     }
    ///     
    ///     protected override string GetEntityName() => "示例实体";
    /// }
    /// 
    /// 收益：
    /// ✅ 减少重复代码 - 统一的操作模板
    /// ✅ 标准化异常处理 - 一致的错误处理策略
    /// ✅ 统一日志格式 - 便于调试和监控
    /// ✅ 代码可维护性 - 修改基类影响所有子类
    /// ✅ 开发效率 - 新增BusinessHelper时重用模板
    /// </summary>
}