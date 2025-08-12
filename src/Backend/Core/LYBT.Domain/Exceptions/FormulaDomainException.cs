using System;

namespace LYBT.Domain.Aggregates.FormulaAggregate
{
    /// <summary>
    /// 验方领域异常
    /// </summary>
    public class FormulaDomainException : Exception
    {
        public FormulaDomainException()
        { }

        public FormulaDomainException(string message)
            : base(message)
        { }

        public FormulaDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}