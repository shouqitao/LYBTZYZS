# P1 Batch1 - 仓储接口继承统一分析报告

**生成时间**: 2025-09-18  
**任务**: P1 Batch1 第五项 - 仓储接口继承统一

## 🎯 任务目标

所有仓储接口改为 I{Domain}Repository : IBaseRepository<TEntity>，确保数据访问层架构一致性。

## 📊 接口继承现状分析

### 已继承 IBaseRepository 的仓储接口 ✅

| 模块 | 接口名称 | 状态 | 实体类型 |
|------|----------|------|----------|
| Users | IUserRepository | ✅ 已继承 | User |
| Patients | IPatientRepository | ✅ 已继承 | Patient |
| Herbs | IHerbRepository | ✅ 已继承 | Herb |
| Auth | IAuthRepository | ✅ 已继承 | User |
| Formula | IFormulaRepository | ✅ 已继承 | Formula |
| Consultation | IConsultationRepository | ✅ 已继承 | Consultation |
| MedicalCase | IMedicalCaseRepository | ✅ 已继承 | MedicalCase |
| Auth | IAuthSessionRepository | ✅ 已继承 | AuthSession |

### 未继承 IBaseRepository 的仓储接口 ❌

| 模块 | 接口名称 | 状态 | 问题 |
|------|----------|------|------|
| Prescriptions | IPrescriptionRepository | ❌ 未继承 | 使用自定义方法，缺乏基础 CRUD 规范 |

## 🔧 问题分析

### IPrescriptionRepository 存在的问题

1. **架构不一致**: 没有继承 IBaseRepository<Prescription>
2. **方法不规范**: 使用自定义命名约定而不是标准 CRUD 方法
3. **功能缺失**: 缺少标准的分页、查询、批量操作等方法
4. **返回类型不一致**: 部分方法返回 bool 而不是标准的实体或操作结果

### 当前 IPrescriptionRepository 方法清单

```csharp
Task<Prescription?> GetByIdAsync(Guid id);      // ✅ 符合标准
Task<List<Prescription>> GetListAsync();        // ❌ 应为 GetAllAsync()
Task<bool> AddAsync(Prescription model);        // ❌ 应返回 Prescription
Task<bool> UpdateAsync(Prescription model);     // ❌ 应返回 Prescription
Task<bool> DeleteAsync(Guid id);               // ✅ 可接受
Task<bool> CancelAsync(Guid id);               // ✅ 业务特定方法
```

## 🔧 修复方案

### 1. 继承 IBaseRepository

将 IPrescriptionRepository 改为继承 IBaseRepository<Prescription>:

```csharp
public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    // 业务特定方法
    Task<bool> CancelAsync(Guid id);
}
```

### 2. 移除重复方法

继承 IBaseRepository 后，以下方法会自动提供：
- GetByIdAsync(Guid id)
- GetAllAsync() (替代 GetListAsync)
- AddAsync(Prescription entity) (返回 Prescription)
- UpdateAsync(Prescription entity) (返回 Prescription)
- DeleteAsync(Guid id)

### 3. 保留业务特定方法

仅保留 IPrescriptionRepository 特有的业务方法：
- CancelAsync(Guid id) - 处方取消业务逻辑

## 📋 实施计划

1. **阶段1**: 修改 IPrescriptionRepository 接口继承 IBaseRepository
2. **阶段2**: 移除与基类重复的方法定义
3. **阶段3**: 验证 PrescriptionRepository 实现类兼容性
4. **阶段4**: 更新相关服务层调用代码

## ⚠️ 影响评估

### 低风险影响
- 接口方法签名保持不变，不影响现有调用
- 新增的基类方法提供更丰富的数据访问能力
- 架构一致性大幅提升

### 潜在影响
- PrescriptionRepository 实现类可能需要更新以实现基类方法
- 服务层可能需要适配新的返回类型

## 🎯 验收标准

- [ ] IPrescriptionRepository 继承 IBaseRepository<Prescription>
- [ ] 移除与基类重复的方法定义
- [ ] 保留业务特定的 CancelAsync 方法
- [ ] 实现类编译通过
- [ ] 相关测试验证通过

## 📝 备注

此修复属于架构一致性调整，符合P1 Batch1"不改业务逻辑，仅做工程与映射一致性调整"的要求。修复后，所有9个仓储接口将统一继承 IBaseRepository，实现数据访问层架构完全一致性。