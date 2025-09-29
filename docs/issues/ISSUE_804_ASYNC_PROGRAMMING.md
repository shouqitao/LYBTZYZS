# Issue #804: 修复同步阻塞问题并规范异步编程模式

## 📋 问题描述
系统中存在多处同步阻塞调用，违反了异步编程最佳实践：
- 使用`.Result`和`.Wait()`导致线程阻塞
- 未正确传递`CancellationToken`
- 独立的异步操作未并行执行
- 缺少`ConfigureAwait(false)`配置

## 🎯 优化目标
- 消除所有同步阻塞调用
- 实现请求取消支持
- 并行化独立的异步操作
- 提升系统响应能力

## 📁 涉及文件和具体修改

### 1. AuthService.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`

#### 修复ValidateUserAsync()同步阻塞
```csharp
// 修改前
public AuthService(IUserService userService)
{
    _userService = userService;
    // 错误：构造函数中同步阻塞
    var admin = _userService.GetUserByNameAsync("admin").Result;
}

// 修改后
public AuthService(IUserService userService)
{
    _userService = userService;
    // 延迟初始化或使用工厂模式
}

// 添加异步初始化方法
public async Task InitializeAsync()
{
    var admin = await _userService.GetUserByNameAsync("admin")
        .ConfigureAwait(false);
}
```

#### 添加CancellationToken支持
```csharp
// 修改前
public async Task<TokenResponse> LoginAsync(LoginRequest request)
{
    var user = await _userService.ValidateUserAsync(request.Username, request.Password);
    // ...
}

// 修改后
public async Task<TokenResponse> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
{
    var user = await _userService.ValidateUserAsync(
        request.Username,
        request.Password,
        cancellationToken)
        .ConfigureAwait(false);
    // ...
}
```

### 2. UserService.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

#### 改为异步方法
```csharp
// 修改前
public User GetUserById(int userId)
{
    return _userRepository.GetByIdAsync(userId).Result;  // 错误！
}

// 修改后
public async Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
{
    return await _userRepository.GetByIdAsync(userId, cancellationToken)
        .ConfigureAwait(false);
}
```

#### 修复权限验证方法
```csharp
// 修改前
public bool ValidatePermission(int userId, string permission)
{
    var user = GetUserById(userId);  // 同步调用
    return user?.Permissions.Contains(permission) ?? false;
}

// 修改后
public async Task<bool> ValidatePermissionAsync(
    int userId,
    string permission,
    CancellationToken cancellationToken = default)
{
    var user = await GetUserByIdAsync(userId, cancellationToken)
        .ConfigureAwait(false);
    return user?.Permissions.Contains(permission) ?? false;
}
```

### 3. ConsultationService.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs`

#### 并行执行独立操作
```csharp
// 修改前
public async Task<ConsultationDetailsDto> GetDetailsAsync(int consultationId)
{
    // 串行执行，效率低
    var consultation = await _consultationRepo.GetByIdAsync(consultationId);
    var patient = await _patientRepo.GetByIdAsync(consultation.PatientId);
    var prescriptions = await _prescriptionRepo.GetByConsultationIdAsync(consultationId);
    var previousConsultations = await _consultationRepo.GetPreviousAsync(consultation.PatientId);

    return new ConsultationDetailsDto
    {
        Consultation = consultation,
        Patient = patient,
        Prescriptions = prescriptions,
        History = previousConsultations
    };
}

// 修改后
public async Task<ConsultationDetailsDto> GetDetailsAsync(
    int consultationId,
    CancellationToken cancellationToken = default)
{
    // 先获取consultation
    var consultation = await _consultationRepo
        .GetByIdAsync(consultationId, cancellationToken)
        .ConfigureAwait(false);

    if (consultation == null)
        return null;

    // 并行执行独立查询
    var patientTask = _patientRepo.GetByIdAsync(
        consultation.PatientId, cancellationToken);
    var prescriptionsTask = _prescriptionRepo.GetByConsultationIdAsync(
        consultationId, cancellationToken);
    var historyTask = _consultationRepo.GetPreviousAsync(
        consultation.PatientId, cancellationToken);

    await Task.WhenAll(patientTask, prescriptionsTask, historyTask)
        .ConfigureAwait(false);

    return new ConsultationDetailsDto
    {
        Consultation = consultation,
        Patient = await patientTask,
        Prescriptions = await prescriptionsTask,
        History = await historyTask
    };
}
```

### 4. PrescriptionService.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

#### 批量处理优化
```csharp
// 修改前
public async Task<List<PrescriptionResult>> CreateBatchAsync(
    List<CreatePrescriptionDto> prescriptions)
{
    var results = new List<PrescriptionResult>();

    // 串行处理
    foreach(var dto in prescriptions)
    {
        var result = await CreateSingleAsync(dto);
        results.Add(result);
    }

    return results;
}

// 修改后
public async Task<List<PrescriptionResult>> CreateBatchAsync(
    List<CreatePrescriptionDto> prescriptions,
    CancellationToken cancellationToken = default)
{
    // 并行处理，但限制并发度
    var semaphore = new SemaphoreSlim(3, 3);  // 最多3个并发
    var tasks = prescriptions.Select(async dto =>
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await CreateSingleAsync(dto, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    });

    return (await Task.WhenAll(tasks)).ToList();
}
```

### 5. 所有Controller基类改造
**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/*.cs`

#### BaseController添加CancellationToken支持
```csharp
// 创建基类
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    // 获取请求的CancellationToken
    protected CancellationToken RequestCancellationToken => HttpContext.RequestAborted;
}
```

#### PatientsController示例
```csharp
// 修改前
[HttpGet]
public async Task<IActionResult> GetPatients([FromQuery] int page = 1)
{
    var patients = await _patientService.GetPagedAsync(page, 20);
    return Ok(patients);
}

// 修改后
[HttpGet]
public async Task<IActionResult> GetPatients(
    [FromQuery] int page = 1,
    CancellationToken cancellationToken = default)  // 自动绑定
{
    var patients = await _patientService.GetPagedAsync(
        page, 20, cancellationToken);
    return Ok(patients);
}
```

### 6. HerbService.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`

#### 修复同步I/O操作
```csharp
// 修改前
public void LoadHerbsData()
{
    var json = File.ReadAllText("herbs.json");  // 同步I/O
    var herbs = JsonSerializer.Deserialize<List<Herb>>(json);
    // ...
}

// 修改后
public async Task LoadHerbsDataAsync(CancellationToken cancellationToken = default)
{
    var json = await File.ReadAllTextAsync("herbs.json", cancellationToken)
        .ConfigureAwait(false);
    var herbs = JsonSerializer.Deserialize<List<Herb>>(json);
    // ...
}
```

### 7. FormulaService.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

#### 移除Wait()调用
```csharp
// 修改前
public Formula CalculateFormula(int formulaId)
{
    var formula = _formulaRepo.GetByIdAsync(formulaId).Wait();  // 错误！
    // 计算逻辑
    return formula;
}

// 修改后
public async Task<Formula> CalculateFormulaAsync(
    int formulaId,
    CancellationToken cancellationToken = default)
{
    var formula = await _formulaRepo
        .GetByIdAsync(formulaId, cancellationToken)
        .ConfigureAwait(false);
    // 计算逻辑
    return formula;
}
```

### 8. 使用ValueTask优化热路径
```csharp
// 对于可能同步完成的操作，使用ValueTask
public ValueTask<bool> ValidateCacheAsync(string key)
{
    if (_cache.TryGetValue(key, out var value))
    {
        // 同步返回，无堆分配
        return new ValueTask<bool>(true);
    }

    // 异步路径
    return new ValueTask<bool>(ValidateFromDatabaseAsync(key));
}
```

## ✅ 验收标准
1. 消除所有`.Result`和`.Wait()`调用
2. 所有public异步方法接受`CancellationToken`参数
3. Service层方法使用`ConfigureAwait(false)`
4. 独立的异步操作使用`Task.WhenAll()`并行执行
5. 热路径使用`ValueTask`优化
6. 通过代码审查，无死锁风险

## 🔧 实施步骤
1. [ ] 全局搜索`.Result`和`.Wait()`，列出所有问题点
2. [ ] 逐个修复同步阻塞问题
3. [ ] 为所有异步方法添加`CancellationToken`参数
4. [ ] 识别可并行的操作并优化
5. [ ] 添加`ConfigureAwait(false)`
6. [ ] 运行异步分析器验证

## 📊 预期效果
- 消除线程阻塞，提升并发能力
- 支持请求取消，改善用户体验
- 并行优化后响应时间降低30%
- 减少线程池饥饿风险

## 🏷️ 标签
`performance` `async` `threading` `optimization` `mvp`

## 📎 相关文档
- [Async/Await Best Practices](https://docs.microsoft.com/dotnet/csharp/async)
- [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)

---
**优先级**: P0（紧急）
**预估工时**: 1天
**负责人**: 待分配
**状态**: 待开始