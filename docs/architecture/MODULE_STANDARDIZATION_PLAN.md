# 模块标准化重构计划 - UltraThink架构统一化

## 🎯 目标

基于UltraThink方法论，将8个核心业务模块进行架构标准化，确保整个Solution的一致性和可维护性。

## 📊 现状分析

### 当前模块列表

1. **LYBT.Module.Auth** - 身份认证和授权
2. **LYBT.Module.Users** - 用户管理  
3. **LYBT.Module.Patients** - 患者档案管理
4. **LYBT.Module.Consultation** - 看诊管理
5. **LYBT.Module.MedicalCase** - 医疗案例管理
6. **LYBT.Module.Prescriptions** - 处方管理
7. **LYBT.Module.Herbs** - 中药材管理
8. **LYBT.Module.Formula** - 验方管理

### 结构一致性问题发现

| 模块 | Interfaces | Repositories | Services | Mapping | Module.cs | Options.cs |
|------|------------|--------------|----------|---------|-----------|------------|
| Auth | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| Users | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Patients | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Consultation | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| MedicalCase | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Prescriptions | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Herbs | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Formula | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

## 🏗️ 标准模块架构定义

### 标准目录结构

```
LYBT.Module.{ModuleName}/
├── Interfaces/           # 服务和仓储接口定义
│   ├── I{ModuleName}Service.cs
│   ├── I{ModuleName}Repository.cs
│   └── I{ModuleName}ValidationService.cs (可选)
├── Repositories/         # 数据访问层实现
│   └── {ModuleName}Repository.cs
├── Services/            # 业务逻辑层实现
│   ├── {ModuleName}Service.cs
│   ├── {ModuleName}ValidationService.cs (可选)
│   └── {ModuleName}CacheService.cs (可选)
├── Mapping/             # AutoMapper配置文件
│   └── {ModuleName}MappingProfile.cs
├── Options/             # 配置选项类 (可选)
│   └── {ModuleName}Options.cs
├── Validators/          # 验证器 (可选)
│   └── {ModuleName}Validator.cs
├── Extensions/          # 扩展方法 (可选)
│   └── {ModuleName}Extensions.cs
├── {ModuleName}Module.cs    # 模块注册类
├── LYBT.Module.{ModuleName}.csproj
├── README.md
└── FUNCTIONALITY.md
```

### 标准接口定义

每个模块应实现以下标准接口模式：

```csharp
// 标准Service接口
public interface I{ModuleName}Service
{
    Task<{Entity}Dto?> GetByIdAsync(Guid id);
    Task<List<{Entity}Dto>> GetListAsync();
    Task<PaginatedResult<{Entity}Dto>> GetPagedAsync({Entity}PagedQueryDto query);
    Task<{Entity}Dto?> AddAsync({Entity}CreateDto dto);
    Task<bool> UpdateAsync({Entity}UpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SetStatusAsync(Guid id, bool isActive);
}

// 标准Repository接口
public interface I{ModuleName}Repository
{
    Task<{Entity}Model?> GetByIdAsync(Guid id);
    Task<List<{Entity}Model>> GetListAsync();
    Task<PaginatedResult<{Entity}Model>> GetPagedAsync({Entity}PagedQueryDto query);
    Task<{Entity}Model> AddAsync({Entity}Model entity);
    Task<bool> UpdateAsync({Entity}Model entity);
    Task<bool> DeleteAsync(Guid id);
}
```

## 🔧 标准化重构步骤

### Phase 1: 缺失组件补全

1. **为没有Mapping目录的模块添加AutoMapper配置**
   - Auth模块缺失Mapping
   
2. **为没有Module.cs的模块添加模块注册类**
   - Consultation, MedicalCase, Prescriptions模块缺失

3. **标准化服务接口**
   - 统一方法命名和返回类型
   - 统一异常处理模式
   - 统一缓存策略

### Phase 2: 代码质量统一

1. **统一异常处理**
   ```csharp
   // 标准异常处理模式
   public async Task<EntityDto?> GetByIdAsync(Guid id)
   {
       try 
       {
           var entity = await _repository.GetByIdAsync(id);
           return entity == null ? null : _mapper.Map<EntityDto>(entity);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error getting {EntityType} by ID: {Id}", typeof(Entity).Name, id);
           throw new BusinessException($"Failed to retrieve {typeof(Entity).Name.ToLower()}", ex);
       }
   }
   ```

2. **统一验证模式**
   ```csharp
   // 标准验证模式
   public async Task<EntityDto?> AddAsync(EntityCreateDto dto)
   {
       if (!await _validationService.ValidateCreateAsync(dto))
           throw new ValidationException("Invalid entity data");
       
       // 实现逻辑...
   }
   ```

3. **统一缓存策略**
   ```csharp
   // 标准缓存模式
   public async Task<List<EntityDto>> GetListAsync()
   {
       var cacheKey = $"{typeof(Entity).Name}:list";
       return await _cacheService.GetOrSetAsync(cacheKey, 
           async () => await LoadEntityListFromDatabase(),
           TimeSpan.FromMinutes(15));
   }
   ```

### Phase 3: 性能优化统一

1. **统一分页查询**
2. **统一缓存策略**  
3. **统一异步操作模式**
4. **统一数据库查询优化**

## 🎯 预期收益

1. **代码一致性**: 所有模块遵循相同的架构模式
2. **可维护性**: 开发者可以快速理解任何模块的结构
3. **测试覆盖**: 统一的测试模式和标准
4. **性能优化**: 统一的缓存和查询优化策略
5. **错误处理**: 统一的异常处理和日志记录

## 📋 执行计划

- [x] Phase 0: 架构分析和规划制定
- [ ] Phase 1: 补全缺失组件 (预计2小时)
- [ ] Phase 2: 代码质量统一 (预计4小时)
- [ ] Phase 3: 性能优化统一 (预计3小时)
- [ ] Phase 4: 测试验证 (预计2小时)

## 🔍 质量指标

- **架构一致性**: 100% (所有模块结构完全一致)
- **编译成功率**: 100% (零错误)
- **代码覆盖率**: 目标从2.76%提升到60%
- **性能基准**: API响应时间 < 2秒
- **文档完整性**: 每个模块都有完整的README和FUNCTIONALITY文档