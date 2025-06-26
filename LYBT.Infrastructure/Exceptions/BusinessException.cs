namespace LYBT.Infrastructure.Exceptions {

    /// <summary>
    /// 业务异常（用于主动抛出业务错误）
    /// </summary>
    public class BusinessException : Exception {

        public BusinessException(string message) : base(message) {
        }
    }
}