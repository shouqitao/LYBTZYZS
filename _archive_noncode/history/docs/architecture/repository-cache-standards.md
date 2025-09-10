# Repository缓存标准化规范

**项目**: 凌隐宝堂中医诊所系统 (LYBTZYZS)  
**文档类型**: 技术规范  
**创建日期**: 2025-09-02  
**适用范围**: 所有继承OptimizedBaseRepository<T>的Repository类

## 📋 规范概述

本规范基于已成功实施的4个Repository（PrescriptionRepository、ConsultationRepository、MedicalCaseRepository、FormulaRepository）的缓存实践，提取通用的缓存设计标准和最佳实践。

## 🗂️ 缓存键命名规范

### 基础格式
```csharp
var cacheKey = $"{CacheKeyPrefix}{category}:{parameter}";
```

### 标准分类定义

#### 1. 基础CRUD缓存键
```csharp
// 单实体查询（基类自动处理）
{EntityName}:{id}                    // 示例：Prescription:123e4567-...

// 集合查询（基类自动处理）  
{EntityName}:all                     // 示例：Formula:all
```

#### 2. 业务查询缓存键
```csharp
// 按关联实体查询
{EntityName}:patient:{patientId}     // 按患者查询
{EntityName}:doctor:{doctorId}       // 按医生查询
{EntityName}:user:{userId}           // 按用户查询

// 按状态查询
{EntityName}:status:{statusValue}    // 示例：Consultation:status:InProgress

// 按日期范围查询
{EntityName}:daterange:{startDate:yyyyMMdd}-{endDate:yyyyMMdd}

// 特殊查询
{EntityName}:medicalcase:{caseId}    // 按医案查询
{EntityName}:latest:patient:{patientId}  // 最新记录
{EntityName}:templates               // 模板数据
```

#### 3. Include查询缓存键
```csharp
// 包含关联数据的查询
{EntityName}:withItems:{id}          // 示例：Prescription:withItems:123e4567-...
{EntityName}:withConsultation:{id}   // 示例：MedicalCase:withConsultation:123e4567-...
{EntityName}:allWithConsultation     // 示例：MedicalCase:allWithConsultation
```

### 命名规则要求

1. **一致性**: 同类业务使用相同的命名模式
2. **层次化**: 使用冒号(:)分隔层级
3. **参数化**: 动态参数使用大括号{}表示
4. **可读性**: 缓存键名称应该清晰表达查询意图
5. **长度控制**: 总长度不超过100字符

## ⏰ 缓存时间策略

### 标准过期时间定义

#### 1. 默认缓存时间
```csharp
protected virtual TimeSpan DefaultCacheDuration => TimeSpan.FromMinutes(5);
```
- 适用：大部分业务查询
- 场景：相对稳定的数据，允许5分钟的数据延迟

#### 2. 短缓存时间
```csharp
TimeSpan.FromMinutes(1)   // 临时方案或频繁变更数据
TimeSpan.FromMinutes(2)   // 动态数据，如"最新记录"
```

#### 3. 长缓存时间  
```csharp
TimeSpan.FromMinutes(30)  // 相对稳定的查询，如按手机号查询患者
TimeSpan.FromHours(1)     // 统计数据
```

### 缓存时间选择指南

| 数据特征 | 建议时间 | 应用场景 | 示例 |
|---------|----------|----------|------|
| 静态基础数据 | 30分钟 | 验方模板、用户信息 | Formula:templates |
| 常规业务数据 | 5分钟 | 患者记录、诊断记录 | Patient:123, Consultation:patient:456 |
| 动态关联数据 | 2分钟 | 最新记录、实时状态 | MedicalCase:latest:patient:789 |
| 临时方案数据 | 1分钟 | 功能不完整的查询 | Consultation:daterange:20250901-20250902 |
| 统计汇总数据 | 1小时 | 报表、仪表板数据 | Patient:stats:20250901:20250901 |

## 🎯 缓存实现标准模式

### 1. 标准缓存查询模式
```csharp
public async Task<List<Entity>> GetByBusinessLogicAsync(BusinessParams params)
{
    var cacheKey = $"{CacheKeyPrefix}category:{params}";
    
    if (_cache.TryGetValue<List<Entity>>(cacheKey, out var cached) && cached != null)
    {
        _logger.LogDebug("从缓存获取{BusinessDescription} {Params}", params);
        return cached;
    }
    
    var entities = await _dbSet
        .Where(/* business logic */)
        .OrderBy(/* business order */)
        .ToListAsync();
        
    _cache.Set(cacheKey, entities, GetCacheDuration(params));
    return entities;
}
```

### 2. 空值缓存处理
```csharp
// ✅ 推荐：显式检查null
if (_cache.TryGetValue<List<Entity>>(cacheKey, out var cached) && cached != null)

// ✅ 推荐：单实体查询可以缓存null
if (_cache.TryGetValue<Entity?>(cacheKey, out var cached))
{
    return cached; // 可能为null，这是合理的
}
```

### 3. 缓存失效模式
```csharp
// 基类自动处理，业务Repository不需要手动调用
// 在AddAsync、UpdateAsync、DeleteAsync中基类会自动调用InvalidateCache()

// 特殊情况下的手动缓存清理
public async Task<bool> SpecialUpdateAsync(Entity entity)
{
    var result = await base.UpdateAsync(entity);
    
    if (result != null)
    {
        // 手动清理特定缓存
        _cache.Remove($"{CacheKeyPrefix}special:{entity.Id}");
        _logger.LogInformation("特殊更新成功，清理相关缓存 {Id}", entity.Id);
    }
    
    return result != null;
}
```

## 📊 日志记录标准

### 1. 缓存命中日志
```csharp
_logger.LogDebug("从缓存获取{DataType} {Key}", "患者看诊记录", patientId);
_logger.LogDebug("从缓存获取{DataType}", "验方模板列表");
```

### 2. 缓存操作日志
```csharp
// 成功操作后的日志
_logger.LogInformation("新增{EntityType}成功 {Id}", "处方", entity.Id);
_logger.LogInformation("更新{EntityType}成功 {Id}", "医案", entity.Id);
```

### 3. 日志级别使用
- `LogDebug`: 缓存命中信息（开发调试用）
- `LogInformation`: 重要业务操作成功（生产监控用）
- `LogWarning`: 缓存异常但不影响业务
- `LogError`: 缓存严重错误

## 🔍 性能优化要求

### 1. 查询优化
```csharp
// ✅ 推荐：使用AsNoTracking()
var entities = await _dbSet
    .AsNoTracking()  // 只读查询，提升性能
    .Where(condition)
    .ToListAsync();

// ✅ 推荐：合理使用Include
var entities = await _dbSet
    .Include(e => e.RelatedEntity)  // 只Include必要的关联
    .Where(condition)
    .ToListAsync();
```

### 2. 缓存键生成优化
```csharp
// ✅ 推荐：简单字符串拼接
var cacheKey = $"{CacheKeyPrefix}patient:{patientId}";

// ✅ 推荐：复杂参数格式化
var cacheKey = $"{CacheKeyPrefix}daterange:{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";

// ❌ 避免：复杂对象序列化作为key
var cacheKey = JsonSerializer.Serialize(complexObject); // 性能差且不可读
```

### 3. 批量查询优化
```csharp
// 利用基类的批量查询能力
var batchResult = await GetByIdsAsync(ids);
var entities = batchResult.Values.ToList();
```

## 🚫 禁止事项

### 1. 缓存键禁止事项
- ❌ 不要在缓存键中包含敏感信息（密码、token等）
- ❌ 不要使用复杂对象序列化作为缓存键
- ❌ 不要使用过长的缓存键（超过100字符）
- ❌ 不要使用不稳定的动态值（如随机数、时间戳）

### 2. 缓存数据禁止事项
- ❌ 不要缓存敏感数据（密码哈希、个人隐私信息）
- ❌ 不要缓存过大的数据集（单个缓存项>1MB）
- ❌ 不要缓存包含循环引用的对象
- ❌ 不要缓存短时间内必须同步的关键数据

### 3. 缓存时间禁止事项
- ❌ 不要设置过长的缓存时间（>1小时，除非特殊情况）
- ❌ 不要对频繁变更的数据使用长缓存时间
- ❌ 不要忽略缓存时间设置（使用默认永不过期）

## ✅ 最佳实践检查清单

### Repository实现检查
- [ ] 继承OptimizedBaseRepository<T>
- [ ] 构造函数包含ILogger和IMemoryCache参数  
- [ ] 缓存键使用标准命名格式
- [ ] 缓存时间符合数据特征
- [ ] 包含适当的缓存命中日志
- [ ] null值处理正确
- [ ] 查询使用AsNoTracking()优化

### 代码审查检查
- [ ] 缓存键命名清晰且一致
- [ ] 缓存时间选择合理
- [ ] 日志级别和内容恰当
- [ ] 没有缓存敏感或过大数据
- [ ] 错误处理完备
- [ ] 性能优化适当

### 测试验证检查
- [ ] 缓存命中率符合预期
- [ ] 缓存失效机制正常工作
- [ ] 并发访问没有缓存竞争
- [ ] 内存使用保持在合理范围
- [ ] 查询性能有明显提升

## 📈 监控和度量指标

### 1. 缓存性能指标
- **缓存命中率**: 目标>80%
- **平均查询时间**: 缓存命中<5ms，数据库查询<100ms  
- **缓存大小**: 单Repository缓存<50MB
- **缓存过期频率**: 符合设置的过期时间策略

### 2. 业务影响指标
- **API响应时间**: 平均响应时间<500ms
- **数据库连接数**: 峰值连接数<15（小型部署）
- **内存使用率**: 应用内存使用<2GB
- **用户体验**: 页面加载时间<2秒

## 🔄 持续改进建议

### 1. 定期审查
- 每季度审查缓存策略有效性
- 监控缓存命中率和性能指标
- 根据业务发展调整缓存时间和策略

### 2. 新功能开发
- 新Repository必须遵循本规范
- 复杂查询优先考虑缓存优化
- 定期更新本规范以反映最佳实践

### 3. 问题解决
- 建立缓存相关问题的排查流程
- 收集和分析缓存性能数据
- 持续优化缓存策略和实现

---

**规范版本**: v1.0  
**最后更新**: 2025-09-02  
**更新责任人**: UltraThink架构优化团队