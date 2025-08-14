namespace LYBT.WebAPI.Exceptions
{
    /// <summary>
    /// 验证异常
    /// </summary>
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]>? Errors { get; set; }

        public ValidationException() : base("验证失败") { }

        public ValidationException(string message) : base(message) { }

        public ValidationException(string message, Dictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }

        public ValidationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}