using System;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    public class TransactionResult<TResult>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 结果数据
        /// </summary>
        public TResult Data { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; set; }
        
        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static TransactionResult<TResult> FromSuccess(TResult data)
        {
            return new TransactionResult<TResult>
            {
                Success = true,
                Data = data
            };
        }
        
        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static TransactionResult<TResult> FromError(string errorMessage, Exception exception = null)
        {
            return new TransactionResult<TResult>
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
}