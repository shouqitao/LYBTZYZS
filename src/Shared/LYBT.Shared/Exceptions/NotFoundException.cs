namespace LYBT.Shared.Exceptions
{
    /// <summary>
    /// 资源未找到异常
    /// </summary>
    public class NotFoundException : Exception
    {
        public string ResourceType { get; }
        public object ResourceId { get; }

        public NotFoundException() : base()
        {
            ResourceType = "Resource";
            ResourceId = string.Empty;
        }

        public NotFoundException(string message) : base(message)
        {
            ResourceType = "Resource";
            ResourceId = string.Empty;
        }

        public NotFoundException(string resourceType, object resourceId) 
            : base($"{resourceType} with id '{resourceId}' was not found.")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
            ResourceType = "Resource";
            ResourceId = string.Empty;
        }
    }
}