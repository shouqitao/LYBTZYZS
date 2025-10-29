# 医案三步看诊流程重建需求分析

> **文档版本**: v2.0（已修正）
> **创建日期**: 2025-10-25
> **最后更新**: 2025-10-25
> **状态**: 📝 待审批
> **优先级**: ⭐⭐⭐ MVP P0（最高优先级）
> **需求来源**: Epic #1611 Phase 3 - 医案功能MVP评估

---

## ⚠️ 重要术语说明

本文档使用以下统一术语:

| 术语 | 含义 | 说明 |
|------|------|------|
| **MedicalCase（医案/病案）** | 一次完整的看诊记录 | 用户视角的"一次看诊"，技术视角的聚合根 |
| **Consultation（诊断）** | 医案中的诊断部分 | 仅指四诊（望闻问切）+ 医生诊断结果，不涉及流程状态变化 |
| **Prescription（处方）** | 医案中的处方部分 | 药材清单及用法用量 |
| **诊断Form** | 诊断录入界面 | 不使用"Step 1"命名 |
| **处方Form** | 处方录入界面 | 不使用"Step 2"命名 |
| **完成Form** | 完成确认界面 | 不使用"Step 3"命名 |
| **删除** | 物理删除 | 数据库记录永久删除，不可恢复 |
| **作废** | 软删除 | 设置IsDeleted=true，数据保留可恢复 |

---

## 📋 执行摘要

### 核心问题
当前三步看诊流程虽然标记为"已完成"(Issue #1567),但**实际不可用**:
- 🔴 UI设计混乱,界面不一致
- 🔴 流程逻辑混乱,用户体验差
- 🔴 **MVP基线失效**:无法满足"可以看诊"的基本要求

### 需求概述
**完全重建**三步看诊流程(诊断Form→处方Form→完成Form),包括:
1. ✅ 系统化UI设计规范
2. ✅ 清晰的流程逻辑和状态管理
3. ✅ 动态处方决策(REQ-001)
4. ✅ 灵活的处方删除/作废策略(REQ-002)
5. ✅ 严格的一诊断一处方验证(REQ-005)
6. ✅ 精细的状态管理和时间追踪(REQ-004)

### 业务价值
- ✅ **恢复MVP基线**:系统真正"可以看诊"
- ✅ **提升用户体验**:清晰的UI和流畅的交互
- ✅ **保障数据一致性**:严格的业务规则验证
- ✅ **支撑未来扩展**:稳定的流程基础

### 实施成本
- **预估工期**: 9-14天
- **影响范围**: Desktop端(7个View/ViewModel) + Server端(3个Service/Repository)
- **风险等级**: 中等(已有1370行详细设计文档,风险可控)

---

## 🎯 需求分析

### 1. 问题诊断

#### 1.1 深度代码分析结果

**分析范围**:
- Desktop端核心代码: 2830行(6个View/ViewModel)
- Server端核心代码: 1081行(Controller/Service/Repository)
- 总计: 3911行代码全面分析

**分析方法**:
使用Explore subagent深度分析,生成3911行代码分析报告

**关键发现**:

**P0级严重Bug**(数据丢失风险):

1. **Bug #1: UpdateAsync级联删除策略错误**
   - **位置**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs:600-615`
   - **问题**: 当病案状态变为`Closed`时,**物理删除**Consultation和Prescription
   ```csharp
   // ❌ BUG：级联删除策略错误
   if (existingEntity.Status != Closed && entity.Status == Closed)
   {
       _logger?.LogInformation("删除关联的Consultation和Prescription...");

       // ❌ 物理删除！应该根据Case是否完成决定删除还是作废
       _context.Set<ConsultationEntity>().Remove(existingEntity.Consultation);
       _context.Set<PrescriptionEntity>().Remove(existingEntity.Prescription);
   }
   ```
   - **影响**:
     - 已完成的医案应该作废（软删除），而非物理删除
     - 导致诊疗记录永久丢失,无法追溯历史
   - **根因**: 错误理解`Closed`状态的语义和删除策略

2. **Bug #2: 处方未保存到数据库**
   - **位置**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs:850-870`
   - **问题**: `SavePrescriptionAndUpdateMedicalCaseAsync()`返回mock对象,未调用Repository保存
   ```csharp
   private async Task<Prescription> SavePrescriptionAndUpdateMedicalCaseAsync(...)
   {
       // ❌ 当前是草稿实现，未实际保存到数据库
       return new Prescription
       {
           Id = Guid.NewGuid(),
           // ... mock对象
       };

       // ❌ 缺失：await _repository.SavePrescriptionAsync(...)
   }
   ```
   - **影响**: 处方数据丢失,医生录入的处方信息未持久化
   - **根因**: 开发中间状态遗留,未完成实际保存逻辑

**P1级问题**(运行时异常风险):

3. **Bug #3: 空引用检查缺失**
   - **位置**: `MedicalCaseFlowViewModel.OnNavigatedTo()`
   - **问题**: `GetByIdWithDetailsAsync()`可能返回null,但未检查
   - **影响**: 运行时NullReferenceException
   - **根因**: 错误假设导航参数一定有效

**代码质量评分**:

| 维度 | 评分 | 说明 |
|------|------|------|
| **功能完整度** | 80% | 核心流程框架存在,但关键保存逻辑缺失 |
| **代码质量** | 68/100 | 架构设计正确,但存在P0级数据丢失Bug |
| **UI/UX** | ★★★★☆ (4/5) | 界面框架完整,但逻辑混乱(用户反馈) |
| **架构合规性** | 85% | 三层架构正确,聚合根模式基本正确 |

#### 1.2 用户反馈分析

**用户原话** (2025-10-25):
> "当前系统不满足'可以看诊',UI和逻辑很混乱"

**问题解读**:
1. **Issue #1567误判**: 标记为"完成",但实际不可用
2. **不是体验优化**: 这是核心功能缺失,非锦上添花
3. **MVP基线失效**: 系统无法满足最基本的看诊需求

**影响范围**:
- ❌ 医生无法正常使用系统看诊
- ❌ 已录入的处方数据可能丢失(Bug #2)
- ❌ 已完成的病案诊疗记录可能被误删(Bug #1)

#### 1.3 已有功能盘点

**Desktop端已实现功能** (80%完成度):
1. ✅ 三步看诊流程UI框架(诊断Form→处方Form→完成Form)
2. ✅ 处方编辑器(8列DataGrid,支持手动录入)
3. ✅ 自动计价、重复药材检测
4. ✅ 待看诊队列(患者列表)
5. ⏳ 处方导入功能(UI已存在,功能未实现)

**Server端已实现功能** (75%完成度):
1. ✅ MedicalCase聚合根模式(共享主键1:1:1)
2. ✅ Controller三层架构(参数验证→Service委托→响应封装)
3. ✅ Include策略优化(GetBaseQuery vs GetDetailQuery)
4. ❌ **关键缺陷**: UpdateAsync级联删除策略错误

**业务规则实现状态**:
- ✅ **BF-001**: 一诊断一处方规则(基本实现,但有验证漏洞)
- ⚠️ **BF-002**: 暂存病案删除规则(需要澄清:就诊界面内不可删除,待诊列表中可删除)
- ✅ **BF-003**: 病案状态流转限制(已实现)
- ❌ **BF-004**: 处方删除后病案状态回退(未实现)

---

### 2. 需求定义

#### 2.1 业务目标

**核心目标**: 建立清晰、可用、符合直觉的三步看诊流程

**具体目标**:
1. **可用性**: 医生能够流畅完成诊断→施治→完成的完整流程
2. **一致性**: UI设计统一,交互模式一致,符合用户直觉
3. **可靠性**: 数据保存正确,业务规则严格验证,无数据丢失风险
4. **灵活性**: 支持动态处方决策,支持灵活的处方删除/作废

**非目标** (本次需求不涵盖):
- ❌ 其他患者病案查询(REQ-003) - 辅助功能,MVP后实施
- ❌ 三表共享主键架构优化(REQ-006) - Long-term Epic,不紧急
- ❌ 处方历史复制功能(已有UI,功能未实现)
- ❌ 验方导入功能(已有UI,功能未实现)

#### 2.2 用户故事

**故事1: 健康咨询场景**(REQ-001核心价值)
```
作为医生,
当我在诊断Form发现患者只需健康咨询(不需开处方)时,
我希望能够直接完成诊断,跳过处方Form,
这样可以节省时间,避免不必要的流程跳转。
```

**故事2: 处方决策灵活调整**(REQ-002核心价值)
```
作为医生,
当我在完成诊断后发现病情有变化(需要取消已录入的处方)时,
我希望能够返回诊断Form,修改"是否开方"选项,
系统会提示我选择"删除"或"作废"处方,
这样可以根据实际情况灵活调整诊疗方案。
```

**故事3: 状态追踪和审计**(REQ-004核心价值)
```
作为系统管理员,
当我需要审计诊疗流程的完成时间时,
我希望能够精确追踪诊断完成时间、处方完成时间、病案完成时间,
这样可以分析诊疗效率和质量。
```

**故事4: 数据一致性保障**(REQ-005核心价值)
```
作为医生,
当我使用历史复制或验方导入功能时,
我希望系统能够严格验证一诊断一处方规则,
这样可以避免数据不一致导致的诊疗风险。
```

**故事5: 暂存和恢复**(暂存功能)
```
作为医生,
当我在看诊过程中需要临时离开时,
我希望能够点击"暂存"按钮保存当前进度,
回来后从待诊列表中选择"打开"继续看诊,
这样可以避免数据丢失。
```

#### 2.3 功能需求清单

**需求优先级定义**:
- **P0 (MVP必需)**: 缺少则系统不可用
- **P1 (重要)**: 影响核心体验,但有workaround
- **P2 (增强)**: 辅助功能,非MVP范围

| 需求编号 | 需求名称 | 优先级 | 来源文档 | 实施天数 |
|---------|---------|--------|---------|---------|
| **REQ-001** | 动态流程与开处方决策点 | P0 | medicalcase-consultation-prescription-enhancement-requirements.md | 2-3天 |
| **REQ-002** | 处方删除/作废策略 | P0 | medicalcase-consultation-prescription-enhancement-requirements.md | 1-2天 |
| **REQ-004** | 完成状态管理与时间追踪 | P0 | medicalcase-consultation-prescription-enhancement-requirements.md | 2-3天 |
| **REQ-005** | 严格的一诊断一处方规则 | P0 | medicalcase-consultation-prescription-enhancement-requirements.md | 1-2天 |
| **UI-001** | 系统化UI设计规范 | P0 | 新增(基于用户反馈) | 贯穿全程 |
| **BUG-001** | 修复级联删除/作废策略错误 | P0 | 代码分析发现 | 0.5天 |
| **BUG-002** | 修复处方未保存到数据库 | P0 | 代码分析发现 | 0.5天 |
| **BUG-003** | 修复空引用检查缺失 | P1 | 代码分析发现 | 0.5天 |

**总计**: 9-14天(含UI设计和Bug修复)

---

### 3. 详细需求规格

#### REQ-001: 动态流程与开处方决策点

**需求描述**:
在诊断Form提供"是否开方"决策点,支持动态流程:
- 选择"开方" → 进入处方Form
- 选择"不开方" → 直接进入完成Form

**当前问题**:
- ❌ 无法在诊断Form决定是否开处方
- ❌ 健康咨询、体质分析等场景需要跳过处方
- ❌ 当前workaround:进入处方Form后直接点击"完成施治"(空处方)

**解决方案**:
1. **诊断Form UI增强**:
   - 添加RadioBox:"是否开方?"(默认:是)
   - 选项:开方 / 不开方
2. **流程逻辑调整**:
   - "开方" → 允许进入处方Form
   - "不开方" → 处方Form禁用,直接进入完成Form
3. **状态管理**:
   - MedicalCase.RequiresPrescription字段(Boolean)

**验收标准**:
- ✅ 诊断Form可以选择"是否开方"
- ✅ 选择"不开方"后,处方Form禁用且显示禁用原因
- ✅ 选择"开方"后,处方Form正常可用
- ✅ 状态切换逻辑正确,UI响应及时

**参考文档**:
- `docs/explanation/requirements/medicalcase-consultation-prescription-enhancement-requirements.md` - REQ-001
- `docs/explanation/design/medicalcase-consultation-prescription-enhancement-design.md` - Section 2.1

---

#### REQ-002: 处方删除/作废策略

**需求描述**:
支持在诊断Form灵活删除/作废处方,提供两种处理策略:
- **删除**(物理删除):永久删除,不可恢复
- **作废**(软删除):标记IsDeleted,保留数据可恢复

**当前问题**:
- ❌ 无法灵活删除/作废已创建的处方
- ❌ 病情变化后无法灵活调整处方决策
- ❌ 当前workaround:重新创建医案(成本高)

**解决方案**:

**核心原则**:
- ✅ **处方删除/作废的唯一入口**:诊断Form中的"是否开方"选项
- ❌ **不在处方Form中设计独立的"删除处方"按钮**

**触发流程**:
1. 医生在诊断Form选择"是否开方" = "开方"
2. 进入处方Form,录入并保存处方数据
3. 医生改变主意,不想开处方了
4. **正确操作**:返回诊断Form,修改"是否开方" = "不开方"
5. **系统检测**:数据库中已存在处方记录
6. **系统提示**:
   ```
   已有处方数据，请选择处理方式：
   [ ] 删除（永久删除，不可恢复）
   [ ] 作废（保留数据，标记已作废）

   提示：如果未来可能需要查看此处方记录，建议选择"作废"
   ```
7. 医生选择后,系统执行对应操作

**删除/作废规则**:
- Case未完成 + 用户选择"删除" → 物理删除Prescription
- Case未完成 + 用户选择"作废" → 软删除Prescription（IsDeleted=true）
- Case已完成 → 只允许作废（不允许删除）

**验收标准**:
- ✅ 诊断Form中"是否开方"从"是"改为"否"时,触发删除/作废提示
- ✅ 用户选择"删除"后,Prescription物理删除
- ✅ 用户选择"作废"后,Prescription.IsDeleted=true
- ✅ Case已完成时,只显示"作废"选项

**参考文档**:
- `docs/explanation/requirements/medicalcase-consultation-prescription-enhancement-requirements.md` - REQ-002
- `docs/explanation/design/medicalcase-consultation-prescription-enhancement-design.md` - Section 2.2

---

#### REQ-004: 完成状态管理与时间追踪

**需求描述**:
精细化状态管理,添加时间戳追踪:
- Consultation.CompletedAt:诊断完成时间
- Prescription.CompletedAt:处方完成时间
- MedicalCase.CompletedAt:医案完成时间

**当前问题**:
- ❌ 状态管理粗糙,无法精确追踪各阶段完成时间
- ❌ 数据审计不完整,无法分析诊疗效率
- ❌ 暂存功能需要持久化状态,但当前只有内存状态

**解决方案**:

**采用方案**:数据库持久化（方案B）

**理由**:
1. ✅ **支持暂存恢复**:医生暂存后返回,能恢复到之前的完成状态
2. ✅ **审计追溯**:可以追踪诊断完成时间、处方完成时间
3. ✅ **数据完整性**:状态持久化到数据库,不会因刷新丢失
4. ✅ **与业务规则一致**:符合"已完成Case才能作废"的规则

**数据模型扩展**:
```csharp
public class ConsultationEntity
{
    public DateTime? CompletedAt { get; set; } // 诊断完成时间
    // ... 其他字段
}

public class PrescriptionEntity
{
    public DateTime? CompletedAt { get; set; } // 处方完成时间
    // ... 其他字段
}

public class MedicalCaseEntity
{
    public DateTime? CompletedAt { get; set; } // 医案完成时间
    // ... 其他字段
}
```

**状态判断逻辑**:
- `CompletedAt != null` → 已完成
- `CompletedAt == null` → 未完成

**触发时机**:
- 点击"完成诊断" → 设置`Consultation.CompletedAt = DateTime.Now`
- 点击"完成处方" → 设置`Prescription.CompletedAt = DateTime.Now`
- 点击"完成医案" → 设置`MedicalCase.CompletedAt = DateTime.Now`

**完成Form进入条件**:
- 条件1:`Consultation.CompletedAt != null`(诊断必须完成)
- 条件2:如果`RequiresPrescription = true`,则`Prescription.CompletedAt != null`(处方必须完成)
- 条件3:如果`RequiresPrescription = false`,则`Prescription.CompletedAt`可为null

**验收标准**:
- ✅ 数据库Schema包含3个CompletedAt字段
- ✅ 各阶段完成时自动设置对应时间戳
- ✅ 完成Form进入条件逻辑正确
- ✅ UI显示当前阶段状态和完成时间
- ✅ 暂存后恢复,状态正确

**参考文档**:
- `docs/explanation/requirements/medicalcase-consultation-prescription-enhancement-requirements.md` - REQ-004
- `docs/explanation/design/medicalcase-consultation-prescription-enhancement-design.md` - Section 2.4

---

#### REQ-005: 严格的一诊断一处方规则

**需求描述**:
修复验证逻辑漏洞,确保所有处方创建场景都严格验证一诊断一处方规则:
- 手动录入处方
- 历史复制处方(追加模式)
- 验方导入处方(追加模式)

**当前问题**:
- ❌ 验证逻辑有漏洞:历史复制/验方导入未验证
- ❌ 可能出现一诊断多处方的数据不一致
- ❌ 数据库约束(ConsultationId UNIQUE)可以兜底,但不够友好

**解决方案**:
1. **统一验证逻辑**:
   ```csharp
   // Service层验证
   public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(Guid medicalCaseId, ...)
   {
       // ✅ 验证：是否已存在处方
       var existingPrescription = await _prescriptionRepository.GetByMedicalCaseIdAsync(medicalCaseId);
       if (existingPrescription != null && !existingPrescription.IsDeleted)
       {
           return ServiceResult<PrescriptionDto>.Failure("一诊断只能有一个处方");
       }

       // ... 创建处方
   }
   ```
2. **追加模式支持**:
   - 历史复制:检测到已有处方时,询问"是否覆盖?"或"是否追加到现有处方?"
   - 验方导入:同上
3. **UI反馈增强**:
   - 验证失败时,显示友好的错误消息
   - 避免依赖数据库约束报错

**验收标准**:
- ✅ 所有处方创建场景都验证一诊断一处方规则
- ✅ 验证失败时,显示友好的错误消息
- ✅ 追加模式正确实现(覆盖/追加选项)
- ✅ 数据一致性得到保障

**参考文档**:
- `docs/explanation/requirements/medicalcase-consultation-prescription-enhancement-requirements.md` - REQ-005
- `docs/explanation/business-rules.md` - BF-001

---

#### UI-001: 系统化UI设计规范

**需求描述**:
建立统一的UI设计规范,解决当前"UI混乱"问题:
- 统一的控件样式(Button/TextBox/DataGrid等)
- 一致的交互模式(导航/保存/取消等)
- 清晰的视觉层次(主次关系/颜色/间距)

**当前问题**:
- ❌ UI设计缺乏规范,界面不一致
- ❌ 交互模式混乱,用户体验差
- ❌ 视觉层次不清晰,信息密度过高

**解决方案**:
1. **参考现有UI设计**:
   - 患者管理模块UI(作为参考基准)
   - WPF Material Design规范
2. **制定UI设计清单**:
   - Button样式:Primary/Secondary/Danger
   - TextBox样式:Normal/ReadOnly/Error
   - DataGrid样式:Header/Row/Selection
   - 颜色规范:Primary/Success/Warning/Danger
   - 间距规范:Margin/Padding标准值
3. **应用到医案模块**:
   - MedicalCaseFlowView:统一导航按钮样式
   - DiagnosisEditorView:统一表单控件样式
   - PrescriptionEditorView:统一DataGrid样式
   - CompletionView:统一信息展示样式

**验收标准**:
- ✅ 医案模块UI与患者管理模块UI风格一致
- ✅ 控件样式统一,无明显不一致
- ✅ 交互模式清晰,符合用户直觉
- ✅ 视觉层次清晰,信息密度适中

**参考文档**:
- `docs/explanation/design/medicalcase-consultation-prescription-enhancement-design.md` - Section 6(UI/UX设计)

---

#### BUG-001: 修复级联删除/作废策略错误

**需求描述**:
修复MedicalCaseService.UpdateAsync()中的级联删除策略错误,正确实现"已完成Case作废,未完成Case删除"的顶层规则。

**问题代码**:
```csharp
// ❌ src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs:600-615
if (existingEntity.Status != Closed && entity.Status == Closed)
{
    _logger?.LogInformation("删除关联的Consultation和Prescription...");

    // ❌ 物理删除！应该根据Case是否完成决定删除还是作废
    _context.Set<ConsultationEntity>().Remove(existingEntity.Consultation);
    _context.Set<PrescriptionEntity>().Remove(existingEntity.Prescription);
}
```

**解决方案**:

**核心规则**:
1. ✅ **已完成Case的作废**:作废Case时,同时作废Consultation和Prescription
2. ✅ **未完成Case的删除**:删除Case时,同时删除Consultation和Prescription
3. ✅ **级联恢复规则**:恢复Case时,同时恢复Consultation和Prescription

**实现方式**:
```csharp
// ✅ 修复方案：正确的删除/作废策略
public async Task<ServiceResult> DeleteOrArchiveMedicalCaseAsync(Guid id, bool isCompleted)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);

    if (isCompleted)
    {
        // ✅ 已完成：作废（软删除）
        medicalCase.IsDeleted = true;
        if (medicalCase.Consultation != null)
            medicalCase.Consultation.IsDeleted = true;
        if (medicalCase.Prescription != null)
            medicalCase.Prescription.IsDeleted = true;

        await _repository.UpdateAsync(medicalCase);
    }
    else
    {
        // ✅ 未完成：删除（物理删除）
        await _repository.DeleteAsync(id); // EF会级联删除Consultation和Prescription
    }
}

// ✅ 恢复作废的Case
public async Task<ServiceResult> RestoreMedicalCaseAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);

    // ✅ 恢复Case时,同时恢复Consultation和Prescription
    medicalCase.IsDeleted = false;
    if (medicalCase.Consultation != null)
        medicalCase.Consultation.IsDeleted = false;
    if (medicalCase.Prescription != null)
        medicalCase.Prescription.IsDeleted = false;

    await _repository.UpdateAsync(medicalCase);
}
```

**级联规则总结**:
- ✅ **删除/作废Case** → 同时删除/作废Consultation和Prescription
- ✅ **恢复Case** → 同时恢复Consultation和Prescription
- ✅ **不存在**只删除/作废Consultation或Prescription的场景（除了处方的独立删除/作废）

**验收标准**:
- ✅ 已完成Case作废后,Consultation和Prescription同时作废
- ✅ 未完成Case删除后,Consultation和Prescription同时删除
- ✅ 恢复Case后,Consultation和Prescription同时恢复
- ✅ 历史医案可以正常查询和审计

---

#### BUG-002: 修复处方未保存到数据库

**需求描述**:
修复PrescriptionEditorViewModel.SavePrescriptionAndUpdateMedicalCaseAsync()中的mock实现,确保处方正确保存到数据库。

**问题代码**:
```csharp
// ❌ src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs:850-870
private async Task<Prescription> SavePrescriptionAndUpdateMedicalCaseAsync(...)
{
    // ❌ 当前是草稿实现，未实际保存到数据库
    return new Prescription
    {
        Id = Guid.NewGuid(),
        // ... mock对象
    };
}
```

**解决方案**:
```csharp
// ✅ 修复方案：通过聚合根Repository保存
private async Task<Prescription> SavePrescriptionAndUpdateMedicalCaseAsync(...)
{
    try
    {
        // ✅ 构造PrescriptionCreateDto
        var dto = new PrescriptionCreateDto
        {
            MedicalCaseId = CurrentMedicalCase.Id,
            PrescriptionItems = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
            {
                HerbId = item.HerbId,
                Quantity = item.Quantity,
                // ... 其他字段
            }).ToList()
        };

        // ✅ 通过聚合根Repository保存
        var result = await _medicalCaseRepository.CreatePrescriptionAsync(CurrentMedicalCase.Id, dto);

        if (result.IsSuccess)
            return result.Data;
        else
            throw new Exception(result.Message);
    }
    catch (Exception ex)
    {
        _notificationService.ShowError($"保存处方失败: {ex.Message}");
        throw;
    }
}
```

**验收标准**:
- ✅ 处方数据正确保存到数据库
- ✅ 保存后可以查询到处方记录
- ✅ 保存失败时显示友好错误消息

---

#### BUG-003: 修复空引用检查缺失

**需求描述**:
修复MedicalCaseFlowViewModel.OnNavigatedTo()中的空引用检查缺失,避免运行时NullReferenceException。

**问题代码**:
```csharp
// ❌ src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("medicalCaseId");
    CurrentMedicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // ❌ 未检查null，可能导致NullReferenceException
    LoadConsultationAndPrescription();
}
```

**解决方案**:
```csharp
// ✅ 修复方案：添加null检查
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    try
    {
        var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("medicalCaseId");
        CurrentMedicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

        // ✅ 添加null检查
        if (CurrentMedicalCase == null)
        {
            _notificationService.ShowError($"未找到病案: {medicalCaseId}");
            _navigationService.NavigateBack();
            return;
        }

        LoadConsultationAndPrescription();
    }
    catch (Exception ex)
    {
        _notificationService.ShowError($"加载病案失败: {ex.Message}");
        _navigationService.NavigateBack();
    }
}
```

**验收标准**:
- ✅ 导航参数无效时,显示友好错误消息
- ✅ 病案不存在时,自动返回上一页
- ✅ 不会出现NullReferenceException

---

### 4. 非功能需求

#### 4.1 性能要求

- **页面加载时间**: ≤1秒(包含数据库查询)
- **保存响应时间**: ≤500ms(单次保存操作)
- **UI响应时间**: ≤100ms(按钮点击、输入响应)

#### 4.2 可靠性要求

- **数据一致性**: 100%(严格验证业务规则)
- **数据持久化**: 100%(无数据丢失)
- **异常处理**: 100%覆盖(所有异常都有友好提示)

#### 4.3 可用性要求

- **UI一致性**: 95%控件样式统一
- **交互直觉性**: 90%用户无需培训即可使用
- **错误提示**: 100%友好且可操作

#### 4.4 可维护性要求

- **代码质量**: ≥80/100(架构合规+无Critical Bug)
- **测试覆盖**: ≥70%(核心业务逻辑)
- **文档完整性**: 100%(设计文档+代码注释)

---

### 5. 约束条件

#### 5.1 技术约束

- **技术栈**: .NET 8 + WPF + Prism 9.0 + EF Core 8.0
- **架构模式**: Desktop端MVVM + Server端三层架构
- **聚合根模式**: 所有Write操作必须通过IMedicalCaseRepository
- **Constitution约束**: 禁止使用Redis/CQRS/MediatR/Docker等黑名单技术

#### 5.2 时间约束

- **总工期**: 9-14天
- **分阶段交付**:
  - Phase 1: Bug修复(1天) - BUG-001/002/003
  - Phase 2: 核心流程重构(4-6天) - REQ-001/002/004/005
  - Phase 3: UI设计规范应用(2-3天) - UI-001
  - Phase 4: 测试和验证(2-4天)

#### 5.3 范围约束

**本次需求包含**:
- ✅ REQ-001: 动态流程与开处方决策点
- ✅ REQ-002: 处方删除/作废策略
- ✅ REQ-004: 完成状态管理与时间追踪
- ✅ REQ-005: 严格的一诊断一处方规则
- ✅ UI-001: 系统化UI设计规范
- ✅ BUG-001/002/003: 修复P0/P1级Bug

**本次需求不包含**:
- ❌ REQ-003: 其他患者病案查询(辅助功能,MVP后实施)
- ❌ REQ-006: 三表共享主键架构优化(Long-term Epic)
- ❌ 处方历史复制功能(已有UI,功能未实现)
- ❌ 验方导入功能(已有UI,功能未实现)
- ❌ 作废后的恢复功能UI(API已实现,UI暂不实现)

---

### 6. 验收标准

#### 6.1 功能验收

**必须通过的功能测试**:
1. ✅ **动态流程**:
   - 诊断Form可以选择"是否开方"
   - 选择"不开方"后,处方Form禁用,直接进入完成Form
   - 选择"开方"后,处方Form正常可用

2. ✅ **处方删除/作废**:
   - 诊断Form中"是否开方"从"是"改为"否"时触发提示
   - 用户选择"删除"后,Prescription物理删除
   - 用户选择"作废"后,Prescription.IsDeleted=true
   - Case已完成时,只显示"作废"选项

3. ✅ **状态管理**:
   - 各阶段完成时自动设置对应CompletedAt时间戳
   - 完成Form进入条件逻辑正确
   - UI显示当前阶段状态
   - 暂存后恢复,状态正确

4. ✅ **数据一致性**:
   - 所有处方创建场景都验证一诊断一处方规则
   - 验证失败时显示友好错误消息

5. ✅ **Bug修复**:
   - 已完成Case作废后,Consultation/Prescription同时作废(BUG-001)
   - 未完成Case删除后,Consultation/Prescription同时删除(BUG-001)
   - 恢复Case后,Consultation/Prescription同时恢复(BUG-001)
   - 处方正确保存到数据库(BUG-002)
   - 空引用检查正确处理(BUG-003)

6. ✅ **暂存和恢复**:
   - 待诊列表显示暂存的Case
   - 点击"打开"继续看诊,状态正确恢复
   - 点击"删除后新建"删除暂存Case并创建新Case
   - 点击"删除"仅删除暂存Case

#### 6.2 质量验收

**必须通过的质量标准**:
1. ✅ **编译质量**: 0 errors, 0 warnings
2. ✅ **运行时验证**: 启动应用,完整测试三步流程
3. ✅ **代码质量**: ≥80/100(架构合规+无Critical Bug)
4. ✅ **UI一致性**: 95%控件样式统一
5. ✅ **文档同步**: 100%(设计文档+API文档+模块README)

#### 6.3 用户验收

**必须得到用户确认**:
1. ✅ 用户确认"可以看诊"(MVP基线恢复)
2. ✅ 用户确认"UI清晰一致"(不再混乱)
3. ✅ 用户确认"流程符合直觉"(易用性提升)

---

### 7. 风险评估

#### 7.1 技术风险

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| 状态机逻辑复杂度超预期 | 中 | 中 | 已有1370行详细设计文档,风险可控 |
| UI样式调整工作量大 | 中 | 低 | 可参考患者管理模块,复用现有样式 |
| 数据迁移Script错误 | 低 | 高 | 充分测试,提供回滚脚本 |
| CompletedAt字段Schema变更 | 低 | 中 | 已有migration经验,风险可控 |

#### 7.2 业务风险

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| 用户需求理解偏差 | 低 | 高 | 已有讨论文档和用户确认,Phase 4用户验收 |
| Epic #1343其他任务延期 | 中 | 中 | 三步看诊流程是MVP最核心功能,必须优先 |

#### 7.3 时间风险

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| 工期超出14天 | 低 | 中 | 已拆分为4个Phase,可按Phase验收和调整 |
| Bug修复引入新问题 | 中 | 低 | 充分测试,逐个Bug修复和验证 |

---

## 📚 参考资料

### 已有文档
1. **[功能深化需求](../medicalcase-consultation-prescription-enhancement-requirements.md)** - REQ-001至REQ-006
2. **[详细设计方案](../../design/medicalcase-consultation-prescription-enhancement-design.md)** - 1370行完整技术设计
3. **[功能深化讨论](../../architecture/shared/medicalcase-consultation-prescription-enhancement-discussion.md)** - 完整讨论过程
4. **[MVP聚焦讨论](../../architecture/shared/medicalcase-mvp-focused-discussion.md)** - 需求优先级评估
5. **[业务规则文档](../explanation/business-rules.md)** - 14条核心业务规则

### 代码分析报告
- **深度分析报告**: Epic #1611 Phase 3 - 3911行代码分析(内部文档)
- **分析范围**: Desktop端2830行 + Server端1081行
- **分析方法**: Explore subagent深度分析

### 架构文档
- **ADR-005**: [长期架构演进原则](../../architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md)
- **ADR-003**: [Desktop端Repository层简化](../../architecture/decisions/ADR-003-repository-simplification.md)
- **三层对齐架构**: `docs/explanation/architecture/server/README.md` + `docs/explanation/architecture/client/README.md`

---

## 🔗 关联Issue

- **Epic #1611**: 系统性重构 - 文档-代码对齐与架构优化(Phase 3进行中)
- **Epic #1343**: MVP核心功能(57个子任务,暂缓)
- **Issue #1567**: 三步看诊流程(标记完成,但实际不可用)
- **Issue #1563**: MedicalCase聚合根重构(已完成)

---

## 📅 更新日志

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建(基于Epic #1611 Phase 3深度代码分析) | Claude Code |
| 2025-10-25 | v2.0 | 修正12条术语和逻辑错误,添加暂存/恢复/级联规则 | Claude Code |

### v2.0主要修正内容

1. ✅ **术语统一**:
   - Consultation = 诊断（仅指四诊+诊断结果,不涉及流程状态）
   - 物理删除 → 删除，软删除 → 作废
   - Step1/2/3 → 诊断Form/处方Form/完成Form

2. ✅ **删除策略澄清**:
   - 顶层规则:已完成Case→作废,未完成Case→删除
   - 级联删除/作废规则
   - 级联恢复规则

3. ✅ **流程设计修正**:
   - 处方删除/作废的唯一入口:诊断Form的"是否开方"选项
   - 取消操作针对整个Case,中途取消=删除整个Case

4. ✅ **状态管理优化**:
   - 采用数据库持久化方案(CompletedAt字段)
   - 支持暂存恢复功能

5. ✅ **暂存功能澄清**:
   - 待诊列表操作:打开/删除后新建/删除

---

**文档状态**: 📝 待审批（v2.0已修正）
**下一步**: 等待用户确认需求文档,确认后进入Phase 3生成设计文档
**维护者**: Epic #1611工作组
