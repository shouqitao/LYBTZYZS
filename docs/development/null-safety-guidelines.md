# Null安全编码规范

## 概述

本文档定义了LYBT中医诊所管理系统的null安全编码标准，旨在预防null引用异常，提升代码质量和系统稳定性。

## 核心原则

### 1. 明确的Null契约
- 每个公共API必须明确指示参数和返回值是否可为null
- 使用C# 8.0+的nullable引用类型特性
- 在方法签名中使用`?`明确标记可空类型

### 2. 防御性编程
- 对外部输入进行null检查
- 在方法开始处验证参数
- 使用合理的默认值代替null

### 3. 编译时安全
- 启用nullable引用类型检查
- 修复所有null相关编译警告
- 不使用`!`操作符绕过null检查（除非绝对确定）

## 具体规范

### 1. 方法参数

#### ✅ 推荐做法
```csharp
// 明确标记可空参数
public async Task<User?> GetUserAsync(string? userId)
{
    if (string.IsNullOrEmpty(userId))
        return null;

    return await _repository.FindAsync(userId);
}

// 使用默认值避免null
public void LogMessage(string message, LogLevel level = LogLevel.Info)
{
    // message不可为null，调用方必须提供
    ArgumentNullException.ThrowIfNull(message);
    _logger.Log(level, message);
}
```

#### ❌ 避免做法
```csharp
// 不明确的null契约
public async Task<User> GetUserAsync(string userId)
{
    // 可能抛出NullReferenceException
    return await _repository.FindAsync(userId);
}

// 隐式允许null但未标记
public void ProcessData(string data = null) // 应该是string? data = null
{
    // ...
}
```

### 2. 属性初始化

#### ✅ 推荐做法
```csharp
public class CacheConfiguration
{
    // 使用默认值初始化
    public string CacheKey { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 明确标记可空属性
    public string? Description { get; set; }

    // 使用required修饰符（C# 11）
    public required string Name { get; set; }
}
```

#### ❌ 避免做法
```csharp
public class CacheConfiguration
{
    // CS8618警告：非null属性未初始化
    public string CacheKey { get; set; }
    public List<string> Tags { get; set; }
}
```

### 3. 返回值处理

#### ✅ 推荐做法
```csharp
// 明确的可空返回值
public async Task<Patient?> FindPatientAsync(Guid id)
{
    var patient = await _dbContext.Patients.FindAsync(id);
    return patient; // 可能为null，调用方需要处理
}

// 使用Result模式代替null
public async Task<ServiceResult<Patient>> GetPatientAsync(Guid id)
{
    var patient = await _dbContext.Patients.FindAsync(id);

    if (patient == null)
        return ServiceResult<Patient>.Failure("患者不存在");

    return ServiceResult<Patient>.Success(patient);
}
```

#### ❌ 避免做法
```csharp
// 不明确的返回值契约
public async Task<Patient> GetPatientAsync(Guid id)
{
    return await _dbContext.Patients.FindAsync(id); // 可能返回null
}
```

### 4. Null检查模式

#### ✅ 推荐做法
```csharp
// 使用模式匹配
if (patient is not null)
{
    ProcessPatient(patient);
}

// 使用null条件操作符
var name = patient?.Name ?? "未知";

// 使用null合并赋值
_cache ??= new MemoryCache();

// 参数验证（.NET 6+）
ArgumentNullException.ThrowIfNull(patient);
```

#### ❌ 避免做法
```csharp
// 过时的null检查
if (patient != null)
{
    ProcessPatient(patient);
}

// 不必要的显式null检查
if (patient == null)
    patient = new Patient();
ProcessPatient(patient);
// 应使用：ProcessPatient(patient ?? new Patient());
```

### 5. 集合处理

#### ✅ 推荐做法
```csharp
// 返回空集合而不是null
public IEnumerable<Patient> GetActivePatients()
{
    var patients = _repository.GetAll()
        .Where(p => p.IsActive);

    return patients ?? Enumerable.Empty<Patient>();
}

// 使用IReadOnlyList防止null元素
public IReadOnlyList<string> GetTags()
{
    return _tags.Where(t => !string.IsNullOrEmpty(t)).ToList();
}
```

#### ❌ 避免做法
```csharp
// 返回null集合
public List<Patient> GetPatients()
{
    if (!HasData())
        return null; // 应返回空列表

    return _patients;
}
```

### 6. 异步方法

#### ✅ 推荐做法
```csharp
// Task不应为null，但结果可以
public async Task<string?> GetConfigValueAsync(string key)
{
    var config = await _configService.GetAsync(key);
    return config?.Value; // 明确可能返回null
}

// 使用ValueTask优化
public async ValueTask<Patient?> GetCachedPatientAsync(Guid id)
{
    if (_cache.TryGetValue(id, out var patient))
        return patient;

    return await LoadPatientAsync(id);
}
```

## 工具支持

### 项目配置
```xml
<PropertyGroup>
  <!-- 启用nullable引用类型 -->
  <Nullable>enable</Nullable>

  <!-- 将null警告视为错误（可选） -->
  <WarningsAsErrors>CS8600;CS8601;CS8602;CS8603</WarningsAsErrors>
</PropertyGroup>
```

### 编辑器配置（.editorconfig）
```ini
[*.cs]
# Nullable引用类型
dotnet_diagnostic.CS8600.severity = error  # 将null赋值给非null变量
dotnet_diagnostic.CS8601.severity = error  # 可能的null引用赋值
dotnet_diagnostic.CS8602.severity = error  # 可能的null引用解除
dotnet_diagnostic.CS8603.severity = error  # 可能的null引用返回
```

## 迁移策略

### 现有代码改造步骤

1. **启用Nullable上下文**
   ```csharp
   #nullable enable
   ```

2. **修复编译警告**
   - 优先修复CS8618（属性未初始化）
   - 然后处理CS8625（null字面量）
   - 最后处理CS8600-8604（null传递）

3. **添加null检查**
   ```csharp
   // 边界检查
   public void ProcessOrder(Order? order)
   {
       ArgumentNullException.ThrowIfNull(order);
       // 继续处理...
   }
   ```

4. **使用分析器**
   - 安装`Microsoft.CodeAnalysis.NetAnalyzers`
   - 启用`CA1062`（验证公共方法参数）

## 最佳实践检查清单

- [ ] 所有public方法参数明确标记是否可空
- [ ] 所有public方法返回值明确标记是否可空
- [ ] 类属性在声明时初始化或在构造函数中赋值
- [ ] 使用`string.Empty`代替`null`字符串
- [ ] 返回空集合而不是`null`
- [ ] 使用null条件操作符（`?.`）简化代码
- [ ] 使用null合并操作符（`??`）提供默认值
- [ ] 在方法开始处进行参数验证
- [ ] 避免使用`!`操作符绕过null检查
- [ ] 单元测试包含null场景

## 代码审查要点

1. **API设计审查**
   - 是否必须允许null？
   - 能否使用默认值代替？
   - 是否可以使用Option/Maybe模式？

2. **防御性检查**
   - 外部输入是否验证？
   - 边界条件是否处理？
   - 异常路径是否安全？

3. **文档完整性**
   - XML文档是否说明null行为？
   - 异常文档是否包含`ArgumentNullException`？

## 相关资源

- [C# Nullable Reference Types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [.NET API设计指南](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [防御性编程实践](https://en.wikipedia.org/wiki/Defensive_programming)

---

**文档版本**: 1.0.0
**最后更新**: 2025-09-28
**适用项目**: LYBT中医诊所管理系统
**强制执行**: 是