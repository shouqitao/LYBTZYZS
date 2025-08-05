namespace LYBT.WebAPI.Exceptions
{
    /// <summary>
    /// 业务异常
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException() { }
        
        public BusinessException(string message) : base(message) { }
        
        public BusinessException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}