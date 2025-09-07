using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Coordinators
{

    /// <summary>
    /// 数据协调器接口 - UltraThink架构的数据操作协调
    /// 为各种业务模块提供统一的数据操作协调模式
    /// </summary>
    /// <typeparam name="TDto">数据传输对象类型</typeparam>
    /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
    /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
    public interface IDataCoordinator<TDto, TCreateDto, TUpdateDto>
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {

        #region Events

        /// <summary>
        /// 数据变化事件
        /// </summary>
        event EventHandler<DataChangedEventArgs<TDto>>? DataChanged;

        /// <summary>
        /// 操作进度事件
        /// </summary>
        event EventHandler<OperationProgressEventArgs>? OperationProgress;

        #endregion Events

        #region Query Operations

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据ID获取详情
        /// </summary>
        Task<ServiceResult<TDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 搜索
        /// </summary>
        Task<ServiceResult<List<TDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取所有活跃项
        /// </summary>
        Task<ServiceResult<List<TDto>>> GetActiveAsync();

        #endregion Query Operations

        #region CRUD Operations

        /// <summary>
        /// 创建
        /// </summary>
        Task<ServiceResult<TDto>> CreateAsync(TCreateDto createDto);

        /// <summary>
        /// 更新
        /// </summary>
        Task<ServiceResult<TDto>> UpdateAsync(Guid id, TUpdateDto updateDto);

        /// <summary>
        /// 删除
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        #endregion CRUD Operations

        #region Status Operations

        /// <summary>
        /// 启用
        /// </summary>
        Task<ServiceResult<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 禁用
        /// </summary>
        Task<ServiceResult<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 批量启用
        /// </summary>
        Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 批量禁用
        /// </summary>
        Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);

        #endregion Status Operations

        #region Validation

        /// <summary>
        /// 验证数据是否有效
        /// </summary>
        Task<ServiceResult<bool>> ValidateAsync(TCreateDto createDto);

        /// <summary>
        /// 验证更新数据是否有效
        /// </summary>
        Task<ServiceResult<bool>> ValidateUpdateAsync(Guid id, TUpdateDto updateDto);

        #endregion Validation

        #region Cache Management

        /// <summary>
        /// 刷新缓存
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// 清除缓存
        /// </summary>
        void ClearCache();

        #endregion Cache Management
    }

    /// <summary>
    /// 数据变化事件参数
    /// </summary>
    public class DataChangedEventArgs<TDto> : EventArgs
    {
        public DataChangeType ChangeType { get; }
        public TDto? Item { get; }
        public List<TDto>? Items { get; }

        public DataChangedEventArgs(DataChangeType changeType, TDto item)
        {
            ChangeType = changeType;
            Item = item;
        }

        public DataChangedEventArgs(DataChangeType changeType, List<TDto> items)
        {
            ChangeType = changeType;
            Items = items;
        }
    }

    /// <summary>
    /// 数据变化类型
    /// </summary>
    public enum DataChangeType
    {
        Created,
        Updated,
        Deleted,
        StatusChanged,
        BatchUpdated,
        Refreshed
    }

    /// <summary>
    /// 操作进度事件参数
    /// </summary>
    public class OperationProgressEventArgs : EventArgs
    {
        public string OperationName { get; }
        public int Current { get; }
        public int Total { get; }
        public string? Message { get; }

        public OperationProgressEventArgs(string operationName, int current, int total, string? message = null)
        {
            OperationName = operationName;
            Current = current;
            Total = total;
            Message = message;
        }
    }
}
