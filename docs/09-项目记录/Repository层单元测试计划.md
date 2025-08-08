# Repository 层单元测试计划

## 测试目标

**目标**: 为数据访问层提供全面的单元测试覆盖  
**覆盖率目标**: 85% 以上  
**测试策略**: 使用内存数据库隔离测试

## Repository 清单

### 核心模块 Repository

1. **UserRepository** - 用户数据访问
2. **PatientRepository** - 患者数据访问
3. **HerbRepository** - 药材数据访问
4. **ConsultationRepository** - 看诊数据访问
5. **PrescriptionRepository** - 处方数据访问
6. **MedicalCaseRepository** - 医疗案例数据访问
7. **DoctorRepository** - 医生数据访问
8. **BillingRepository** - 收费数据访问

### 辅助模块 Repository

9. **FormulaTemplateRepository** - 验方模板数据访问
10. **QueueingRepository** - 排队叫号数据访问
11. **TreatmentRoomRepository** - 治疗室数据访问
12. **PharmacyRepository** - 药房数据访问

## 测试范围

### 1. 基础 CRUD 操作
- Create: 创建实体
- Read: 查询单个/多个实体
- Update: 更新实体
- Delete: 删除实体（软删除）

### 2. 复杂查询测试
- 分页查询
- 条件筛选
- 排序功能
- 关联查询（Include）

### 3. 业务逻辑测试
- 唯一性约束
- 级联操作
- 软删除逻辑
- 时间戳自动更新

### 4. 并发和事务测试
- 并发更新
- 事务回滚
- 死锁处理
- 批量操作

### 5. 性能相关测试
- 大数据量查询
- 索引效果验证
- N+1 查询问题

## 技术方案

### 测试框架
```csharp
// 使用技术栈
- xUnit: 测试框架
- FluentAssertions: 断言库
- Microsoft.EntityFrameworkCore.InMemory: 内存数据库
- Bogus: 测试数据生成
```

### 测试基类设计
```csharp
public abstract class RepositoryTestBase<TEntity, TRepository> : IDisposable
    where TEntity : BaseEntity
    where TRepository : IRepository<TEntity>
{
    protected AppDbContext Context { get; }
    protected TRepository Repository { get; }
    
    // 通用测试方法
    protected async Task TestCreateAsync();
    protected async Task TestGetByIdAsync();
    protected async Task TestUpdateAsync();
    protected async Task TestDeleteAsync();
    protected async Task TestGetPagedAsync();
}
```

### 测试数据生成器
```csharp
public static class TestDataGenerators
{
    public static Faker<User> UserGenerator { get; }
    public static Faker<Patient> PatientGenerator { get; }
    public static Faker<Herb> HerbGenerator { get; }
    // ... 其他实体生成器
}
```

## 实施计划

### 第一阶段：基础测试（3小时）
1. 创建测试基类和辅助类
2. 实现 UserRepository 测试
3. 实现 PatientRepository 测试
4. 实现 HerbRepository 测试

### 第二阶段：核心业务测试（3小时）
1. 实现 ConsultationRepository 测试
2. 实现 PrescriptionRepository 测试
3. 实现 MedicalCaseRepository 测试
4. 实现关联查询测试

### 第三阶段：辅助模块测试（2小时）
1. 实现其他 Repository 测试
2. 完善边界条件测试
3. 性能测试场景

### 第四阶段：整合和报告（2小时）
1. 运行所有测试
2. 生成覆盖率报告
3. 优化未覆盖代码
4. 编写测试文档

## 测试用例示例

### UserRepository 测试用例
1. **创建用户**
   - 正常创建
   - 用户名重复
   - 必填字段验证

2. **查询用户**
   - 按ID查询
   - 按用户名查询
   - 分页查询
   - 模糊搜索

3. **更新用户**
   - 更新基本信息
   - 更新密码
   - 并发更新

4. **删除用户**
   - 软删除
   - 级联影响
   - 恢复删除

### 复杂查询测试用例
1. **关联查询**
   - Patient with MedicalCases
   - Consultation with Prescriptions
   - 多级关联

2. **聚合查询**
   - 统计看诊数量
   - 计算处方金额
   - 药材库存汇总

## 预期成果

1. **测试文件数量**: 12+ 个
2. **测试用例数量**: 200+ 个
3. **代码覆盖率**: 85%+
4. **执行时间**: < 30秒

## 质量标准

1. **命名规范**: 测试方法名清晰描述测试场景
2. **独立性**: 每个测试独立运行，不依赖其他测试
3. **可重复性**: 测试结果稳定可重复
4. **性能**: 单个测试执行时间 < 100ms

## 风险和挑战

1. **内存数据库限制**
   - 某些 SQL Server 特性不支持
   - 需要针对性调整

2. **测试数据管理**
   - 避免数据污染
   - 合理的数据清理策略

3. **异步测试**
   - 正确处理异步操作
   - 避免死锁

## 成功标准

- ✅ 所有 Repository 都有对应的测试类
- ✅ 核心功能覆盖率 > 90%
- ✅ 整体覆盖率 > 85%
- ✅ 所有测试稳定通过
- ✅ 测试执行时间合理