using FluentAssertions;
using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registrations.Application.Commands;
using LYBT.Module.Registrations.Infrastructure;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Tests.Server;

/// <summary>
/// ADR-0030 跨聚合写入一致性 — StartVisitCommandHandler 失败注入测试。
/// <para>
/// 覆盖 ADR-0030 的补偿/幂等契约（真实 SQLite DbContext + 手写 fake，零 mock 框架）：
/// ① 医案侧写入失败 → 挂号侧不得半写（失败如实返回）；
/// ② 挂号侧写入失败回滚后，ChangeTracker 中的脏跟踪实体必须被 <c>CompensateInMemory</c> 还原，
///    否则同一作用域后续 SaveChanges 会把回滚掉的 InProgress 复活；
/// ③ 建案返回 null → 回滚等待态并返回失败；
/// ④ 幂等键（Registration.MedicalCaseId）阻止重试重复建案。
/// </para>
/// </summary>
public class StartVisitCrossAggregateFailureTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RegistrationDbContext _context;
    private readonly FaultInjectingRegistrationRepository _repository;
    private readonly FakeMedicalCaseCrossModuleService _medicalCase;
    private readonly FakeRegistrationNotificationService _notifications = new();

    public StartVisitCrossAggregateFailureTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new RegistrationDbContext(CreateOptions(_connection));
        _context.Database.EnsureCreated();

        _repository = new FaultInjectingRegistrationRepository(_context);
        _medicalCase = new FakeMedicalCaseCrossModuleService(_repository);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static DbContextOptions<RegistrationDbContext> CreateOptions(SqliteConnection connection)
        => new DbContextOptionsBuilder<RegistrationDbContext>().UseSqlite(connection).Options;

    private StartVisitCommandHandler CreateHandler()
        => new(_repository, _medicalCase, _notifications);

    private async Task<Registration> SeedWaitingAsync()
    {
        var registration = new Registration
        {
            PatientId = Guid.NewGuid(),
            PatientName = "测试患者",
            DoctorId = Guid.NewGuid(),
            DoctorName = "测试医生",
            Source = RegistrationSource.Receptionist,
            Status = RegistrationStatus.Waiting,
            QueueNumber = 1,
            RegistrationFee = 10m
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();
        return registration;
    }

    /// <summary>从数据库（独立 DbContext/无跟踪）读取落库真相。</summary>
    private async Task<Registration> ReloadFromDatabaseAsync(Guid id)
    {
        await using var fresh = new RegistrationDbContext(CreateOptions(_connection));
        return await fresh.Registrations.AsNoTracking().SingleAsync(r => r.Id == id);
    }

    [Fact]
    public async Task MedicalCaseWriteFails_RegistrationIsNotHalfWritten_AndFailureIsReturned()
    {
        var registration = await SeedWaitingAsync();
        _medicalCase.Failure = new BusinessException(ErrorCode.McActiveCaseExists, "该患者已有进行中的医案");

        var result = await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("跨聚合写入失败必须如实返回失败（禁止静默成功）");
        result.ErrorCode.Should().Be(ErrorCode.McActiveCaseExists);
        _medicalCase.CreateCalls.Should().Be(1);
        _notifications.StatusChangeCalls.Should().Be(0, "失败路径不得推送接诊状态变更");

        var persisted = await ReloadFromDatabaseAsync(registration.Id);
        persisted.Status.Should().Be(RegistrationStatus.Waiting, "医案写失败后挂号必须回到等待态（补偿契约）");
        persisted.MedicalCaseId.Should().BeNull("不得留下半写的医案关联");
    }

    [Fact]
    public async Task RegistrationWriteFails_CompensationPreventsChangeTrackerResurrection()
    {
        var registration = await SeedWaitingAsync();
        _medicalCase.Result = Guid.NewGuid();
        _repository.SaveFailuresRemaining = 1; // 挂号侧（关联/状态）写入失败

        var act = async () => await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // 补偿：回滚不清理 ChangeTracker——实体必须已被还原，否则后续 SaveChanges 会复活 InProgress
        var tracked = _context.Entry(registration).Entity;
        tracked.Status.Should().Be(RegistrationStatus.Waiting, "CompensateInMemory 必须还原脏跟踪实体");
        tracked.MedicalCaseId.Should().BeNull();

        // 同一作用域后续 SaveChanges：不得把回滚掉的写入复活
        await _repository.SaveChangesAsync();

        var persisted = await ReloadFromDatabaseAsync(registration.Id);
        persisted.Status.Should().Be(RegistrationStatus.Waiting, "回滚后的补偿必须阻止脏跟踪实体被再次落库");
        persisted.MedicalCaseId.Should().BeNull();
    }

    [Fact]
    public async Task MedicalCaseNotPersisted_NullResult_RevertsToWaitingAndReturnsFailure()
    {
        var registration = await SeedWaitingAsync();
        _medicalCase.Result = null; // 建案未落库

        var result = await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unknown);
        _medicalCase.CreateCalls.Should().Be(1);

        var persisted = await ReloadFromDatabaseAsync(registration.Id);
        persisted.Status.Should().Be(RegistrationStatus.Waiting);
        persisted.MedicalCaseId.Should().BeNull();
    }

    [Fact]
    public async Task IdempotencyKey_RetryAfterSuccess_DoesNotCreateDuplicateMedicalCase()
    {
        var registration = await SeedWaitingAsync();
        var medicalCaseId = Guid.NewGuid();
        _medicalCase.Result = medicalCaseId;

        var first = await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();
        first.Value.Should().Be(medicalCaseId);
        _medicalCase.CreateCalls.Should().Be(1);

        // 重试：挂号已 InProgress 且已关联医案 → 幂等短路，绝不重复建案
        var retry = await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);
        retry.IsSuccess.Should().BeTrue();
        retry.Value.Should().Be(medicalCaseId);
        _medicalCase.CreateCalls.Should().Be(1, "幂等键（Registration.MedicalCaseId）必须阻止重试重复建案");

        var persisted = await ReloadFromDatabaseAsync(registration.Id);
        persisted.Status.Should().Be(RegistrationStatus.InProgress);
        persisted.MedicalCaseId.Should().Be(medicalCaseId);
    }

    [Fact]
    public async Task RetryAfterFailedRegistrationWrite_LeavesConsistentLinkedRegistration()
    {
        var registration = await SeedWaitingAsync();
        var medicalCaseId = Guid.NewGuid();
        _medicalCase.Result = medicalCaseId;
        _repository.SaveFailuresRemaining = 1;

        var act = async () => await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // 失败窗口：挂号未被关联，仍可安全重试
        (await ReloadFromDatabaseAsync(registration.Id)).Status.Should().Be(RegistrationStatus.Waiting);

        var retry = await CreateHandler().Handle(new StartVisitCommand(registration.Id), CancellationToken.None);

        retry.IsSuccess.Should().BeTrue();
        retry.Value.Should().Be(medicalCaseId);
        var persisted = await ReloadFromDatabaseAsync(registration.Id);
        persisted.Status.Should().Be(RegistrationStatus.InProgress);
        persisted.MedicalCaseId.Should().Be(medicalCaseId);
    }

    /// <summary>
    /// 手写 IRegistrationRepository fake（Server 测试禁止 mock 框架）：委托真实仓储，
    /// 仅在需要时注入挂号侧 SaveChanges 失败。
    /// </summary>
    private sealed class FaultInjectingRegistrationRepository : IRegistrationRepository
    {
        private readonly RegistrationRepository _inner;

        public FaultInjectingRegistrationRepository(RegistrationDbContext context)
            => _inner = new RegistrationRepository(context);

        public int SaveFailuresRemaining { get; set; }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => _inner.BeginTransactionAsync(cancellationToken);

        public Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _inner.GetByIdAsync(id, cancellationToken);

        public Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
            => _inner.GetByMedicalCaseIdAsync(medicalCaseId, cancellationToken);

        public Task AddAsync(Registration registration, CancellationToken cancellationToken = default)
            => _inner.AddAsync(registration, cancellationToken);

        public Task UpdateAsync(Registration registration, CancellationToken cancellationToken = default)
            => _inner.UpdateAsync(registration, cancellationToken);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (SaveFailuresRemaining > 0)
            {
                SaveFailuresRemaining--;
                throw new InvalidOperationException("injected registration save failure");
            }

            return _inner.SaveChangesAsync(cancellationToken);
        }

        public Task<PagedResult<Registration>> GetPagedAsync(
            int page, int pageSize, string? keyword,
            DateTime? startDate, DateTime? endDate,
            Guid? patientId, Guid? doctorId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<Registration>> GetWaitingQueueAsync(
            Guid? doctorId = null, bool onlyToday = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasSameDayWaitingAsync(Guid patientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasPendingAsync(Guid patientId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 手写 IMedicalCaseCrossModuleService fake：模拟真实链路
    /// （医案在自有 DbContext 落库 → 同作用域挂号侧关联 MedicalCaseId），并支持失败/null 注入。
    /// </summary>
    private sealed class FakeMedicalCaseCrossModuleService : IMedicalCaseCrossModuleService
    {
        private readonly IRegistrationRepository _repository;

        public FakeMedicalCaseCrossModuleService(IRegistrationRepository repository)
            => _repository = repository;

        public Guid? Result { get; set; }

        public Exception? Failure { get; set; }

        public int CreateCalls { get; private set; }

        public async Task<Guid?> CreateMedicalCaseForRegistrationAsync(
            Guid patientId, Guid registrationId, Guid doctorId, CancellationToken cancellationToken = default)
        {
            CreateCalls++;

            if (Failure is not null)
                throw Failure;

            if (Result is null)
                return null;

            // 真实链路：MedicalCaseCrossModuleService → RegistrationCrossModuleService.LinkRegistrationToMedicalCaseAsync
            // （复用同一 DI 作用域的 RegistrationDbContext，参与处理器事务）
            var registration = await _repository.GetByIdAsync(registrationId, cancellationToken);
            if (registration is not null)
            {
                registration.AssignMedicalCase(Result.Value);
                await _repository.UpdateAsync(registration, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
            }

            return Result;
        }

        public Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Dictionary<Guid, int>> CountMedicalCasesBatchAsync(IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default)
            => Task.FromResult(new Dictionary<Guid, int>());

        public Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<MedicalCaseReferenceDto>());
    }

    /// <summary>手写 INotificationService fake：记录状态变更推送次数。</summary>
    private sealed class FakeRegistrationNotificationService : INotificationService
    {
        public int StatusChangeCalls { get; private set; }

        public Task NotifyNewRegistrationAsync(Guid doctorId, RegistrationDetailDto registration, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task NotifyRegistrationStatusChangedAsync(Guid doctorId, Guid registrationId, string newStatus, CancellationToken cancellationToken = default)
        {
            StatusChangeCalls++;
            return Task.CompletedTask;
        }
    }
}
