using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Infrastructure.Repositories.DDD;
using LYBT.Domain.Aggregates.PatientAggregate;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.Aggregates.HerbAggregate;
using LYBT.Domain.Aggregates.FormulaAggregate;

namespace LYBT.Infrastructure.UnitOfWork
{
    /// <summary>
    /// 工作单元实现 - DDD模式事务管理
    /// 保证聚合根的一致性和事务完整性
    /// </summary>
    public class UnitOfWork : IUnitOfWorkWithRepositories
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction _transaction;
        private bool _disposed = false;

        // 延迟初始化的Repository实例
        private IPatientRepository _patientRepository;
        private IConsultationRepository _consultationRepository;
        private IMedicalCaseRepository _medicalCaseRepository;
        private IHerbRepository _herbRepository;
        private IFormulaRepository _formulaRepository;

        public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Repository Properties (延迟初始化)

        /// <summary>
        /// 患者Repository
        /// </summary>
        public IPatientRepository Patients =>
            _patientRepository ??= new PatientRepository(_context, 
                _logger.AsLogger<PatientRepository>());

        /// <summary>
        /// 看诊Repository
        /// </summary>
        public IConsultationRepository Consultations =>
            _consultationRepository ??= new ConsultationRepository(_context, 
                _logger.AsLogger<ConsultationRepository>());

        /// <summary>
        /// 病案Repository
        /// </summary>
        public IMedicalCaseRepository MedicalCases =>
            _medicalCaseRepository ??= new MedicalCaseRepository(_context, 
                _logger.AsLogger<MedicalCaseRepository>());

        /// <summary>
        /// 中药材Repository
        /// </summary>
        public IHerbRepository Herbs =>
            _herbRepository ??= new HerbRepository(_context, 
                _logger.AsLogger<HerbRepository>());

        /// <summary>
        /// 验方Repository
        /// </summary>
        public IFormulaRepository Formulas =>
            _formulaRepository ??= new FormulaRepository(_context, 
                _logger.AsLogger<FormulaRepository>());

        #endregion

        #region Transaction Management

        /// <summary>
        /// 保存所有更改
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                _logger.LogDebug("Saving changes to database");

                // 在保存前处理领域事件（如果有的话）
                await ProcessDomainEventsAsync();

                // 保存更改
                var result = await _context.SaveChangesAsync();
                
                _logger.LogDebug("Successfully saved {ChangeCount} changes to database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving changes to database");
                throw;
            }
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogWarning("Transaction already exists, cannot begin new transaction");
                return;
            }

            _logger.LogDebug("Beginning database transaction");
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to commit");
                return;
            }

            try
            {
                _logger.LogDebug("Committing database transaction");
                
                // 先保存更改
                await SaveChangesAsync();
                
                // 然后提交事务
                await _transaction.CommitAsync();
                
                _logger.LogDebug("Transaction committed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while committing transaction");
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to rollback");
                return;
            }

            try
            {
                _logger.LogDebug("Rolling back database transaction");
                await _transaction.RollbackAsync();
                _logger.LogDebug("Transaction rolled back successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rolling back transaction");
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// 检查是否有活跃事务
        /// </summary>
        public bool HasActiveTransaction => _transaction != null;

        #endregion

        #region Domain Events Processing

        /// <summary>
        /// 处理领域事件
        /// </summary>
        private async Task ProcessDomainEventsAsync()
        {
            try
            {
                // 获取所有有领域事件的聚合根
                var domainEntities = _context.ChangeTracker
                    .Entries<IAggregateRoot>()
                    .Where(x => x.Entity.DomainEvents?.Any() == true)
                    .ToList();

                // 提取所有领域事件
                var domainEvents = domainEntities
                    .SelectMany(x => x.Entity.DomainEvents)
                    .ToList();

                // 清空领域事件
                domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

                // 发布领域事件（这里可以集成MediatR或其他事件总线）
                foreach (var domainEvent in domainEvents)
                {
                    _logger.LogDebug("Processing domain event: {EventType}", domainEvent.GetType().Name);
                    
                    // TODO: 在这里添加领域事件发布逻辑
                    // 例如：await _mediator.Publish(domainEvent);
                    
                    await Task.CompletedTask; // 占位符，实际应该发布事件
                }

                _logger.LogDebug("Processed {EventCount} domain events", domainEvents.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing domain events");
                throw;
            }
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// 批量保存多个聚合根的更改
        /// </summary>
        public async Task<int> SaveChangesWithTransactionAsync()
        {
            if (HasActiveTransaction)
            {
                // 如果已有事务，直接保存
                return await SaveChangesAsync();
            }

            // 否则创建新事务
            await BeginTransactionAsync();
            try
            {
                var result = await SaveChangesAsync();
                await CommitTransactionAsync();
                return result;
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// 执行事务操作
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var transactionCreated = false;
            
            if (!HasActiveTransaction)
            {
                await BeginTransactionAsync();
                transactionCreated = true;
            }

            try
            {
                var result = await operation();
                
                if (transactionCreated)
                {
                    await CommitTransactionAsync();
                }
                
                return result;
            }
            catch
            {
                if (transactionCreated)
                {
                    await RollbackTransactionAsync();
                }
                throw;
            }
        }

        /// <summary>
        /// 执行事务操作（无返回值）
        /// </summary>
        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            await ExecuteInTransactionAsync(async () =>
            {
                await operation();
                return Task.CompletedTask;
            });
        }

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                        _transaction = null;
                    }
                    
                    _context?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while disposing UnitOfWork");
                }
                
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }

        #endregion
    }
}

/// <summary>
/// 扩展UnitOfWork的便利方法
/// </summary>
public static class UnitOfWorkExtensions
{
    /// <summary>
    /// 将通用ILogger转换为特定类型的ILogger
    /// </summary>
    public static ILogger<T> AsLogger<T>(this ILogger logger)
    {
        return new LoggerAdapter<T>(logger);
    }

    /// <summary>
    /// ILogger适配器，用于类型转换
    /// </summary>
    private class LoggerAdapter<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public LoggerAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}