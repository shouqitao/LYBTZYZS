# Server端全栈重构需求文档

**文档类型**: 需求规格说明 (Requirements Specification)
**创建时间**: 2025-10-27
**重构策略**: 激进式重构，不考虑兼容性
**目标**: 代码与需求完全一致，有需求、有文档、有代码

---

## 📋 一、功能概述

### 1.1 重构目标

对Server端（从API到数据库）进行全栈架构重构，清理代码与需求的分歧，消除冗余设计，确保架构符合DDD最佳实践和项目规范。

### 1.2 业务价值

1. **代码质量提升** - 删除约~200行冗余代码，降低维护成本
2. **架构一致性** - 符合AR-001聚合根规范，清晰的职责边界
3. **技术债清理** - 消除PrescriptionsController等无功能组件
4. **可维护性增强** - 统一的代码组织和访问路径

### 1.3 关联Issues

- 基于分析报告：`docs/reports/server-refactor-analysis-2025-10-27.md`
- 验证文档：`docs/business-rules.md`（AR-001聚合根规则）
- 架构文档：`docs/architecture/server/README.md`

---

## 二、发现的问题（来自分析报告）

### 2.1 🔴 严重问题

#### 问题1：PrescriptionsController完全冗余

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs`

**当前状态**:
- Controller壳子存在
- 无任何API端点
- 仅保留注释："Write方法已移除（Issue #1600 Phase 4）"

**问题分析**:
- 完全没有功能，纯粹的"僵尸代码"
- 占用命名空间和文件结构
- 误导开发者（可能认为该文件有实际功能）

**决策**: **直接删除文件**

#### 问题2：PrescriptionService冗余（无对应Controller）

**文件**:
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- `src/Server/Core/LYBT.Server.Interfaces/Services/IPrescriptionService.cs`

**当前状态**:
- Service存在，包含约~200行业务逻辑
- 但PrescriptionsController无任何端点，Service未被使用
- 所有Prescription写操作已通过MedicalCaseService实现

**问题分析**:
- Service层代码冗余，无调用者
- 维护成本高（需要同步更新，但实际无用）
- 违反YAGNI原则（You Aren't Gonna Need It）

**决策**: **删除PrescriptionService和IPrescriptionService**

### 2.2 ⚠️ 建议确认项（需讨论）

#### 建议1：ConsultationController端点去重

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs`

**当前状态**:
- 保留4个只读查询端点（符合AR-001规则）
- 注释明确说明"Read Layer"和"所有Write操作请使用MedicalCaseController"

**端点清单**:
```csharp
GET /api/v1/consultations?page=1&pageSize=10    // 分页查询
GET /api/v1/consultations/{id}                  // 详情查询
GET /api/v1/consultations/medicalcase/{id}      // 根据MedicalCaseId查询
GET /api/v1/consultations/search?keyword=XXX    // 搜索
```

**问题分析**:
1. ✅ **符合AR-001规则**："读操作可绕过聚合根"（明确允许）
2. ⚠️ **查询路径重复**：
   - 可以通过`/consultations/{id}`查询Consultation
   - 也可以通过`/medicalcases/{id}`加载Consultation（Include）
   - 两种路径都能获取相同的数据

3. ⚠️ **端点必要性疑问**：
   - `GET /consultations/medicalcase/{id}` - 功能与`GET /medicalcases/{id}/consultation`重复
   - `GET /consultations/search` - 可以通过`GET /medicalcases/search?includeConsultation=true`替代

**用户决策点**:
> ❓ **请确认**：是否保留ConsultationController的4个只读端点？
>
> **选项A（保守）**：保留所有4个端点（符合AR-001，无架构违规）
> - ✅ 优点：完全符合文档规范，无需修改Client端
> - ❌ 缺点：查询路径重复，维护两套端点
>
> **选项B（激进）**：删除冗余端点，仅保留必要的2个
> - 删除：`GET /consultations/medicalcase/{id}`（可用MedicalCaseController替代）
> - 删除：`GET /consultations/search`（可用MedicalCaseController替代）
> - 保留：`GET /consultations?page=1`（独立的Consultation分页查询）
> - 保留：`GET /consultations/{id}`（快速查询Consultation详情）
> - ✅ 优点：减少端点数量，清晰的职责分工
> - ❌ 缺点：需要更新Client端部分调用
>
> **选项C（极端激进）**：完全删除ConsultationController
> - ⚠️ 风险：违反用户意图（文档明确允许读操作绕过聚合根）
> - ❌ 不推荐

**推荐决策**: **选项B（激进）** - 删除冗余端点，保留核心查询功能

#### 建议2：ConsultationService是否需要简化

**文件**:
- `src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs`
- `src/Server/Core/LYBT.Server.Interfaces/Services/IConsultationService.cs`

**当前状态**:
- Service包含约~130行业务逻辑
- 提供4个查询方法（对应ConsultationController的4个端点）

**问题分析**:
1. ✅ **有存在价值**：支持ConsultationController的查询功能
2. ⚠️ **可能简化**：如果删除部分端点，可以简化Service方法

**用户决策点**:
> ❓ **请确认**：ConsultationService如何处理？
>
> **选项A（保留）**：完全保留（如果选择保留ConsultationController所有端点）
>
> **选项B（简化）**：删除冗余方法（如果选择删除部分ConsultationController端点）
> - 删除：`GetByMedicalCaseIdAsync()`（对应`GET /consultations/medicalcase/{id}`）
> - 删除：`SearchAsync()`（对应`GET /consultations/search`）
> - 保留：`GetPagedAsync()`和`GetByIdAsync()`
>
> **选项C（删除）**：完全删除ConsultationService（如果选择完全删除ConsultationController）
> - ⚠️ 风险：违反AR-001规则，不推荐

**推荐决策**: **选项B（简化）** - 与Controller端点保持一致

#### 建议3：ConsultationRepository可见性调整

**文件**:
- `src/Server/Modules/LYBT.Module.Consultation/Repositories/ConsultationRepository.cs`
- `src/Server/Modules/LYBT.Module.Consultation/Interfaces/IConsultationRepository.cs`

**当前状态**:
- `public class ConsultationRepository`
- 继承自`BaseRepository`，拥有Add/Update/Delete等写方法
- 实际代码中未调用写方法（grep检测：0次调用）

**问题分析**:
1. ✅ **当前安全**：无代码绕过聚合根直接写入
2. ⚠️ **潜在风险**：理论上可以绕过聚合根调用写方法
3. ✅ **查询功能必要**：ConsultationService依赖此Repository

**用户决策点**:
> ❓ **请确认**：如何防止绕过聚合根的写操作？
>
> **选项A（改为internal）**：
> ```csharp
> internal class ConsultationRepository : BaseRepository<ConsultationEntity>
> ```
> - ✅ 优点：防止外部Service绕过聚合根
> - ✅ 保留查询功能（ConsultationService仍可访问，因为同一Assembly）
> - ❌ 缺点：需要确认ConsultationService和Repository在同一Assembly
>
> **选项B（改为只读Repository）**：
> ```csharp
> public class ConsultationRepository : IConsultationQueryRepository  // 不继承BaseRepository
> {
>     // 仅保留Get/Find等查询方法
>     public async Task<ConsultationEntity?> GetByIdAsync(Guid id) { ... }
>     public async Task<PagedResult<ConsultationEntity>> GetPagedAsync(...) { ... }
>     // 移除所有Add/Update/Delete方法
> }
> ```
> - ✅ 优点：完全禁止写操作，架构最安全
> - ❌ 缺点：需要重构Repository接口和实现（工作量较大）
>
> **选项C（保持public，依赖代码审查）**：
> - ⚠️ 风险：理论上可以绕过聚合根，依赖开发者自律

**推荐决策**: **选项A（改为internal）** - 平衡安全性和工作量

### 2.3 ✅ 通过检查（无需修复）

1. **技术黑名单** - 未发现Redis、CQRS、MediatR等禁用技术
2. **依赖方向** - Presentation → Application → Infrastructure依赖方向正确
3. **依赖注入** - 所有业务代码使用构造函数注入（符合规范）

---

## 三、重构范围

### 3.1 必须删除的组件（无争议）

| 组件 | 文件路径 | 原因 | 代码行数 |
|------|---------|------|---------|
| PrescriptionsController | `LYBT.WebAPI/Controllers/PrescriptionsController.cs` | 完全为空，无功能 | ~40行 |
| PrescriptionService | `LYBT.Module.Prescriptions/Services/PrescriptionService.cs` | 无调用者，冗余 | ~200行 |
| IPrescriptionService | `LYBT.Server.Interfaces/Services/IPrescriptionService.cs` | 对应Service冗余 | ~20行 |

**小计**：3个文件，约**260行代码**

### 3.2 需用户确认的组件

| 组件 | 当前状态 | 推荐方案 | 影响 |
|------|---------|---------|------|
| ConsultationController | 4个只读端点 | **删除2个冗余端点** | 需更新Client端部分调用 |
| ConsultationService | 4个查询方法 | **删除2个冗余方法** | 与Controller保持一致 |
| ConsultationRepository | public可见性 | **改为internal** | 防止绕过聚合根写操作 |

**预计删除代码量**（如果选择推荐方案）：约**60行**

### 3.3 总计删除代码量

- **必须删除**：约260行
- **建议删除**（用户确认后）：约60行
- **总计**：约**320行代码**（修正后，原估算560行过高）

---

## 四、验收标准

### 4.1 编译质量标准

- ✅ 编译通过：0 errors, 0 warnings
- ✅ 所有引用正确
- ✅ 类型检查通过

### 4.2 架构合规性标准

- ✅ **AR-001规则验证**：
  - 所有Consultation/Prescription的写操作通过MedicalCase聚合根
  - 读操作可以绕过聚合根（ConsultationController保留只读端点）
  - ConsultationRepository无法从外部调用写方法（internal可见性）

- ✅ **依赖方向验证**：
  - Presentation → Application → Infrastructure → Domain

- ✅ **技术黑名单验证**：
  - 无Redis、CQRS、MediatR等禁用技术

### 4.3 功能完整性标准

- ✅ **运行时验证**（强制）：
  - 启动应用（Client + Server）
  - 执行具体操作场景
  - 验证数据库状态
  - 确认所有现有功能正常

- ✅ **API端点验证**：
  - 所有保留的端点可正常访问
  - 返回数据格式正确
  - 错误处理符合规范

### 4.4 文档同步标准

- ✅ **API文档更新**：
  - 更新`docs/api/consultation-api.md`（如果删除端点）
  - 更新`docs/api/medicalcase-api.md`（如果新增替代端点）

- ✅ **架构文档更新**：
  - 更新`docs/architecture/server/README.md`（说明Controller清理）
  - 更新`docs/modules/consultation/README.md`（说明Service简化）

- ✅ **代码模式文档更新**：
  - 更新`docs/quick-reference/code-patterns.md`（Repository可见性规范）

---

## 五、实施计划（分Phase）

### Phase 1：删除无争议组件（优先级：高）

**目标**：删除PrescriptionsController和PrescriptionService

**步骤**：
1. 删除PrescriptionsController.cs
2. 删除PrescriptionService.cs
3. 删除IPrescriptionService.cs
4. 清理Module注册代码（PrescriptionModule.cs）
5. 编译验证（0 errors, 0 warnings）

**预计工作量**：2小时

**验收标准**：
- ✅ 编译通过
- ✅ 无任何对PrescriptionService的引用
- ✅ PrescriptionsController路由无法访问

---

### Phase 2：简化ConsultationController（优先级：中，需用户确认）

**前置条件**：用户确认选择选项B（激进）

**目标**：删除冗余端点，保留核心查询

**步骤**：
1. 删除ConsultationController的2个端点：
   - `GET /consultations/medicalcase/{id}` → 删除
   - `GET /consultations/search` → 删除
2. 删除ConsultationService的2个方法：
   - `GetByMedicalCaseIdAsync()` → 删除
   - `SearchAsync()` → 删除
3. 更新API文档（`docs/api/consultation-api.md`）
4. 编译验证
5. 运行时验证（测试保留的2个端点）

**预计工作量**：3小时

**验收标准**：
- ✅ 编译通过
- ✅ 保留端点正常工作：
  - `GET /consultations?page=1` ✓
  - `GET /consultations/{id}` ✓
- ✅ 删除的端点返回404

---

### Phase 3：调整Repository可见性（优先级：低，需用户确认）

**前置条件**：用户确认选择选项A（改为internal）

**目标**：防止绕过聚合根的写操作

**步骤**：
1. 确认ConsultationService和ConsultationRepository在同一Assembly
2. 修改ConsultationRepository可见性：
   ```csharp
   // 改为internal
   internal class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
   ```
3. 修改PrescriptionRepository可见性：
   ```csharp
   internal class PrescriptionRepository : BaseRepository<PrescriptionEntity>, IPrescriptionRepository
   ```
4. 编译验证（确保ConsultationService仍可访问）
5. 更新代码模式文档（`docs/quick-reference/code-patterns.md`）

**预计工作量**：1小时

**验收标准**：
- ✅ 编译通过
- ✅ ConsultationService可正常访问ConsultationRepository
- ✅ 外部Module无法访问ConsultationRepository

---

## 六、风险与缓解措施

### 6.1 关键风险

| 风险 | 等级 | 影响 | 缓解措施 |
|------|-----|------|---------|
| Client端调用PrescriptionsController失败 | 🟡 中 | Client端编译错误 | Phase 1完成后立即更新Client端 |
| 删除Consultation端点破坏现有功能 | 🟡 中 | Client端部分功能异常 | Phase 2前确认Client端使用情况 |
| Repository可见性调整导致编译失败 | 🟢 低 | 编译错误 | 先验证Assembly结构，再修改 |

### 6.2 缓解措施

1. **分Phase实施** - 每个Phase独立完成，验证通过后再进行下一阶段
2. **编译 + 运行时验证** - 每个Phase必须通过编译和运行时双重验证
3. **Client端同步更新** - Server端修改完成后立即更新Client端
4. **文档同步更新** - 每个Phase完成后立即更新相关文档

---

## 七、优先级和时间估算

### 7.1 Phase优先级

1. **Phase 1（高）**：删除PrescriptionsController和PrescriptionService - **2小时**
2. **Phase 2（中）**：简化ConsultationController - **3小时**（需用户确认）
3. **Phase 3（低）**：调整Repository可见性 - **1小时**（需用户确认）

### 7.2 总工作量估算

- **最小方案**（仅Phase 1）：2小时
- **推荐方案**（Phase 1 + Phase 2 + Phase 3）：6小时
- **全部完成**（含Client端同步更新）：8-10小时

---

## 八、用户决策清单

> ⚠️ **请用户明确以下决策**，然后进入设计文档阶段：

### 决策点1：ConsultationController端点处理

- [ ] **选项A（保守）** - 保留所有4个端点，无需修改Client端
- [ ] **选项B（激进）** - 删除2个冗余端点，需更新Client端
- [ ] **选项C（极端激进）** - 完全删除ConsultationController（不推荐）

**推荐**：选项B（激进）

---

### 决策点2：ConsultationService处理

- [ ] **选项A（保留）** - 完全保留所有4个方法
- [ ] **选项B（简化）** - 删除2个冗余方法
- [ ] **选项C（删除）** - 完全删除ConsultationService（不推荐）

**推荐**：选项B（简化）

---

### 决策点3：ConsultationRepository可见性

- [ ] **选项A（改为internal）** - 防止外部绕过聚合根
- [ ] **选项B（改为只读Repository）** - 完全禁止写操作（工作量大）
- [ ] **选项C（保持public）** - 依赖代码审查（有风险）

**推荐**：选项A（改为internal）

---

## 九、参考资料

### 分析报告
- `docs/reports/server-refactor-analysis-2025-10-27.md` - 自动化检测报告

### 架构文档
- `docs/business-rules.md` - AR-001聚合根规则
- `docs/architecture/server/README.md` - Server端架构指南

### API文档
- `docs/api/consultation-api.md` - Consultation API端点（需更新）
- `docs/api/medicalcase-api.md` - MedicalCase API端点

### 相关Issues
- #1600: Phase 4 Controller清理（ConsultationController注释来源）

---

## 十、版本历史

| 日期 | 版本 | 变更内容 | 作者 |
|-----|------|---------|------|
| 2025-10-27 | v1.0 | 初始版本，基于分析报告和文档验证 | Claude Code |

---

**下一步**: 等待用户确认三个决策点，然后生成设计文档。
