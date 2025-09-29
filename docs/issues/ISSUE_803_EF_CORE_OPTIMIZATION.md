# Issue #803: 优化EF Core查询性能 - 解决N+1问题和添加AsNoTracking

## 📋 问题描述
当前系统存在严重的EF Core查询性能问题：
- 大量查询未使用`AsNoTracking()`，导致不必要的变更跟踪开销
- 存在N+1查询问题，未正确使用`Include()`预加载关联数据
- 未使用投影查询，加载了不必要的字段

## 🎯 优化目标
- 减少数据库查询次数50%以上
- 降低内存占用30%
- 提升查询响应速度40%

## 📁 涉及文件和具体修改

### 1. PatientRepository.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Patients/Repositories/PatientRepository.cs`

#### GetAllAsync() 方法优化
```csharp
// 修改前
public async Task<List<Patient>> GetAllAsync()
{
    return await _context.Patients
        .Where(p => !p.IsDeleted)
        .ToListAsync();
}

// 修改后
public async Task<List<Patient>> GetAllAsync()
{
    return await _context.Patients
        .AsNoTracking()  // 添加AsNoTracking
        .Where(p => !p.IsDeleted)
        .ToListAsync();
}
```

#### SearchAsync() 方法优化
```csharp
// 修改前
public async Task<List<Patient>> SearchAsync(string keyword)
{
    return await _context.Patients
        .Where(p => p.Name.Contains(keyword) || p.Phone.Contains(keyword))
        .ToListAsync();
}

// 修改后
public async Task<List<PatientSearchDto>> SearchAsync(string keyword, int pageSize = 20)
{
    return await _context.Patients
        .AsNoTracking()
        .Where(p => p.Name.Contains(keyword) || p.Phone.Contains(keyword))
        .Select(p => new PatientSearchDto  // 使用投影
        {
            Id = p.Id,
            Name = p.Name,
            Gender = p.Gender,
            Age = p.Age,
            Phone = p.Phone
        })
        .Take(pageSize)
        .ToListAsync();
}
```

#### GetByIdAsync() 方法优化
```csharp
// 添加参数控制是否跟踪
public async Task<Patient?> GetByIdAsync(int id, bool tracking = false)
{
    var query = _context.Patients.Where(p => p.Id == id);

    if (!tracking)
    {
        query = query.AsNoTracking();
    }

    return await query.FirstOrDefaultAsync();
}
```

### 2. ConsultationRepository.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Consultation/Repositories/ConsultationRepository.cs`

#### GetByPatientIdAsync() 方法优化
```csharp
// 修改前
public async Task<List<Consultation>> GetByPatientIdAsync(int patientId)
{
    var consultations = await _context.Consultations
        .Where(c => c.PatientId == patientId)
        .ToListAsync();

    // N+1问题：循环查询处方
    foreach(var consultation in consultations)
    {
        consultation.Prescriptions = await _context.Prescriptions
            .Where(p => p.ConsultationId == consultation.Id)
            .ToListAsync();
    }

    return consultations;
}

// 修改后
public async Task<List<Consultation>> GetByPatientIdAsync(int patientId)
{
    return await _context.Consultations
        .AsNoTracking()
        .Include(c => c.Prescriptions)  // 预加载处方
            .ThenInclude(p => p.PrescriptionDetails)  // 预加载处方详情
        .Where(c => c.PatientId == patientId)
        .OrderByDescending(c => c.ConsultationDate)
        .ToListAsync();
}
```

#### GetPagedAsync() 方法优化
```csharp
// 新增分页查询方法
public async Task<(List<ConsultationListDto> Items, int TotalCount)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    var query = _context.Consultations.AsNoTracking();

    if (startDate.HasValue)
        query = query.Where(c => c.ConsultationDate >= startDate.Value);

    if (endDate.HasValue)
        query = query.Where(c => c.ConsultationDate <= endDate.Value);

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderByDescending(c => c.ConsultationDate)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(c => new ConsultationListDto  // 投影优化
        {
            Id = c.Id,
            PatientId = c.PatientId,
            PatientName = c.Patient.Name,
            ConsultationDate = c.ConsultationDate,
            ChiefComplaint = c.ChiefComplaint,
            Status = c.Status
        })
        .ToListAsync();

    return (items, totalCount);
}
```

### 3. PrescriptionRepository.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Prescriptions/Repositories/PrescriptionRepository.cs`

#### GetByConsultationIdAsync() 方法优化
```csharp
// 修改前
public async Task<List<Prescription>> GetByConsultationIdAsync(int consultationId)
{
    return await _context.Prescriptions
        .Where(p => p.ConsultationId == consultationId)
        .ToListAsync();
    // 问题：未加载PrescriptionDetails和Herbs
}

// 修改后
public async Task<List<Prescription>> GetByConsultationIdAsync(int consultationId)
{
    return await _context.Prescriptions
        .AsNoTracking()
        .Include(p => p.PrescriptionDetails)
            .ThenInclude(pd => pd.Herb)  // 预加载草药信息
        .Where(p => p.ConsultationId == consultationId)
        .ToListAsync();
}
```

#### GetTemplatePrescriptions() 方法优化
```csharp
// 新增模板处方查询（带缓存）
public async Task<List<PrescriptionTemplateDto>> GetTemplatePrescriptionsAsync()
{
    return await _context.Prescriptions
        .AsNoTracking()
        .Where(p => p.IsTemplate)
        .Select(p => new PrescriptionTemplateDto
        {
            Id = p.Id,
            Name = p.TemplateName,
            Description = p.Description,
            HerbCount = p.PrescriptionDetails.Count,
            TotalDosage = p.PrescriptionDetails.Sum(pd => pd.Dosage)
        })
        .ToListAsync();
}
```

### 4. HerbRepository.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Herbs/Repositories/HerbRepository.cs`

#### GetAllAsync() 方法优化
```csharp
// 使用编译查询优化频繁调用
private static readonly Func<AppDbContext, Task<List<HerbDto>>> _getAllHerbs =
    EF.CompileAsyncQuery((AppDbContext context) =>
        context.Herbs
            .AsNoTracking()
            .Where(h => h.IsActive)
            .Select(h => new HerbDto
            {
                Id = h.Id,
                Name = h.Name,
                PinYin = h.PinYin,
                Category = h.Category,
                DefaultDosage = h.DefaultDosage,
                Unit = h.Unit,
                Price = h.Price
            })
            .ToList());

public Task<List<HerbDto>> GetAllAsync()
{
    return _getAllHerbs(_context);
}
```

### 5. UserRepository.cs
**文件路径**: `src/Server/Modules/LYBT.Module.Users/Repositories/UserRepository.cs`

#### GetUserWithRolesAsync() 方法优化
```csharp
// 修改前
public async Task<User?> GetUserWithRolesAsync(int userId)
{
    var user = await _context.Users.FindAsync(userId);
    if (user != null)
    {
        user.Roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
    }
    return user;
}

// 修改后
public async Task<User?> GetUserWithRolesAsync(int userId)
{
    return await _context.Users
        .AsNoTracking()
        .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Id == userId);
}
```

## ✅ 验收标准
1. 所有只读查询必须添加`AsNoTracking()`
2. 消除所有N+1查询问题
3. 实施投影查询，只查询需要的字段
4. 对频繁查询使用编译查询
5. 添加分页支持，避免一次加载过多数据
6. 单元测试验证查询性能提升

## 🔧 实施步骤
1. [ ] 审查所有Repository类，标记需要优化的方法
2. [ ] 逐个方法添加AsNoTracking()
3. [ ] 识别并修复N+1查询问题
4. [ ] 实施投影查询优化
5. [ ] 添加编译查询优化
6. [ ] 运行性能测试验证

## 📊 预期效果
- 数据库查询次数：100次/请求 → 20次/请求
- 平均响应时间：150ms → 90ms
- 内存占用：减少30%

## 🏷️ 标签
`performance` `ef-core` `database` `optimization` `mvp`

## 📎 相关文档
- [EF Core性能优化最佳实践](https://docs.microsoft.com/ef/core/performance)
- [SERVER_OPTIMIZATION_PLAN.md](../optimization/SERVER_OPTIMIZATION_PLAN.md)

---
**优先级**: P0（紧急）
**预估工时**: 2天
**负责人**: 待分配
**状态**: 待开始