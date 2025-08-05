namespace LYBT.WebAPI.Exceptions
{
    /// <summary>
    /// 资源未找到异常
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException() : base("请求的资源不存在") { }
        
        public NotFoundException(string message) : base(message) { }
        
        public NotFoundException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}