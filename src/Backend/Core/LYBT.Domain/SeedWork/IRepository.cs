using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LYBT.Domain.Common;

namespace LYBT.Domain.SeedWork
{
    /// <summary>
    /// 领域层Repository接口 - DDD聚合根持久化抽象
    /// </summary>
    /// <typeparam name="TAggregateRoot">聚合根类型</typeparam>
    public interface IRepository<TAggregateRoot> where TAggregateRoot : AggregateRoot
    {
        #region Query Operations
        
        /// <summary>
        /// 根据ID获取聚合根
        /// </summary>
        Task<TAggregateRoot> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 根据ID列表获取聚合根
        /// </summary>
        Task<List<TAggregateRoot>> GetByIdsAsync(List<Guid> ids);
        
        /// <summary>
        /// 获取所有聚合根
        /// </summary>
        Task<List<TAggregateRoot>> GetAllAsync();
        
        /// <summary>
        /// 根据条件查找聚合根
        /// </summary>
        Task<List<TAggregateRoot>> FindAsync(Expression<Func<TAggregateRoot, bool>> predicate);
        
        /// <summary>
        /// 获取第一个匹配的聚合根
        /// </summary>
        Task<TAggregateRoot> FirstOrDefaultAsync(Expression<Func<TAggregateRoot, bool>> predicate);
        
        /// <summary>
        /// 检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> predicate);
        
        /// <summary>
        /// 获取数量
        /// </summary>
        Task<int> CountAsync(Expression<Func<TAggregateRoot, bool>> predicate = null);
        
        #endregion

        #region Command Operations
        
        /// <summary>
        /// 添加聚合根
        /// </summary>
        Task<TAggregateRoot> AddAsync(TAggregateRoot aggregateRoot);
        
        /// <summary>
        /// 更新聚合根
        /// </summary>
        Task<TAggregateRoot> UpdateAsync(TAggregateRoot aggregateRoot);
        
        /// <summary>
        /// 删除聚合根
        /// </summary>
        Task DeleteAsync(TAggregateRoot aggregateRoot);
        
        /// <summary>
        /// 根据ID删除聚合根
        /// </summary>
        Task DeleteAsync(Guid id);
        
        #endregion
    }

    /// <summary>
    /// 患者Repository扩展接口
    /// </summary>
    public interface IPatientRepository : IRepository<LYBT.Domain.Aggregates.PatientAggregate.Patient>
    {
        Task<List<LYBT.Domain.Aggregates.PatientAggregate.Patient>> GetByPhoneNumberAsync(string phoneNumber);
        Task<LYBT.Domain.Aggregates.PatientAggregate.Patient> GetByIdNumberAsync(string idNumber);
    }

    /// <summary>
    /// 看诊Repository扩展接口
    /// </summary>
    public interface IConsultationRepository : IRepository<LYBT.Domain.Aggregates.ConsultationAggregate.Consultation>
    {
        Task<List<LYBT.Domain.Aggregates.ConsultationAggregate.Consultation>> GetByPatientIdAsync(Guid patientId);
        Task<List<LYBT.Domain.Aggregates.ConsultationAggregate.Consultation>> GetByDoctorIdAsync(Guid doctorId);
        Task<List<LYBT.Domain.Aggregates.ConsultationAggregate.Consultation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// 病案Repository扩展接口
    /// </summary>
    public interface IMedicalCaseRepository : IRepository<LYBT.Domain.Aggregates.MedicalCaseAggregate.MedicalCase>
    {
        Task<List<LYBT.Domain.Aggregates.MedicalCaseAggregate.MedicalCase>> GetByPatientIdAsync(Guid patientId);
        Task<List<LYBT.Domain.Aggregates.MedicalCaseAggregate.MedicalCase>> GetActiveByPatientIdAsync(Guid patientId);
        Task<List<LYBT.Domain.Aggregates.MedicalCaseAggregate.MedicalCase>> GetByDoctorIdAsync(Guid doctorId);
        Task<List<LYBT.Domain.Aggregates.MedicalCaseAggregate.MedicalCase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// 中药材Repository扩展接口
    /// </summary>
    public interface IHerbRepository : IRepository<LYBT.Domain.Aggregates.HerbAggregate.Herb>
    {
        Task<List<LYBT.Domain.Aggregates.HerbAggregate.Herb>> GetByNameAsync(string name);
        Task<List<LYBT.Domain.Aggregates.HerbAggregate.Herb>> GetByCategoryAsync(LYBT.Domain.Aggregates.HerbAggregate.HerbCategory category);
        Task<List<LYBT.Domain.Aggregates.HerbAggregate.Herb>> GetActiveHerbsAsync();
    }

    /// <summary>
    /// 验方Repository扩展接口
    /// </summary>
    public interface IFormulaRepository : IRepository<LYBT.Domain.Aggregates.FormulaAggregate.Formula>
    {
        Task<List<LYBT.Domain.Aggregates.FormulaAggregate.Formula>> GetByCreatorIdAsync(Guid creatorId);
        Task<List<LYBT.Domain.Aggregates.FormulaAggregate.Formula>> GetPublicFormulasAsync();
        Task<List<LYBT.Domain.Aggregates.FormulaAggregate.Formula>> GetByTargetSyndromeAsync(LYBT.Domain.ValueObjects.TCMSyndrome syndrome);
        Task<List<LYBT.Domain.Aggregates.FormulaAggregate.Formula>> GetApprovedFormulasAsync();
    }
}