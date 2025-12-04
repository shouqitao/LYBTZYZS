# Tasks: cleanup-unused-methods

## Overview

删除Repository层5个和Service层9个未被调用的方法，涉及多个模块。

---

## Part A: Repository层清理

### Phase 1: MedicalCase模块清理

#### Task 1.1: 删除GetByDoctorIdAsync
- [x] 从`IMedicalCaseRepository.cs`删除接口定义
- [x] 从`MedicalCaseRepository.cs`删除实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 2: Patient模块清理

#### Task 2.1: 删除GetByDateRangeAsync
- [x] 从`IPatientRepository.cs`删除接口定义
- [x] 从`PatientRepository.cs`删除实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 3: Formula模块清理

#### Task 3.1: 删除GetByCategoryAsync和GetSharedFormulasAsync
- [x] 从`IFormulaRepository.cs`删除两个接口定义
- [x] 从`FormulaRepository.cs`删除两个实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 4: Herbs模块清理

#### Task 4.1: 删除GetByCategoryAsync
- [x] 从`IHerbRepository.cs`删除接口定义
- [x] 从`HerbRepository.cs`删除实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

## Part B: Service层清理

### Phase 5: UserService清理

#### Task 5.1: 删除状态切换方法
- [x] 从`IUserService.cs`删除DisableAsync、EnableAsync、ToggleStatusAsync接口
- [x] 从`UserService.cs`删除对应实现

#### Task 5.2: 删除BatchDeleteAsync
- [x] 从`IUserService.cs`删除BatchDeleteAsync接口
- [x] 从`UserService.cs`删除实现

#### Task 5.3: 删除ResetPasswordAsync重载
- [x] 从`IUserService.cs`删除ResetPasswordAsync(Guid, string)接口
- [x] 从`UserService.cs`删除对应实现
- [x] 保留ResetPasswordAsync(Guid, ResetPasswordRequestDto)版本

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 6: HerbService清理

#### Task 6.1: 删除BatchDeleteAsync
- [x] 从`IHerbService.cs`删除BatchDeleteAsync接口
- [x] 从`HerbService.cs`删除实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 7: FormulaService清理

#### Task 7.1: 删除BatchDeleteAsync
- [x] 从`IFormulaService.cs`删除BatchDeleteAsync接口
- [x] 从`FormulaService.cs`删除实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 8: PatientService清理

#### Task 8.1: 删除SearchEntityAsync
- [x] 从`IPatientServiceOptimized.cs`删除SearchEntityAsync接口
- [x] 从`PatientService.cs`删除实现

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

### Phase 9: TokenRevocationService清理

#### Task 9.1: 删除RevokeAllUserTokensAsync
- [x] 从`ITokenRevocationService.cs`删除接口定义
- [x] 从`TokenRevocationService.cs`删除实现
- [x] 删除`TokenRevocationServiceTests.cs`中的相关测试用例（3个）

**验证**: `dotnet build LYBT.All.sln` - PASSED

---

## Phase 10: 最终验证

### Task 10.1: 完整编译验证
- [x] 运行完整编译: `dotnet build LYBT.All.sln -c Release`
- [x] 确认0 errors, 1 warning (文件锁定警告，非代码问题)

### Task 10.2: 单元测试验证
- [x] 运行单元测试: `dotnet test LYBT.Module.Auth.Tests.dll`
- [x] 确认所有81个测试通过

### Task 10.3: 文档更新
- [x] tasks.md已更新

---

## Dependencies

- 无外部依赖
- Repository清理可与Service清理并行执行
- 同一模块内的清理需顺序执行

## Estimated Effort

- 总工作量: ~60分钟
- 主要是查找和删除代码，验证编译

## Summary

**Completed**: 2024-12-04
**Lines Removed**: ~495行代码
**Methods Deleted**: 14个（5个Repository + 9个Service）
**Tests Deleted**: 3个（RevokeAllUserTokensAsync相关测试）
**Build**: PASSED (0 errors)
**Tests**: PASSED (81/81)
