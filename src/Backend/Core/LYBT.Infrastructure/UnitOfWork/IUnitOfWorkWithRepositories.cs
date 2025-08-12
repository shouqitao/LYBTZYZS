using System;
using System.Threading.Tasks;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Aggregates.PatientAggregate;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.Aggregates.HerbAggregate;
using LYBT.Domain.Aggregates.FormulaAggregate;

namespace LYBT.Infrastructure.UnitOfWork
{
    /// <summary>
    /// 包含Repository访问的工作单元接口 - Infrastructure层扩展
    /// 继承Domain层的IUnitOfWork接口并添加Repository访问
    /// </summary>
    public interface IUnitOfWorkWithRepositories : IUnitOfWork
    {
        #region Repository Access Properties

        /// <summary>
        /// 患者聚合根Repository
        /// </summary>
        IPatientRepository Patients { get; }

        /// <summary>
        /// 看诊聚合根Repository
        /// </summary>
        IConsultationRepository Consultations { get; }

        /// <summary>
        /// 病案聚合根Repository
        /// </summary>
        IMedicalCaseRepository MedicalCases { get; }

        /// <summary>
        /// 中药材聚合根Repository
        /// </summary>
        IHerbRepository Herbs { get; }

        /// <summary>
        /// 验方聚合根Repository
        /// </summary>
        IFormulaRepository Formulas { get; }

        #endregion

        #region Enhanced Transaction Operations

        /// <summary>
        /// 在事务中保存更改
        /// </summary>
        Task<int> SaveChangesWithTransactionAsync();

        /// <summary>
        /// 在事务中执行操作（有返回值）
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// 在事务中执行操作（无返回值）
        /// </summary>
        Task ExecuteInTransactionAsync(Func<Task> operation);

        #endregion
    }
}