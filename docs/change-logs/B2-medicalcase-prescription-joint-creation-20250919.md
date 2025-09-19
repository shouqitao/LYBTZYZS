# Phase B2: 医案+处方联建加短事务外壳

**变更日期**: 2025-09-19  
**变更类型**: 事务优化 - 医案+处方联建短事务支持  
**影响模块**: MedicalCase模块, Prescription模块

## 变更概述

为医案业务服务添加了医案+处方联建功能，在单个短事务中创建医案和可选的关联处方，确保数据一致性并优化性能。

## 技术实现

### 1. 新增DTO类型

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDtos.cs`

```csharp
/// <summary>
/// 医案+处方联建创建DTO - Phase B2 事务优化
/// 在单个事务中创建医案和关联处方
/// </summary>
public class MedicalCaseWithPrescriptionCreateDto
{
    public MedicalCaseCreateDto MedicalCase { get; set; } = new();
    public PrescriptionCreateDto? Prescription { get; set; }
    public bool CreatePrescriptionImmediately { get; set; } = false;
}

/// <summary>
/// 医案+处方联建结果DTO - Phase B2 事务优化
/// </summary>
public class MedicalCaseWithPrescriptionResultDto
{
    public MedicalCaseDto MedicalCase { get; set; } = new();
    public PrescriptionDto? Prescription { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}
```

### 2. 业务服务接口扩展

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseBusinessService.cs`

```csharp
/// <summary>
/// 创建医疗案例并关联处方 - Phase B2 事务优化
/// 在单个短事务中创建医案和可选的关联处方
/// </summary>
Task<ServiceResult<MedicalCaseWithPrescriptionResultDto>> CreateWithPrescriptionAsync(
    MedicalCaseWithPrescriptionCreateDto createDto, Guid operatorId, string operatorName);
```

### 3. 短事务实现

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseBusinessService.cs`

**关键技术特点**:

1. **ExecutionStrategy + Transaction**: 使用EF Core的执行策略包装短事务
2. **原子性操作**: 医案创建 → 处方创建 → 处方项目创建 → 一次SaveChanges
3. **并发控制**: 包含DbUpdateConcurrencyException处理
4. **业务验证**: 患者活跃案例检查、数据完整性验证
5. **审计日志**: 完整的操作日志记录

```csharp
public async Task<ServiceResult<MedicalCaseWithPrescriptionResultDto>> CreateWithPrescriptionAsync(
    MedicalCaseWithPrescriptionCreateDto dto, Guid operatorId, string operatorName)
{
    return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 数据验证和业务规则检查
            // ...

            // Step 1: 创建医疗案例
            var medicalCase = new Entities.MedicalCase.MedicalCase { ... };
            _context.MedicalCases.Add(medicalCase);

            // Step 2: 如果需要，创建关联处方
            if (dto.CreatePrescriptionImmediately && dto.Prescription != null)
            {
                var prescription = new Prescription { ... };
                _context.Prescriptions.Add(prescription);
                
                // 处理处方项目
                foreach (var itemDto in dto.Prescription.Items)
                {
                    var item = new PrescriptionItem { ... };
                    _context.PrescriptionItems.Add(item);
                }
                
                // 建立关联
                medicalCase.PrescriptionId = prescription.Id;
            }

            // 一次性保存所有更改
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            // 返回结果
            return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Success(resultDto);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure("数据已被其他用户修改，请刷新后重试");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure($"医案+处方联建失败: {ex.Message}");
        }
    });
}
```

## 变更清单

### 新增文件
- 无

### 修改文件
1. `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDtos.cs`
   - 新增 `MedicalCaseWithPrescriptionCreateDto` DTO类
   - 新增 `MedicalCaseWithPrescriptionResultDto` DTO类
   - 添加处方模块using引用

2. `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseBusinessService.cs`
   - 新增 `CreateWithPrescriptionAsync` 方法签名

3. `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseBusinessService.cs`
   - 添加处方相关using引用
   - 实现 `CreateWithPrescriptionAsync` 方法
   - 包含完整的短事务逻辑和并发控制

## 技术优势

### 1. 事务一致性
- 医案和处方在单个数据库事务中创建
- 避免部分成功状态（医案创建成功但处方创建失败）
- 支持完整回滚机制

### 2. 性能优化
- 短事务设计：BeginTransaction → 操作 → SaveChanges → Commit
- 减少数据库往返次数（单次SaveChanges）
- ExecutionStrategy自动重试支持

### 3. 并发安全
- DbUpdateConcurrencyException专门处理
- 结合Phase A1的RowVersion并发控制
- 友好的并发冲突提示信息

### 4. 业务完整性
- 患者活跃案例冲突检查
- 数据验证和业务规则执行
- 可选的处方创建（灵活性）

## 使用场景

### 1. 一站式接诊
医生在接诊时可以同时创建医案和初步处方，在单个操作中完成：
- 医案登记
- 处方开具
- 药材选择

### 2. 快速诊疗
对于常见病症，可以基于经验快速创建医案并套用常用处方模板。

### 3. 处方预开
在完成诊断前预先准备处方草稿，减少患者等待时间。

## 风险评估

### 1. 低风险
- **API兼容性**: 新增方法，不影响现有功能
- **数据安全**: 完整事务保护，包含回滚机制
- **并发处理**: 结合RowVersion并发控制

### 2. 注意事项
- **性能影响**: 短事务设计，对数据库连接池影响最小
- **内存使用**: 单次操作涉及多个实体，内存消耗略增
- **复杂度**: 增加代码复杂度，但提供显著业务价值

## 测试建议

### 1. 功能测试
- 仅创建医案（不创建处方）
- 同时创建医案和处方
- 创建包含处方项目的完整流程

### 2. 异常测试
- 数据验证失败
- 患者活跃案例冲突
- 数据库连接异常
- 并发修改冲突

### 3. 性能测试
- 单次操作响应时间
- 并发操作处理能力
- 事务回滚性能

## 回滚方案

如需回滚此变更：

1. **移除新增DTO类**:
   ```bash
   # 从MedicalCaseDtos.cs中移除 MedicalCaseWithPrescriptionCreateDto 和 MedicalCaseWithPrescriptionResultDto
   ```

2. **移除接口方法**:
   ```bash
   # 从 IMedicalCaseBusinessService.cs 中移除 CreateWithPrescriptionAsync 方法签名
   ```

3. **移除实现方法**:
   ```bash
   # 从 MedicalCaseBusinessService.cs 中移除 CreateWithPrescriptionAsync 方法实现
   ```

4. **移除相关using引用**:
   ```bash
   # 移除不再需要的 using LYBT.Entities.Prescriptions 等引用
   ```

5. **数据回滚**: 
   - 此变更未修改数据库结构
   - 如有测试数据需要清理，使用标准的数据清理脚本

## 相关文档

- [Phase A1: RowVersion并发控制](A1-rowversion-concurrency-control-20250919.md)
- [Phase B1: 处方复制短事务](B1-prescription-copy-short-transaction-20250919.md)
- [TX决策检查清单](../TX_DECISION_CHECKLIST.md)
- [TX审计报告](../TX_AUDIT_REPORT.md)

## 后续计划

Phase B2完成后，下一步计划：

- **Phase C1**: Patient/Herb批量导入改为50条/批的短事务模式
- **Phase D1**: 统一SQL Server测试基座（移除LocalDB/SQLite依赖）
- **Phase E1**: 小诊所资源保守配置（连接池、超时等）

---

**变更确认**: Phase B2 - 医案+处方联建短事务支持已完成  
**下一阶段**: Phase C1 - 批量导入事务优化