using System;

namespace LYBT.Infrastructure.Exceptions {
    /// <summary>
    /// 业务异常，表示可预期的错误
    /// </summary>
    public class BusinessException : Exception {
        public int Code { get; }

        public BusinessException(string message, int code = 400) : base(message) {
            Code = code;
        }
    }
}
