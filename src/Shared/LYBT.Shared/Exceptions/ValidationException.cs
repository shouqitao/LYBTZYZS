namespace LYBT.Shared.Exceptions
{
    /// <summary>
    /// 验证异常
    /// </summary>
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]>? Errors { get; }

        public ValidationException() : base("One or more validation errors occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(string message) : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
        {
            Errors = errors;
        }

        public ValidationException(IDictionary<string, string[]> errors) 
            : base("One or more validation errors occurred.")
        {
            Errors = errors;
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
            Errors = new Dictionary<string, string[]>();
        }
    }
}