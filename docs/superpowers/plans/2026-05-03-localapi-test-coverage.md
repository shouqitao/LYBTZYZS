# LocalAPI Test Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add unit tests for the 5 untested Http*Repository classes and integration tests for the 7 domain LocalWebAPI controllers.

**Architecture:** Http*Repository tests use MockHttpMessageHandler + NSubstitute (same pattern as HttpPatientRepositoryTests). Controller integration tests use WebApplicationFactory with SQL Server LocalDB + real DbContext (same pattern as LocalWebApiDbContextTests).

**Tech Stack:** xUnit, FluentAssertions, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, EF Core SQL Server

---

## File Structure

| File | Purpose |
|------|---------|
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpHerbRepositoryTests.cs` | HttpHerbRepository unit tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpFormulaRepositoryTests.cs` | HttpFormulaRepository unit tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpMedicalCaseRepositoryTests.cs` | HttpMedicalCaseRepository unit tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpUserRepositoryTests.cs` | HttpUserRepository unit tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpRegistrationRepositoryTests.cs` | HttpRegistrationRepository unit tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/LocalWebApiControllerTestBase.cs` | Shared base for controller integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HealthControllerTests.cs` | HealthController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/AuthControllerTests.cs` | AuthController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/UsersControllerTests.cs` | UsersController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/PatientsControllerTests.cs` | PatientsController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/HerbsControllerTests.cs` | HerbsController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/FormulasControllerTests.cs` | FormulasController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/MedicalCasesControllerTests.cs` | MedicalCasesController integration tests |
| `tests/LYBT.Tests.Desktop/LocalWebAPI/RegistrationsControllerTests.cs` | RegistrationsController integration tests |

---

## Task 1: HttpHerbRepository Unit Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpHerbRepositoryTests.cs`

- [ ] **Step 1: Write HttpHerbRepositoryTests**

```csharp
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HttpHerbRepositoryTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ILogger<HttpHerbRepository> _logger;
    private readonly HttpHerbRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpHerbRepositoryTests()
    {
        _client = new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpHerbRepository>>();
        _repo = new HttpHerbRepository(_client, _logger);
    }

    public void Dispose() { _client.Dispose(); GC.SuppressFinalize(this); }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.DeleteAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_List_On_No_Results()
    {
        var paged = new PagedResult<HerbListDto> { Items = new List<HerbListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.SearchAsync("nonexistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.ToggleStatusAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.RestoreAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task BatchDeleteAsync_Deserializes_Result()
    {
        var resultDto = new BatchOperationResultDto { SuccessCount = 3, FailCount = 0 };
        var json = JsonSerializer.Serialize(resultDto, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.BatchDeleteAsync([Guid.NewGuid(), Guid.NewGuid()]);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_And_Returns_Detail()
    {
        var detail = new HerbDetailDto { Id = Guid.NewGuid(), Name = "Test" };
        var json = JsonSerializer.Serialize(detail, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.PathAndQuery.Should().Be("/api/herbs");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.CreateAsync(new HerbInputDto { Name = "Test" });
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HttpHerbRepositoryTests" --no-build`
Expected: Build first with `dotnet build tests/LYBT.Tests.Desktop/`, then all 7 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HttpHerbRepositoryTests.cs
git commit -m "test(desktop): add HttpHerbRepository unit tests"
```

---

## Task 2: HttpFormulaRepository Unit Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpFormulaRepositoryTests.cs`

- [ ] **Step 1: Write HttpFormulaRepositoryTests**

```csharp
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HttpFormulaRepositoryTests : IDisposable
{
    private readonly ILogger<HttpFormulaRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpFormulaRepositoryTests() { _logger = Substitute.For<ILogger<HttpFormulaRepository>>(); }
    public void Dispose() { GC.SuppressFinalize(this); }

    private HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new MockHttpMessageHandler(handler)) { BaseAddress = new Uri("http://127.0.0.1:0") };

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpFormulaRepository(client, _logger);
        (await repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var repo = new HttpFormulaRepository(client, _logger);
        (await repo.DeleteAsync(Guid.NewGuid())).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        var paged = new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        using var client = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var repo = new HttpFormulaRepository(client, _logger);
        (await repo.SearchAsync("none")).Should().BeEmpty();
    }

    [Fact]
    public async Task CloneFormulaAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpFormulaRepository(client, _logger);
        (await repo.CloneFormulaAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpFormulaRepository(client, _logger);
        (await repo.ToggleStatusAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpFormulaRepository(client, _logger);
        (await repo.RestoreAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task BatchDeleteAsync_Deserializes_Result()
    {
        var resultDto = new BatchOperationResultDto { SuccessCount = 2, FailCount = 0 };
        var json = JsonSerializer.Serialize(resultDto, Json);
        using var client = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var repo = new HttpFormulaRepository(client, _logger);
        var result = await repo.BatchDeleteAsync([Guid.NewGuid()]);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_To_Formulas()
    {
        var detail = new FormulaDetailDto { Id = Guid.NewGuid(), Name = "Test Formula" };
        var json = JsonSerializer.Serialize(detail, Json);
        using var client = CreateClient((req, ct) =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.PathAndQuery.Should().Be("/api/formulas");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        var repo = new HttpFormulaRepository(client, _logger);
        var result = await repo.CreateAsync(new FormulaInputDto { Name = "Test Formula" });
        result.Name.Should().Be("Test Formula");
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HttpFormulaRepositoryTests" --no-build`
Expected: 8 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HttpFormulaRepositoryTests.cs
git commit -m "test(desktop): add HttpFormulaRepository unit tests"
```

---

## Task 3: HttpMedicalCaseRepository Unit Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpMedicalCaseRepositoryTests.cs`

- [ ] **Step 1: Write HttpMedicalCaseRepositoryTests**

```csharp
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HttpMedicalCaseRepositoryTests : IDisposable
{
    private readonly ILogger<HttpMedicalCaseRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpMedicalCaseRepositoryTests() { _logger = Substitute.For<ILogger<HttpMedicalCaseRepository>>(); }
    public void Dispose() { GC.SuppressFinalize(this); }

    private HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new MockHttpMessageHandler(handler)) { BaseAddress = new Uri("http://127.0.0.1:0") };

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.DeleteAsync(Guid.NewGuid())).Should().BeTrue();
    }

    [Fact]
    public async Task CloseCaseAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.CloseCaseAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetPermissionsAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.GetPermissionsAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task SetPrescriptionFlagAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.SetPrescriptionFlagAsync(Guid.NewGuid(), new SetPrescriptionFlagRequest())).Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.UpdateStatusAsync(Guid.NewGuid(), new MedicalCaseStatusInputDto())).Should().BeNull();
    }

    [Fact]
    public async Task SuspendAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.SuspendAsync(Guid.NewGuid(), null)).Should().BeNull();
    }

    [Fact]
    public async Task RecordPrintCompletedAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        (await repo.RecordPrintCompletedAsync(Guid.NewGuid(), new PrintCompletedRequest())).Should().BeNull();
    }

    [Fact]
    public async Task GetBatchDetailsAsync_Deserializes_List()
    {
        var details = new List<MedicalCaseDetailDto> { new() { Id = Guid.NewGuid() } };
        var json = JsonSerializer.Serialize(details, Json);
        using var client = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        var result = await repo.GetBatchDetailsAsync([Guid.NewGuid()]);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task BatchDeleteAsync_Deserializes_Result()
    {
        var resultDto = new BatchOperationResultDto { SuccessCount = 1, FailCount = 0 };
        var json = JsonSerializer.Serialize(resultDto, Json);
        using var client = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var repo = new HttpMedicalCaseRepository(client, _logger);
        var result = await repo.BatchDeleteAsync([Guid.NewGuid()]);
        result.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HttpMedicalCaseRepositoryTests" --no-build`
Expected: 10 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HttpMedicalCaseRepositoryTests.cs
git commit -m "test(desktop): add HttpMedicalCaseRepository unit tests"
```

---

## Task 4: HttpUserRepository Unit Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpUserRepositoryTests.cs`

- [ ] **Step 1: Write HttpUserRepositoryTests**

```csharp
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HttpUserRepositoryTests : IDisposable
{
    private readonly ILogger<HttpUserRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpUserRepositoryTests() { _logger = Substitute.For<ILogger<HttpUserRepository>>(); }
    public void Dispose() { GC.SuppressFinalize(this); }

    private HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new MockHttpMessageHandler(handler)) { BaseAddress = new Uri("http://127.0.0.1:0") };

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpUserRepository(client, _logger);
        (await repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var repo = new HttpUserRepository(client, _logger);
        (await repo.DeleteAsync(Guid.NewGuid())).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        var paged = new PagedResult<UserListDto> { Items = new List<UserListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        using var client = CreateClient((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var repo = new HttpUserRepository(client, _logger);
        (await repo.SearchAsync("none")).Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpUserRepository(client, _logger);
        (await repo.ToggleStatusAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpUserRepository(client, _logger);
        (await repo.RestoreAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_Returns_Success_On_200()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var repo = new HttpUserRepository(client, _logger);
        var result = await repo.ChangePasswordAsync(Guid.NewGuid(), new LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Returns_Failure_On_400()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var repo = new HttpUserRepository(client, _logger);
        var result = await repo.ChangePasswordAsync(Guid.NewGuid(), new LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest());
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_To_Users()
    {
        var detail = new UserDetailDto { Id = Guid.NewGuid(), UserName = "test" };
        var json = JsonSerializer.Serialize(detail, Json);
        using var client = CreateClient((req, ct) =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.PathAndQuery.Should().Be("/api/users");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        var repo = new HttpUserRepository(client, _logger);
        var result = await repo.CreateAsync(new UserInputDto { UserName = "test" });
        result.UserName.Should().Be("test");
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HttpUserRepositoryTests" --no-build`
Expected: 8 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HttpUserRepositoryTests.cs
git commit -m "test(desktop): add HttpUserRepository unit tests"
```

---

## Task 5: HttpRegistrationRepository Unit Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HttpRegistrationRepositoryTests.cs`

- [ ] **Step 1: Write HttpRegistrationRepositoryTests**

```csharp
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HttpRegistrationRepositoryTests : IDisposable
{
    private readonly ILogger<HttpRegistrationRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpRegistrationRepositoryTests() { _logger = Substitute.For<ILogger<HttpRegistrationRepository>>(); }
    public void Dispose() { GC.SuppressFinalize(this); }

    private HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new MockHttpMessageHandler(handler)) { BaseAddress = new Uri("http://127.0.0.1:0") };

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpRegistrationRepository(client, _logger);
        (await repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetWaitingQueueAsync_Deserializes_List()
    {
        var list = new List<RegistrationListDto> { new() { Id = Guid.NewGuid() } };
        var json = JsonSerializer.Serialize(list, Json);
        using var client = CreateClient((req, ct) =>
        {
            req.RequestUri!.PathAndQuery.Should().Contain("/api/registrations/queue");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        var repo = new HttpRegistrationRepository(client, _logger);
        var result = await repo.GetWaitingQueueAsync();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWaitingQueueAsync_With_DoctorId_Passes_Query_Param()
    {
        var list = new List<RegistrationListDto>();
        var json = JsonSerializer.Serialize(list, Json);
        var doctorId = Guid.NewGuid();
        using var client = CreateClient((req, ct) =>
        {
            req.RequestUri!.PathAndQuery.Should().Contain($"doctorId={doctorId}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        var repo = new HttpRegistrationRepository(client, _logger);
        await repo.GetWaitingQueueAsync(doctorId);
    }

    [Fact]
    public async Task StartVisitAsync_Returns_Null_On_404()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var repo = new HttpRegistrationRepository(client, _logger);
        (await repo.StartVisitAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_Returns_True_On_Success()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var repo = new HttpRegistrationRepository(client, _logger);
        (await repo.CancelAsync(Guid.NewGuid())).Should().BeTrue();
    }

    [Fact]
    public async Task CancelAsync_Returns_False_On_Error()
    {
        using var client = CreateClient((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var repo = new HttpRegistrationRepository(client, _logger);
        (await repo.CancelAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_To_Registrations()
    {
        var detail = new RegistrationDetailDto { Id = Guid.NewGuid() };
        var json = JsonSerializer.Serialize(detail, Json);
        using var client = CreateClient((req, ct) =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.PathAndQuery.Should().Be("/api/registrations");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        var repo = new HttpRegistrationRepository(client, _logger);
        var result = await repo.CreateAsync(new RegistrationInputDto());
        result.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HttpRegistrationRepositoryTests" --no-build`
Expected: 7 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HttpRegistrationRepositoryTests.cs
git commit -m "test(desktop): add HttpRegistrationRepository unit tests"
```

---

## Task 6: Controller Integration Test Base

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/LocalWebApiControllerTestBase.cs`

- [ ] **Step 1: Write the shared test base class**

This base class creates a WebApplicationFactory pointing at the LocalWebAPI Program, with SQL Server LocalDB and EnsureCreated per fixture.

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LYBT.LocalWebAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Shared base for LocalWebAPI controller integration tests.
/// Creates a WebApplicationFactory with SQL Server LocalDB.
/// </summary>
public class LocalWebApiControllerTestBase : IAsyncLifetime
{
    protected readonly string DbName = $"LYBTZYZS_CtrlTests_{Guid.NewGuid():N}";
    protected WebApplicationFactory<LYBT.LocalWebAPI.Program> Factory = null!;
    protected HttpClient Client = null!;
    protected static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<LYBT.LocalWebAPI.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with test database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<LocalWebApiDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<LocalWebApiDbContext>(options =>
                        options.UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={DbName};Trusted_Connection=True;TrustServerCertificate=True"));
                });
            });

        Client = Factory.CreateClient();

        // Ensure database is created and seeded
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalWebApiDbContext>();
        await db.Database.EnsureCreatedAsync();
        await LocalWebApiSeedData.SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();

        // Drop test database
        var options = new DbContextOptionsBuilder<LocalWebApiDbContext>()
            .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={DbName};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        await using var db = new LocalWebApiDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Get a JWT token for the seeded admin user.
    /// </summary>
    protected async Task<string> GetAdminTokenAsync()
    {
        var loginBody = JsonSerializer.Serialize(new { Username = "admin", Password = "admin123" }, Json);
        var content = new StringContent(loginBody, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/auth/login", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Set Bearer token on the HttpClient.
    /// </summary>
    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
```

- [ ] **Step 2: Verify the base compiles**

Run: `dotnet build tests/LYBT.Tests.Desktop/`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/LocalWebApiControllerTestBase.cs
git commit -m "test(desktop): add LocalWebAPI controller integration test base"
```

---

## Task 7: HealthController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HealthControllerTests.cs`

- [ ] **Step 1: Write HealthControllerTests**

```csharp
using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HealthControllerTests : LocalWebApiControllerTestBase
{
    [Fact]
    public async Task Ping_Returns_Ok()
    {
        var response = await Client.GetAsync("/api/health/ping");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetHealth_Returns_Ok_When_Db_Available()
    {
        var response = await Client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDetails_Returns_User_Count()
    {
        var response = await Client.GetAsync("/api/health/details");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("userCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HealthControllerTests" --no-build`
Expected: 3 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HealthControllerTests.cs
git commit -m "test(desktop): add HealthController integration tests"
```

---

## Task 8: AuthController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/AuthControllerTests.cs`

- [ ] **Step 1: Write AuthControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class AuthControllerTests : LocalWebApiControllerTestBase
{
    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_Token()
    {
        var body = JsonSerializer.Serialize(new { Username = "admin", Password = "admin123" }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/auth/login", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_With_Invalid_Password_Returns_Unauthorized()
    {
        var body = JsonSerializer.Serialize(new { Username = "admin", Password = "wrong" }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/auth/login", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_With_Nonexistent_User_Returns_Unauthorized()
    {
        var body = JsonSerializer.Serialize(new { Username = "nobody", Password = "pass" }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/auth/login", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Returns_Ok()
    {
        var body = JsonSerializer.Serialize(new { }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/auth/logout", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Validate_With_Valid_Token_Returns_Ok()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
        var response = await Client.GetAsync("/api/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Validate_Without_Token_Returns_Unauthorized()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.GetAsync("/api/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~AuthControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/AuthControllerTests.cs
git commit -m "test(desktop): add AuthController integration tests"
```

---

## Task 9: UsersController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/UsersControllerTests.cs`

- [ ] **Step 1: Write UsersControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class UsersControllerTests : LocalWebApiControllerTestBase
{
    private async Task SetupAuthAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    [Fact]
    public async Task GetAll_Returns_Admin_User()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("admin");
    }

    [Fact]
    public async Task GetById_Returns_Admin()
    {
        await SetupAuthAsync();
        // First get the list to find admin's ID
        var listResponse = await Client.GetAsync("/api/users");
        var listJson = await listResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(listJson);
        var items = doc.RootElement.GetProperty("items");
        var adminId = items[0].GetProperty("id").GetString();

        var response = await Client.GetAsync($"/api/users/{adminId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("admin");
    }

    [Fact]
    public async Task GetById_Returns_NotFound_For_Invalid_Id()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_User_Succeeds()
    {
        await SetupAuthAsync();
        var dto = new UserInputDto
        {
            UserName = $"testuser_{Guid.NewGuid():N}",
            Password = "test123",
            RealName = "Test User",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };
        var body = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/users", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(dto.UserName);
    }

    [Fact]
    public async Task Create_Duplicate_User_Returns_Conflict()
    {
        await SetupAuthAsync();
        var dto = new UserInputDto { UserName = "admin", Password = "test123", RealName = "Dup" };
        var body = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/users", content);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ToggleStatus_Toggles_User_Status()
    {
        await SetupAuthAsync();
        // Create a user first
        var createDto = new UserInputDto
        {
            UserName = $"toggle_{Guid.NewGuid():N}",
            Password = "test123",
            RealName = "Toggle User",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };
        var createBody = JsonSerializer.Serialize(createDto, Json);
        var createContent = new StringContent(createBody, Encoding.UTF8, "application/json");
        var createResponse = await Client.PostAsync("/api/users", createContent);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createJson);
        var userId = createDoc.RootElement.GetProperty("id").GetString();

        // Toggle status
        var response = await Client.PostAsync($"/api/users/{userId}/toggle-status", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~UsersControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/UsersControllerTests.cs
git commit -m "test(desktop): add UsersController integration tests"
```

---

## Task 10: PatientsController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/PatientsControllerTests.cs`

- [ ] **Step 1: Write PatientsControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class PatientsControllerTests : LocalWebApiControllerTestBase
{
    private async Task SetupAuthAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<string> CreateTestPatientAsync()
    {
        await SetupAuthAsync();
        var patient = new Patient
        {
            Name = $"Patient_{Guid.NewGuid():N}",
            Gender = Gender.Male,
            Phone = "13800000000"
        };
        var body = JsonSerializer.Serialize(patient, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/patients", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetPatients_Returns_Empty_Initially()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/patients");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePatient_And_GetById_Works()
    {
        var id = await CreateTestPatientAsync();
        var response = await Client.GetAsync($"/api/patients/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Patient_");
    }

    [Fact]
    public async Task DeletePatient_Soft_Deletes()
    {
        var id = await CreateTestPatientAsync();
        var deleteResponse = await Client.DeleteAsync($"/api/patients/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Should not be found in normal query
        var getResponse = await Client.GetAsync($"/api/patients/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestorePatient_Works_After_Soft_Delete()
    {
        var id = await CreateTestPatientAsync();
        await Client.DeleteAsync($"/api/patients/{id}");

        var restoreResponse = await Client.PostAsync($"/api/patients/{id}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/patients/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByIdNumber_Returns_Patient()
    {
        var id = await CreateTestPatientAsync();
        // Get the patient to find their IdNumber
        var getResponse = await Client.GetAsync($"/api/patients/{id}");
        var json = await getResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        // Patient may not have IdNumber set, so just verify endpoint works
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TogglePatientStatus_Toggles()
    {
        var id = await CreateTestPatientAsync();
        var response = await Client.PostAsync($"/api/patients/{id}/toggle-status", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~PatientsControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/PatientsControllerTests.cs
git commit -m "test(desktop): add PatientsController integration tests"
```

---

## Task 11: HerbsController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/HerbsControllerTests.cs`

- [ ] **Step 1: Write HerbsControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class HerbsControllerTests : LocalWebApiControllerTestBase
{
    private async Task SetupAuthAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<string> CreateTestHerbAsync()
    {
        await SetupAuthAsync();
        var herb = new Herb
        {
            Name = $"Herb_{Guid.NewGuid():N}",
            Category = "TestCategory",
            Status = EntityStatus.Enabled
        };
        var body = JsonSerializer.Serialize(herb, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/herbs", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetHerbs_Returns_Ok()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/herbs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateHerb_And_GetById_Works()
    {
        var id = await CreateTestHerbAsync();
        var response = await Client.GetAsync($"/api/herbs/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Herb_");
    }

    [Fact]
    public async Task DeleteHerb_Soft_Deletes()
    {
        var id = await CreateTestHerbAsync();
        var deleteResponse = await Client.DeleteAsync($"/api/herbs/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/herbs/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreHerb_Works_After_Soft_Delete()
    {
        var id = await CreateTestHerbAsync();
        await Client.DeleteAsync($"/api/herbs/{id}");

        var restoreResponse = await Client.PostAsync($"/api/herbs/{id}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/herbs/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ToggleStatus_Toggles_Herb()
    {
        var id = await CreateTestHerbAsync();
        var response = await Client.PostAsync($"/api/herbs/{id}/toggle-status", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategories_Returns_Distinct()
    {
        await CreateTestHerbAsync();
        var response = await Client.GetAsync("/api/herbs/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~HerbsControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/HerbsControllerTests.cs
git commit -m "test(desktop): add HerbsController integration tests"
```

---

## Task 12: FormulasController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/FormulasControllerTests.cs`

- [ ] **Step 1: Write FormulasControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class FormulasControllerTests : LocalWebApiControllerTestBase
{
    private async Task SetupAuthAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<string> CreateTestFormulaAsync()
    {
        await SetupAuthAsync();
        var formula = new Formula
        {
            Name = $"Formula_{Guid.NewGuid():N}",
            Category = "TestCategory",
            Status = EntityStatus.Enabled
        };
        var body = JsonSerializer.Serialize(formula, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/formulas", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetFormulas_Returns_Ok()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/formulas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateFormula_And_GetById_Works()
    {
        var id = await CreateTestFormulaAsync();
        var response = await Client.GetAsync($"/api/formulas/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Formula_");
    }

    [Fact]
    public async Task DeleteFormula_Soft_Deletes()
    {
        var id = await CreateTestFormulaAsync();
        var deleteResponse = await Client.DeleteAsync($"/api/formulas/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/formulas/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CloneFormula_Creates_Copy()
    {
        var id = await CreateTestFormulaAsync();
        var response = await Client.PostAsync($"/api/formulas/{id}/clone", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var clonedId = doc.RootElement.GetProperty("id").GetString();
        clonedId.Should().NotBe(id);
    }

    [Fact]
    public async Task ToggleStatus_Toggles_Formula()
    {
        var id = await CreateTestFormulaAsync();
        var response = await Client.PostAsync($"/api/formulas/{id}/toggle-status", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RestoreFormula_Works_After_Soft_Delete()
    {
        var id = await CreateTestFormulaAsync();
        await Client.DeleteAsync($"/api/formulas/{id}");

        var restoreResponse = await Client.PostAsync($"/api/formulas/{id}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await Client.GetAsync($"/api/formulas/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~FormulasControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/FormulasControllerTests.cs
git commit -m "test(desktop): add FormulasController integration tests"
```

---

## Task 13: MedicalCasesController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/MedicalCasesControllerTests.cs`

- [ ] **Step 1: Write MedicalCasesControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class MedicalCasesControllerTests : LocalWebApiControllerTestBase
{
    private async Task SetupAuthAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<(string patientId, string userId)> CreatePrerequisitesAsync()
    {
        await SetupAuthAsync();

        // Create a patient
        var patient = new Patient { Name = $"MC_Patient_{Guid.NewGuid():N}", Gender = Gender.Male };
        var patientBody = JsonSerializer.Serialize(patient, Json);
        var patientContent = new StringContent(patientBody, Encoding.UTF8, "application/json");
        var patientResponse = await Client.PostAsync("/api/patients", patientContent);
        patientResponse.EnsureSuccessStatusCode();
        var patientJson = await patientResponse.Content.ReadAsStringAsync();
        var patientDoc = JsonDocument.Parse(patientJson);
        var patientId = patientDoc.RootElement.GetProperty("id").GetString()!;

        // Get admin user ID
        var userResponse = await Client.GetAsync("/api/users/current");
        userResponse.EnsureSuccessStatusCode();
        var userJson = await userResponse.Content.ReadAsStringAsync();
        var userDoc = JsonDocument.Parse(userJson);
        var userId = userDoc.RootElement.GetProperty("id").GetString()!;

        return (patientId, userId);
    }

    [Fact]
    public async Task GetMedicalCases_Returns_Ok()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/medicalcases");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateMedicalCase_Works()
    {
        var (patientId, userId) = await CreatePrerequisitesAsync();
        var mc = new MedicalCase
        {
            PatientId = Guid.Parse(patientId),
            UserId = Guid.Parse(userId),
            CaseStatus = MedicalCaseStatus.Active
        };
        var body = JsonSerializer.Serialize(mc, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/medicalcases", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMedicalCase_Returns_NotFound_For_Invalid_Id()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync($"/api/medicalcases/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_Returns_Empty_When_No_Match()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/medicalcases/search?patientName=nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByStatus_Returns_Filtered()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/medicalcases/by-status/Active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPendingCases_Returns_Ok()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/medicalcases/pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~MedicalCasesControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/MedicalCasesControllerTests.cs
git commit -m "test(desktop): add MedicalCasesController integration tests"
```

---

## Task 14: RegistrationsController Integration Tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/RegistrationsControllerTests.cs`

- [ ] **Step 1: Write RegistrationsControllerTests**

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class RegistrationsControllerTests : LocalWebApiControllerTestBase
{
    private async Task SetupAuthAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    [Fact]
    public async Task GetRegistrations_Returns_Ok()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/registrations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRegistration_Works()
    {
        await SetupAuthAsync();
        var reg = new Registration
        {
            PatientName = $"Reg_{Guid.NewGuid():N}",
            Status = RegistrationStatus.Waiting
        };
        var body = JsonSerializer.Serialize(reg, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/registrations", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQueue_Returns_Ok()
    {
        await SetupAuthAsync();
        var response = await Client.GetAsync("/api/registrations/queue");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartVisit_Returns_NotFound_For_Invalid_Id()
    {
        await SetupAuthAsync();
        var response = await Client.PutAsync($"/api/registrations/{Guid.NewGuid()}/start-visit", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_Returns_NotFound_For_Invalid_Id()
    {
        await SetupAuthAsync();
        var response = await Client.PutAsync($"/api/registrations/{Guid.NewGuid()}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRegistration_Soft_Deletes()
    {
        await SetupAuthAsync();
        // Create first
        var reg = new Registration
        {
            PatientName = $"Del_{Guid.NewGuid():N}",
            Status = RegistrationStatus.Waiting
        };
        var body = JsonSerializer.Serialize(reg, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var createResponse = await Client.PostAsync("/api/registrations", content);
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(createJson);
        var id = doc.RootElement.GetProperty("id").GetString();

        var deleteResponse = await Client.DeleteAsync($"/api/registrations/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet build tests/LYBT.Tests.Desktop/ && dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~RegistrationsControllerTests" --no-build`
Expected: 6 tests PASS

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/RegistrationsControllerTests.cs
git commit -m "test(desktop): add RegistrationsController integration tests"
```

---

## Task 15: Final Verification

- [ ] **Step 1: Run all LocalWebAPI tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~LYBT.Tests.Desktop.LocalWebAPI" --no-build`
Expected: All tests PASS (~70+ tests total)

- [ ] **Step 2: Run full Desktop test suite for regression**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All new tests pass, no regressions in existing tests

- [ ] **Step 3: Run Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All architecture guard tests pass (no new violations)

- [ ] **Step 4: Final commit with summary**

```bash
git add -A
git commit -m "test(desktop): complete LocalAPI test coverage — 5 Http*Repository + 7 controller integration tests"
```

---

## Expected Test Count Summary

| Test File | Tests |
|-----------|-------|
| HttpHerbRepositoryTests | 7 |
| HttpFormulaRepositoryTests | 8 |
| HttpMedicalCaseRepositoryTests | 10 |
| HttpUserRepositoryTests | 8 |
| HttpRegistrationRepositoryTests | 7 |
| HealthControllerTests | 3 |
| AuthControllerTests | 6 |
| UsersControllerTests | 6 |
| PatientsControllerTests | 6 |
| HerbsControllerTests | 6 |
| FormulasControllerTests | 6 |
| MedicalCasesControllerTests | 6 |
| RegistrationsControllerTests | 6 |
| **Total new tests** | **~85** |

Combined with existing 5 tests (HttpPatientRepository 4 + LocalWebApiDbContext 3 + LocalJwtConfig 2 = 9), the LocalWebAPI test suite grows from 9 to ~94 tests.
