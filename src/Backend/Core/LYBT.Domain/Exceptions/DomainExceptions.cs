using System;

namespace LYBT.Domain.Exceptions
{
    /// <summary>
    /// 领域异常基类
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
        protected DomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 患者领域异常
    /// </summary>
    public class PatientDomainException : DomainException
    {
        public PatientDomainException(string message) : base(message) { }
        public PatientDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 处方领域异常
    /// </summary>
    public class PrescriptionDomainException : DomainException
    {
        public PrescriptionDomainException(string message) : base(message) { }
        public PrescriptionDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 看诊领域异常
    /// </summary>
    public class ConsultationDomainException : DomainException
    {
        public ConsultationDomainException(string message) : base(message) { }
        public ConsultationDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 中药材领域异常
    /// </summary>
    public class HerbDomainException : DomainException
    {
        public HerbDomainException(string message) : base(message) { }
        public HerbDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 验方领域异常
    /// </summary>
    public class FormulaDomainException : DomainException
    {
        public FormulaDomainException(string message) : base(message) { }
        public FormulaDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 病案领域异常
    /// </summary>
    public class MedicalCaseDomainException : DomainException
    {
        public MedicalCaseDomainException(string message) : base(message) { }
        public MedicalCaseDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 用户领域异常
    /// </summary>
    public class UserDomainException : DomainException
    {
        public UserDomainException(string message) : base(message) { }
        public UserDomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// 业务规则验证异常
    /// </summary>
    public class BusinessRuleValidationException : DomainException
    {
        public string Rule { get; }
        public string Details { get; }

        public BusinessRuleValidationException(string rule, string details) 
            : base($"业务规则验证失败: {rule}")
        {
            Rule = rule;
            Details = details;
        }
    }

    /// <summary>
    /// 并发冲突异常
    /// </summary>
    public class ConcurrencyException : DomainException
    {
        public ConcurrencyException(string message) : base(message) { }
        public ConcurrencyException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}