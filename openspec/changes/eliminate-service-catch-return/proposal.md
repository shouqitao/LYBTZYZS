# Proposal: eliminate-service-catch-return

## Summary

消除Service层97个catch-return反模式，统一使用`ExecuteAsync<T>()`包装器进行异常处理，使Controller层可安全删除try-catch块并依赖全局`IExceptionHandler`。

## Motivation

### 当前问题

1. **违反SVC-003规范**: 97个Service方法手动catch异常并return `Result.Failure()`，未使用`BaseService.ExecuteAsync<T>()`
2. **双重异常处理**: Controller和Service都有try-catch，造成代码冗余和职责混乱
3. **异常信息不一致**: 部分catch块仍使用`ex.Message`暴露敏感信息，违反ERR-012
4. **维护成本高**: 每个方法都需要手写相似的异常处理逻辑

### 目标状态

- Service层统一使用`ExecuteAsync<T>()`，自动处理异常并记录日志
- Controller层仅处理Result返回值，无try-catch块
- 全局`IExceptionHandler`统一处理未捕获异常
- 符合SVC-003和ERR-012规范

## Requirements (Spec Deltas)

### New: SVC-008 ExecuteAsync包装器强制使用

所有可能抛出异常的Service操作 MUST 使用`ExecuteAsync<T>()`包装。

**规范**:
- 数据库操作 SHALL 使用`ExecuteAsync<T>()`包装
- 外部服务调用 SHALL 使用`ExecuteAsync<T>()`包装
- 复杂业务逻辑 SHALL 使用`ExecuteAsync<T>()`包装
- 简单的验证逻辑 MAY 直接返回Result，无需包装

#### Scenario: 数据库CRUD操作
- **GIVEN** Service方法执行数据库CRUD
- **WHEN** 实现方法体
- **THEN** 必须使用`ExecuteAsync<T>()`包装数据库调用
- **AND** 操作名称应描述具体操作（如"创建患者"）

### Modified: SVC-003 错误处理模式 (Update)

新增约束：Service层 SHALL NOT 存在手动try-catch-return Result.Failure()模式。

**新增场景**:

#### Scenario: 禁止catch-return反模式
- **GIVEN** Service方法可能抛出异常
- **WHEN** 实现异常处理
- **THEN** SHALL NOT 使用 `try { ... } catch { return Result.Failure() }` 模式
- **AND** SHALL 使用 `ExecuteAsync<T>()` 替代

## Scope

### 影响模块 (97个catch块)

| 模块 | Service文件 | catch块数量 |
|------|-------------|-------------|
| Users | UserService.cs | 14 |
| Patients | PatientService.cs | 15 |
| Herbs | HerbService.cs | 19 |
| Formula | FormulaService.cs | 17 |
| MedicalCase | MedicalCaseCommandService.cs | 12 |
| MedicalCase | MedicalCaseQueryService.cs | 10 |
| MedicalCase | MedicalCaseStateService.cs | 10 |

### 同步影响

- Controller层：对应Controller的try-catch块可删除
- 测试：部分单元测试需更新断言

## Design

### 重构模式

**Before (反模式)**:
```csharp
public async Task<Result<PatientDto>> CreateAsync(PatientInputDto dto)
{
    try
    {
        var entity = _mapper.Map<Patient>(dto);
        await _repository.AddAsync(entity);
        return Result<PatientDto>.Success(_mapper.Map<PatientDto>(entity));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建患者失败");
        return Result<PatientDto>.Failure("创建患者失败");
    }
}
```

**After (目标模式)**:
```csharp
public Task<Result<PatientDto>> CreateAsync(PatientInputDto dto)
{
    return ExecuteAsync(async () =>
    {
        var entity = _mapper.Map<Patient>(dto);
        await _repository.AddAsync(entity);
        return _mapper.Map<PatientDto>(entity);
    }, "创建患者");
}
```

### 实施策略

1. **Phase 1**: Users/Patients模块 (29个catch块)
2. **Phase 2**: Herbs/Formula模块 (36个catch块)
3. **Phase 3**: MedicalCase模块 (32个catch块)

每Phase完成后运行测试验证。

## Risks

- **回归风险**: 低 - `ExecuteAsync<T>()`已在部分Service中使用，模式成熟
- **测试影响**: 中 - 部分测试可能需要更新mock设置
- **性能影响**: 无 - 仅代码结构变更，无运行时开销

## Success Criteria

1. Service层0个手动try-catch-return Result.Failure()模式
2. 所有数据库操作使用ExecuteAsync包装
3. Controller层无try-catch块（依赖IExceptionHandler）
4. 所有单元测试通过
5. 编译无警告
