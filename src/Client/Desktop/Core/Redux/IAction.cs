namespace LYBT.Desktop.Core.Redux {

    /// <summary>
    /// Redux Action接口 - 所有Action的基础接口
    /// </summary>
    public interface IAction {

        /// <summary>
        /// Action类型标识
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Action时间戳
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Action来源（用于调试）
        /// </summary>
        string? Source { get; }
    }

    /// <summary>
    /// 带负载的Action
    /// </summary>
    public interface IAction<TPayload> : IAction {

        /// <summary>
        /// Action负载数据
        /// </summary>
        TPayload Payload { get; }
    }

    /// <summary>
    /// 基础Action实现
    /// </summary>
    public abstract class ActionBase : IAction {
        public string Type { get; }
        public DateTimeOffset Timestamp { get; }
        public string? Source { get; set; }

        protected ActionBase(string type) {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// 带负载的Action基类
    /// </summary>
    public abstract class ActionBase<TPayload> : ActionBase, IAction<TPayload> {
        public TPayload Payload { get; }

        protected ActionBase(string type, TPayload payload) : base(type) {
            Payload = payload;
        }
    }

    /// <summary>
    /// 异步Action接口
    /// </summary>
    public interface IAsyncAction : IAction {

        /// <summary>
        /// 是否正在执行
        /// </summary>
        bool IsExecuting { get; }

        /// <summary>
        /// 执行进度（0-100）
        /// </summary>
        int Progress { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        string? Error { get; }
    }

    /// <summary>
    /// Action创建器
    /// </summary>
    public static class ActionCreator {

        /// <summary>
        /// 创建简单Action
        /// </summary>
        public static SimpleAction Create(string type) {
            return new SimpleAction(type);
        }

        /// <summary>
        /// 创建带负载的Action
        /// </summary>
        public static PayloadAction<T> Create<T>(string type, T payload) {
            return new PayloadAction<T>(type, payload);
        }

        /// <summary>
        /// 创建异步开始Action
        /// </summary>
        public static AsyncStartAction CreateAsyncStart(string type) {
            return new AsyncStartAction(type);
        }

        /// <summary>
        /// 创建异步成功Action
        /// </summary>
        public static AsyncSuccessAction<T> CreateAsyncSuccess<T>(string type, T result) {
            return new AsyncSuccessAction<T>(type, result);
        }

        /// <summary>
        /// 创建异步失败Action
        /// </summary>
        public static AsyncErrorAction CreateAsyncError(string type, string error) {
            return new AsyncErrorAction(type, error);
        }
    }

    #region 具体Action实现

    /// <summary>
    /// 简单Action（无负载）
    /// </summary>
    public class SimpleAction : ActionBase {

        public SimpleAction(string type) : base(type) {
        }
    }

    /// <summary>
    /// 带负载的Action
    /// </summary>
    public class PayloadAction<TPayload> : ActionBase<TPayload> {

        public PayloadAction(string type, TPayload payload) : base(type, payload) {
        }
    }

    /// <summary>
    /// 异步开始Action
    /// </summary>
    public class AsyncStartAction : ActionBase, IAsyncAction {
        public bool IsExecuting => true;
        public int Progress => 0;
        public string? Error => null;

        public AsyncStartAction(string type) : base($"{type}_START") {
        }
    }

    /// <summary>
    /// 异步成功Action
    /// </summary>
    public class AsyncSuccessAction<TResult> : ActionBase<TResult>, IAsyncAction {
        public bool IsExecuting => false;
        public int Progress => 100;
        public string? Error => null;

        public AsyncSuccessAction(string type, TResult result)
            : base($"{type}_SUCCESS", result) { }
    }

    /// <summary>
    /// 异步错误Action
    /// </summary>
    public class AsyncErrorAction : ActionBase, IAsyncAction {
        public bool IsExecuting => false;
        public int Progress => 0;
        public string? Error { get; }

        public AsyncErrorAction(string type, string error) : base($"{type}_ERROR") {
            Error = error;
        }
    }

    #endregion 具体Action实现
}
