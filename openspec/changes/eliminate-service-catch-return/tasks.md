# Tasks: eliminate-service-catch-return

**状态**: 已完成
**完成日期**: 2025-12-20
**实际实现方式**: 直接移除try-catch-rethrow反模式，异常由IExceptionHandler统一处理

---

## 实现说明

实际实现采用了更简洁的方案：直接移除冗余的try-catch-rethrow块，而非使用ExecuteAsync包装。

**移除模式**:
```csharp
// BEFORE (反模式)
try {
    // 业务逻辑
} catch (Exception ex) {
    _logger.LogError(ex, "操作失败");
    throw;  // 或 return Result.Failure(...)
}

// AFTER (简洁模式)
// eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
// 业务逻辑（无try-catch包装）
```

**保留的catch块**:
- Fire-and-forget模式（如审计日志、GetOperatorInfoAsync）
- 重试逻辑
- 批处理中的item-level错误隔离

---

## Phase 1: Auth模块 (已完成)

### 1.1 AuthService重构
- [x] 移除try-catch-rethrow反模式

### 1.2 TokenRevocationService重构
- [x] 保留fire-and-forget模式（审计场景）

### 1.3 SecurityAuditService重构
- [x] 保留fire-and-forget模式

---

## Phase 2: Users模块 (已完成)

### 2.1 UserService重构
- [x] 移除14个try-catch-rethrow反模式
- [x] 更新5个单元测试（期望异常而非Result.Failure）

---

## Phase 3: Patients模块 (已完成)

### 3.1 PatientService重构
- [x] 移除15个try-catch-rethrow反模式
- [x] 更新6个单元测试（期望异常而非Result.Failure）

---

## Phase 4: Herbs模块 (已完成)

### 4.1 HerbService重构
- [x] 移除try-catch-rethrow反模式

---

## Phase 5: Formula模块 (已完成)

### 5.1 FormulaService重构
- [x] 移除try-catch-rethrow反模式

---

## Phase 6: MedicalCase模块 (已完成)

### 6.1 MedicalCaseCommandService重构
- [x] 移除try-catch-rethrow反模式

### 6.2 MedicalCaseQueryService重构
- [x] 移除try-catch-rethrow反模式

### 6.3 MedicalCaseStateService重构
- [x] 移除5个try-catch-rethrow反模式
- [x] 保留GetOperatorInfoAsync的fire-and-forget模式

### 6.4 MedicalCaseAuditService
- [x] 保留fire-and-forget模式（审计场景）

---

## Phase 7: 优化补充 (已完成)

### 7.1 TokenRevocationService优化
- [x] 移除RevokeTokenAsync外层catch-return-false（丢失异常信息）
- [x] 移除IsTokenRevokedAsync的catch-return-false（安全隐患：查询异常不应默认返回"未撤销"）
- [x] 保留内层fire-and-forget模式（审计日志失败不影响主操作）

**安全改进**: `IsTokenRevokedAsync`在查询异常时返回`false`(未撤销)会导致可能已撤销的Token被当作有效Token使用，现在异常将正确传播让调用方决定如何处理。

---

## 验证任务 (已完成)

- [x] 运行Auth模块单元测试 - 81通过
- [x] 运行Users模块单元测试 - 31通过
- [x] 运行Patients模块单元测试 - 54通过
- [x] 运行Herbs模块单元测试 - 33通过
- [x] 运行MedicalCase模块单元测试 - 41通过
- [x] 验证编译无警告 - 0警告，0错误
- [x] 代码审查 - 完成

---

## 测试更新汇总

共更新11个测试方法从`_ShouldReturnFailure`改为`_ShouldThrowException`:

**UserServiceTests** (5个):
- GetPagedAsync_WhenRepositoryThrowsException_ShouldThrowException
- GetByIdAsync_WhenRepositoryThrowsException_ShouldThrowException
- CreateAsync_WhenRepositoryThrowsException_ShouldThrowException
- UpdateAsync_WhenRepositoryThrowsException_ShouldThrowException
- DeleteAsync_WhenRepositoryThrowsException_ShouldThrowException

**PatientServiceTests** (6个):
- GetPagedAsync_WhenRepositoryThrowsException_ShouldThrowException
- GetByIdAsync_WhenRepositoryThrowsException_ShouldThrowException
- CreateAsync_WhenRepositoryThrowsException_ShouldThrowException
- UpdateAsync_WhenRepositoryThrowsException_ShouldThrowException
- SearchAsync_WhenRepositoryThrowsException_ShouldThrowException
- DeleteAsync_WhenRepositoryThrowsException_ShouldThrowException

---

## 总测试结果

| 模块 | 通过 | 失败 | 跳过 |
|------|------|------|------|
| Auth | 81 | 0 | 0 |
| Users | 31 | 0 | 0 |
| Patients | 54 | 0 | 0 |
| Herbs | 33 | 0 | 0 |
| MedicalCases | 41 | 0 | 0 |
| **总计** | **240** | **0** | **0** |
