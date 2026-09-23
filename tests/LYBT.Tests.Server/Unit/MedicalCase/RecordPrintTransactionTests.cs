using FluentAssertions;
using FluentValidation;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.MedicalCases.Guards;
using LYBT.Module.MedicalCases.Infrastructure;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Server;

/// <summary>
/// ADR-0030 同 DbContext 多聚合事务 — 打印记录（<c>RecordPrintAsync</c>）失败注入测试。
/// <para>
/// 打印路径在同一 <c>MedicalCaseDbContext</c>（MedicalCases + MedicalCasePrintLogs）内用显式事务包住两步写：
/// 医案打印状态回写 + 打印日志插入。本测试用真实仓储 + 真实 SQLite DbContext，
/// 经 <see cref="SaveChangesInterceptor"/> 在打印日志落库前注入失败，验证：
/// ① 成功 → 两写一并提交；
/// ② 日志写失败 → 显式事务回滚 → 不留下「打印计数已 +1 却无日志」的半写状态。
/// </para>
/// </summary>
public class RecordPrintTransactionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FailOnPrintLogSaveInterceptor _interceptor = new();
    private readonly MedicalCaseDbContext _context;
    private readonly MedicalCaseCommandService _service;

    public RecordPrintTransactionTests()
    {
        // Foreign Keys=False：本测试只验证事务语义，不构造 Patient/User 聚合（避免无关前置数据）
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();

        _context = new MedicalCaseDbContext(
            new DbContextOptionsBuilder<MedicalCaseDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(_interceptor)
                .Options);
        _context.Database.EnsureCreated();

        var repository = new MedicalCaseRepository(_context, NullLogger<MedicalCaseRepository>.Instance);
        var stubs = new MedicalCaseServiceStubs();
        var itemService = new PrescriptionItemService(repository, stubs, NullLogger<PrescriptionItemService>.Instance);
        var prescriptionService = new MedicalCasePrescriptionService(
            repository, stubs, NullLogger<MedicalCasePrescriptionService>.Instance, itemService);

        _service = new MedicalCaseCommandService(
            repository,
            stubs,
            stubs,
            stubs,
            NullLogger<MedicalCaseCommandService>.Instance,
            stubs,
            prescriptionService,
            itemService,
            new MedicalCaseMapper(),
            new InlineValidator<MedicalCaseInputDto>(),
            new InlineValidator<ConsultationInputDto>(),
            stubs,
            new MedicalCaseStateGuard(stubs),
            stubs);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private MedicalCaseDbContext CreateFreshContext()
        => new(new DbContextOptionsBuilder<MedicalCaseDbContext>().UseSqlite(_connection).Options);

    private MedicalCase SeedCase()
    {
        var doctorId = Guid.NewGuid();
        var entity = new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "张三",
            UserId = doctorId,
            DoctorName = "李医生",
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = true,
            IsDeleted = false,
            CreatedBy = doctorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MedicalCases.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    [Fact]
    public async Task RecordPrintAsync_Success_PersistsCaseAndPrintLogTogether()
    {
        var entity = SeedCase();

        var result = await _service.RecordPrintAsync(
            entity.Id, printType: 0, printerName: "HP-1", Guid.NewGuid(), "李医生");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        await using var fresh = CreateFreshContext();
        var persisted = await fresh.MedicalCases.AsNoTracking().SingleAsync(m => m.Id == entity.Id);
        persisted.IsPrinted.Should().BeTrue();
        persisted.PrintCount.Should().Be(1);
        persisted.PrintVersion.Should().Be(1);
        persisted.LastPrintedAt.Should().NotBeNull();

        var logs = await fresh.MedicalCasePrintLogs.AsNoTracking()
            .Where(l => l.MedicalCaseId == entity.Id).ToListAsync();
        logs.Should().ContainSingle();
        logs[0].PrintVersion.Should().Be(1);
        logs[0].PrintedBy.Should().Be("李医生");
        logs[0].PrinterName.Should().Be("HP-1");
    }

    [Fact]
    public async Task RecordPrintAsync_PrintLogWriteFails_RollsBackCaseUpdate_NoPartialWrite()
    {
        var entity = SeedCase();
        _interceptor.Enabled = true;

        var act = async () => await _service.RecordPrintAsync(
            entity.Id, printType: 0, printerName: "HP-1", Guid.NewGuid(), "李医生");
        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var fresh = CreateFreshContext();
        var persisted = await fresh.MedicalCases.AsNoTracking().SingleAsync(m => m.Id == entity.Id);
        persisted.IsPrinted.Should().BeFalse("日志写失败必须整体回滚——不允许「计数已 +1 却无日志」的半写");
        persisted.PrintCount.Should().Be(0);
        persisted.PrintVersion.Should().Be(0);
        persisted.LastPrintedAt.Should().BeNull();

        (await fresh.MedicalCasePrintLogs.AsNoTracking()
            .CountAsync(l => l.MedicalCaseId == entity.Id)).Should().Be(0);
    }

    /// <summary>
    /// 在打印日志落库前抛错，模拟「第二步写失败」——命中 <c>RecordPrintAsync</c> 的 catch → Rollback。
    /// </summary>
    private sealed class FailOnPrintLogSaveInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled
                && eventData.Context is not null
                && eventData.Context.ChangeTracker.Entries<MedicalCasePrintLog>().Any(e => e.State == EntityState.Added))
            {
                throw new InvalidOperationException("injected print-log write failure");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>
    /// 手写多接口 stub（Server 测试禁止 mock 框架）：<c>RecordPrintAsync</c> 只依赖仓储，
    /// 其余构造依赖仅为满足 <see cref="MedicalCaseCommandService"/> 图，行为不被触及。
    /// </summary>
    private sealed class MedicalCaseServiceStubs :
        IRegistrationCrossModuleService,
        IPatientCrossModuleService,
        IUserCrossModuleService,
        ICacheInvalidationService,
        ICatalogCrossModuleService,
        IMedicalCaseTimeService,
        IDomainEventDispatcher
    {
        // IRegistrationCrossModuleService
        public Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default) => Task.CompletedTask;

        public Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default) => Task.CompletedTask;

        public Task LinkRegistrationToMedicalCaseAsync(Guid registrationId, Guid medicalCaseId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> HasWaitingRegistrationsAsync(Guid doctorId, CancellationToken ct = default) => Task.FromResult(false);

        // IPatientCrossModuleService
        public Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default)
            => Task.FromResult<PatientBasicDto?>(null);

        // IUserCrossModuleService
        public Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
            int page, int pageSize, string? keyword, UserRole? role = null, CommonStatus? status = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct) => throw new NotSupportedException();

        public Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<UserBasicDto?>(null);

        public Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult<UserCredentialDto?>(null);

        public Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        // ICacheInvalidationService
        public Task InvalidateAsync(string tag, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default) => Task.CompletedTask;

        // ICatalogCrossModuleService
        public Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
            => Task.FromResult(new HashSet<Guid>());

        public Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
            => Task.FromResult(new Dictionary<Guid, decimal>());

        public Task<HashSet<Guid>> GetExistingHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
            => Task.FromResult(new HashSet<Guid>());

        // IMedicalCaseTimeService
        public bool IsLocked(MedicalCase medicalCase, DateTimeOffset? now = null) => false;

        // IDomainEventDispatcher
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default) => Task.CompletedTask;
    }
}
