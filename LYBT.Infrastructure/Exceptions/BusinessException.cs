namespace LYBT.Infrastructure.Exceptions {

    /// <summary>
    /// 业务异常（用于主动抛出业务错误）
    /// </summary>
    public class BusinessException : Exception {

        /// <summary>
        /// 执行base操作。
        /// </summary>
        /// <param name="message">参数message</param>
        /// <returns>返回值</returns>
        public BusinessException(string message) : base(message) {
        }
    }
}