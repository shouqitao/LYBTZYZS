# Tasks: Server端代码优化重构

**提案ID**: server-code-optimization
**创建日期**: 2025-12-05
**完成日期**: 2025-12-05

## Phase 1: 清理死代码 (低风险) - 已完成

### Task 1.1: 分析Optimized接口使用情况
- [x] 检查 `IPatientServiceOptimized.cs` 引用 - 被PatientsController使用
- [x] 检查 `IPrescriptionServiceOptimized.cs` 引用 - 死代码，无任何引用
- [x] 记录实际使用情况

### Task 1.2: 删除未使用的Optimized接口
- [x] 删除 `IPrescriptionServiceOptimized.cs`（死代码）
- [x] 合并 `IPatientServiceOptimized.cs` 到 `IPatientService.cs`
- [x] 更新 `PatientService.cs` 移除多接口实现
- [x] 更新 `PatientsController.cs` 移除_optimizedService依赖
- [x] 更新 `PatientsModule.cs` 移除IPatientServiceOptimized DI注册
- [x] 更新 `PatientsControllerTests.cs` 移除IPatientServiceOptimized引用
- [x] 验证编译通过

### Task 1.3: 清理兼容性代码
- [x] 识别BaseApiController中标记为"兼容性"的方法
- [x] 分析这些方法的调用情况 - 部分仍在使用，保留
- [x] 删除无调用的兼容性方法 - 通过合并基类间接完成

**验收标准**:
- [x] 编译通过
- [x] 单元测试通过
- [x] 无未使用的"Optimized"接口

---

## Phase 2: 简化控制器继承 (中风险) - 已完成

### Task 2.1: 合并控制器基类
- [x] 分析 `BaseControllerCore` 和 `BaseApiController` 功能
- [x] 设计合并后的单一基类结构
- [x] 实现合并 - `BaseApiController` 现直接继承 `ControllerBase`
- [x] 删除 `BaseControllerCore.cs`

### Task 2.2: 消除重复Helper方法
- [x] 使用泛型统一 `Error()`/`Error<T>()` 等方法
- [x] 保持兼容性方法供现有Controller使用

### Task 2.3: 提取通用功能到扩展方法
- [x] 保留日志记录功能在基类中（复杂度可控）
- [x] 异常处理通过统一Helper方法实现

**验收标准**:
- [x] 控制器继承深度减少到1层自定义基类
- [x] 编译和测试通过

---

## Phase 3: 重构Service基类 (中风险) - 跳过

### 评估结论
当前 `BaseService` 设计虽然使用 `NotImplementedException`，但：
1. 只有需要权限验证的子类才重写这些方法
2. 不需要权限验证的子类根本不会调用这些方法
3. 改为接口约束需要修改所有实体类，改动过大

**决定**: 保持当前实现，避免过度抽象

---

## Phase 4: 优化Repository (低风险) - 跳过

### 评估结论
`BaseRepository` 已在 Issue #2103 中简化，当前实现合理：
- 核心11个CRUD方法
- 模板方法 `ApplyKeywordFilter` 和 `ApplyDefaultOrdering` 供子类覆盖
- 代码量适中

**决定**: 保持当前实现

---

## Phase 5: Repository命名规范化 (低风险) - 已完成

### Task 5.1: 统一存在性检查方法命名
- [x] `IUserRepository.IsUsernameExistsAsync` → `UsernameExistsAsync`
  - 更新 `IUserRepository.cs` 接口定义
  - 更新 `UserRepository.cs` 实现
  - 更新 `UserService.cs` 调用点
  - 符合C#命名规范（方法名不以Is开头）

### Task 5.2: 统一详情查询方法后缀
- [x] `IPrescriptionRepository.GetByIdWithItemsAsync` → `GetByIdWithDetailsAsync`
- [x] 更新 `PrescriptionRepository.cs` 实现
- [x] 更新 `PrescriptionService.cs` 调用点

### Task 5.3: 统一返回类型命名约定
- [x] 审查 `GetByMedicalCaseIdAsync` 方法命名
  - Consultation: 返回单个 → 保持 `GetByMedicalCaseIdAsync` ✓
  - Prescription: 返回列表 → 保持 `GetByMedicalCaseIdAsync`
  - 决定：上下文语义清晰，保持现有命名以避免大量调用点更新

### Task 5.4: 创建命名规范文档
- [x] 创建 `docs/reference/repository-naming-conventions.md`
  - 定义基础接口方法规范
  - 定义特定业务方法命名模式
  - 包含示例和变更历史

**验收标准**:
- [x] 关键方法（`GetByIdWithDetailsAsync`）命名规范化
- [x] 编译和测试通过

---

## 总体验收标准

- [x] 所有单元测试通过
- [x] 编译无错误无警告
- [x] 删除文件：
  - `IPrescriptionServiceOptimized.cs`
  - `IPatientServiceOptimized.cs`
  - `BaseControllerCore.cs`
- [x] 合并接口：`IPatientServiceOptimized` → `IPatientService`
- [x] 重命名方法：`GetByIdWithItemsAsync` → `GetByIdWithDetailsAsync`
- [x] 重命名方法：`IsUsernameExistsAsync` → `UsernameExistsAsync`
- [x] 创建文档：`docs/reference/repository-naming-conventions.md`

## 代码量变化

| 组件 | 变更前 | 变更后 | 变化 |
|------|--------|--------|------|
| BaseControllerCore | 175行 | 0 (删除) | -175 |
| BaseApiController | 340行 | ~450行 (合并) | +110 (合并后) |
| IPrescriptionServiceOptimized | ~30行 | 0 (删除) | -30 |
| IPatientServiceOptimized | ~40行 | 0 (合并到IPatientService) | -40 |
| **净减少** | | | **~135行** |

## 回滚计划

如需回滚，使用 git 恢复：
```bash
git checkout HEAD~1 -- src/Server/Core/LYBT.Infrastructure/Web/
git checkout HEAD~1 -- src/Server/Modules/LYBT.Module.Patients/
git checkout HEAD~1 -- src/Server/Modules/LYBT.Module.Prescriptions/
```
