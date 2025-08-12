using System;

namespace LYBT.Domain.Aggregates.HerbAggregate
{
    /// <summary>
    /// 药材领域异常
    /// </summary>
    public class HerbDomainException : Exception
    {
        public HerbDomainException()
        { }

        public HerbDomainException(string message)
            : base(message)
        { }

        public HerbDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}