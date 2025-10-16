# MVP "能看诊" 功能需求确认报告

**文档版本**：v1.0
**创建日期**：2025-10-16
**确认方式**：逐项确认（每次一个问题）
**目标**：明确 MVP 阶段"能看诊"核心功能的实现范围与优先级

---

## 📋 一、需求确认汇总

### 1.1 核心确认事项（8项）

| 序号 | 确认事项 | 结论 | 影响范围 |
|------|---------|------|---------|
| 1 | 验方模块 | ✅ 保留，延迟绑定设计 | 数据模型、导入逻辑、验证UI |
| 2 | 处方录入方式 | ✅ 四种方式（详见专项报告） | 表格编辑、验方导入、历史复制、快速输入预留 |
| 3 | 处方打印格式 | ✅ 标准中药处方笺格式 | 打印模板、布局设计 |
| 4 | 患者/药材导入 | ✅ UI实现，持续功能 | 导入向导保留 |
| 5 | 诊疗记录搜索 | ✅ 需要关键词搜索 | 搜索功能保留 |
| 6 | 处方编号生成 | ✅ 自动生成流水号 | 编号生成逻辑保留 |
| 7 | 医案业务规则 | ✅ 一病案一诊断，当天可改隔日锁定 | 业务逻辑、权限控制 |
| 8 | 处方状态管理 | ✅ 简单标记"是否已打印" | 状态字段简化 |

### 1.2 已生成专项报告

- 📄 `docs/reports/formula-feature-requirements-and-design-2025-10-16.md` - 验方模块详细设计（15个任务，14-20小时）
- 📄 `docs/reports/prescription-entry-requirements-2025-10-16.md` - 处方录入详细设计（19个任务，24-27小时）

---

## 🎯 二、详细需求说明

### 2.1 验方模块（Formula Module）

#### 业务需求
- **核心价值**：验方是中医宝贵资源，必须保留
- **关键问题**：老系统验方药材名称与新系统不完全匹配（例如："枣" vs "红枣"）
- **解决方案**：延迟绑定设计
  - 导入时允许不匹配（保存原始名称）
  - 医生验证时手动映射（"枣" → "红枣"）
  - 未验证的验方不能导入到处方

#### 技术设计要点
```csharp
// 数据模型调整
public class FormulaHerbItemDto : BaseDto
{
    public Guid? HerbId { get; set; }              // ✅ 改为可空
    public string HerbName { get; set; }
    public string? OriginalHerbName { get; set; }  // ✅ 新增：保存原始名称
    public bool IsValidated { get; set; }          // ✅ 新增：验证状态
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
}

public enum FormulaValidationStatus
{
    Draft = 0,      // 草稿 - 未校验
    Validated = 1   // 已校验 - 可用于处方
}
```

#### 实现任务（15个，14-20小时）
- **Phase 1**：数据模型调整（FORMULA-1至FORMULA-3，2-3小时）
- **Phase 2**：Server端实现（FORMULA-4至FORMULA-8，4-6小时）
- **Phase 3**：Client端UI（FORMULA-9至FORMULA-12，6-8小时）
- **Phase 4**：测试与文档（FORMULA-13至FORMULA-15，2-3小时）

---

### 2.2 处方录入（Prescription Entry）

#### 四种录入方式

**方式1：表格智能编辑**（优先级：⭐⭐⭐）
- 固定8列布局（每行4种药材：药材1-用量1-药材2-用量2-药材3-用量3-药材4-用量4）
- 智能补全：ComboBox支持拼音码（例如输入"dg"匹配"当归"）
- 焦点跳转：Enter确认后自动跳转到下一个单元格
- Tab切换：在补全候选项之间切换

**方式2：验方导入**（优先级：⭐⭐⭐）
- 选择已验证的验方 → 批量导入所有药材
- 记录引用的验方名称（Prescription.ReferencedFormulas字段）
- 导入后可继续编辑、删除、添加药材
- 可导入多个验方组合

**方式3：历史处方复制**（优先级：⭐⭐⭐）
- **模式A**：当前患者历史处方（默认显示最近5条）
- **模式B**：全局处方查询（按患者姓名 OR 症状/诊断）
- 处方自动包含对应病案的诊断信息（从Consultation表查询，不冗余存储）
- 复制后可编辑修改

**方式4：快速文本输入**（优先级：⭐ - MVP预留）
- MVP阶段：仅预留UI空间（禁用状态）
- Post-MVP：实现文本解析（例如："当归10 白芍15"）
- 输入逻辑：药名（回车）→ 数字（回车）→ 药名（回车）→ ...

#### 实现任务（19个，24-27小时）
- **Phase 1**：表格智能编辑（ENTRY-1至ENTRY-6，8小时）
- **Phase 2**：验方导入（ENTRY-7至ENTRY-11，5小时）
- **Phase 3**：历史复制（ENTRY-12至ENTRY-18，11小时）
- **Phase 4**：快速输入占位（ENTRY-19，0.5小时）

---

### 2.3 处方打印（Prescription Printing）

#### 格式要求
- ✅ 标准中药处方笺格式（已提供样例图片）
- ✅ 包含以下信息：
  - **顶部**：条形码、机构名称（"123 处方笺"）
  - **患者信息**：费别、医保证号、处方编号、姓名、性别、年龄、门诊/住院病房号、科室/病区/床位号、临床诊断、开具日期、地址/电话
  - **处方内容**：Rp. 标记、药材列表（名称+用量）、配伍说明（"配付 每日一剂 水煎服 每日两次"）
  - **底部**：医师签名、药品金额、审核药师、调配药师/士、核对/发药药师

#### 技术实现
- 现有代码：`GeneratePrintFormatAsync` 和 `GenerateSimplePrintFormat`
- 需求：重新实现为标准格式
- 输出：可打印的格式化文本或PDF（待确认）

#### 实现任务（新增）
- **PRINT-1**：分析现有打印方法，确定输出格式（文本 vs PDF）
- **PRINT-2**：实现标准处方笺模板（参照样例图片）
- **PRINT-3**：实现打印布局逻辑（药材列表排版、金额计算）
- **PRINT-4**：集成打印功能到处方详情页
- **PRINT-5**：测试打印功能

**工作量估算**：6-8小时

---

### 2.4 患者/药材数据导入（Data Import）

#### 需求确认
- ✅ 需要UI实现（不是一次性脚本）
- ✅ 持续功能（管理员经常需要导入数据）
- ✅ 保留现有的导入向导UI（PatientImportWizardViewModel）

#### 功能范围
- **患者导入**：从老系统Excel导入患者基础信息
- **药材导入**：导入药材字典数据（支持定期更新）
- **验方导入**：从老系统导入验方数据（详见2.1验方模块）

#### 现有代码
- `PatientImportWizardViewModel.cs` - 患者导入向导（已实现）
- 需补充：药材导入UI和逻辑

#### 实现任务（新增）
- **IMPORT-1**：完善患者导入向导逻辑
- **IMPORT-2**：实现药材导入UI（HerbImportWizardViewModel）
- **IMPORT-3**：实现药材导入Excel解析逻辑
- **IMPORT-4**：测试导入功能

**工作量估算**：4-6小时（患者已有，主要补充药材）

---

### 2.5 诊疗记录搜索（Consultation Search）

#### 需求确认
- ✅ 需要关键词搜索功能
- ✅ 可以按症状、诊断、治则等关键词搜索历史诊疗记录
- ✅ 方便医生查找类似病例参考

#### 功能范围
- 按患者ID查询（基础功能）
- 按关键词搜索（症状、诊断、治则、主诉等）
- 跨患者搜索（全局病例库）

#### 现有代码
- `ConsultationService.SearchAsync` (216-232行) - 已实现

#### 实现任务
- **SEARCH-1**：保留现有搜索方法
- **SEARCH-2**：验证搜索功能是否满足需求
- **SEARCH-3**：集成到UI（诊疗记录管理页面）

**工作量估算**：2-3小时（主要是验证和UI集成）

---

### 2.6 处方编号生成（Prescription Number）

#### 需求确认
- ✅ 自动生成流水号
- ✅ 格式：P + 日期 + 流水号（例如：P20250116001）

#### 功能范围
- 自动生成唯一处方编号
- 日期格式：YYYYMMDD
- 流水号：每日重置（001开始）

#### 现有代码
- `PrescriptionService.GeneratePrescriptionNoAsync` (309-334行) - 已实现

#### 实现任务
- **NUMBER-1**：保留现有编号生成逻辑
- **NUMBER-2**：验证编号唯一性和并发安全
- **NUMBER-3**：集成到处方创建流程

**工作量估算**：1-2小时（主要是验证）

---

### 2.7 医案业务规则（MedicalCase Business Rules）

#### 核心规则
- ✅ **一病案一诊断**：一个医案（MedicalCase）只能有一次诊疗记录（Consultation）
- ✅ **一诊断一处方**：一次诊疗记录只能有一个处方（Prescription）
- ✅ **当天可改隔日锁定**：病案/诊疗/处方在创建当天可修改，隔日起锁定不可修改
- ✅ **复诊新建病案**：患者复诊需要创建新的病案

#### 技术实现
```csharp
// 数据模型关系
MedicalCase (1) ←→ (1) Consultation (1) ←→ (1) Prescription

// 业务逻辑
- 创建诊疗记录时检查：一个病案是否已有诊疗记录
- 创建处方时检查：一个诊疗记录是否已有处方
- 修改时检查：是否为创建当天（CreatedAt.Date == DateTime.Today）
```

#### 实现任务（新增）
- **RULE-1**：实现一病案一诊断约束（数据验证）
- **RULE-2**：实现一诊断一处方约束（数据验证）
- **RULE-3**：实现当天可改隔日锁定逻辑（权限控制）
- **RULE-4**：UI提示：隔日后显示"只读"模式
- **RULE-5**：测试业务规则约束

**工作量估算**：4-6小时

---

### 2.8 处方状态管理（Prescription Status）

#### 需求确认
- ✅ 简单标记方式：只需要"是否已打印"标记
- ❌ 不需要复杂的状态流转（草稿 → 已确认 → 已配药 → 已完成）

#### 技术实现
```csharp
// 数据模型
public class PrescriptionDto : BaseDto
{
    public bool IsPrinted { get; set; }  // 是否已打印
    public DateTime? PrintedAt { get; set; }  // 打印时间
    // ... 其他字段
}
```

#### 实现任务（新增）
- **STATUS-1**：添加 IsPrinted 和 PrintedAt 字段
- **STATUS-2**：打印时自动更新状态
- **STATUS-3**：UI显示打印状态标记

**工作量估算**：1-2小时

---

## 📊 三、开发任务汇总

### 3.1 任务分类统计

| 分类 | 任务数 | 工作量（小时） | 优先级 |
|------|-------|--------------|--------|
| 验方模块 | 15 | 14-20 | ⭐⭐⭐ |
| 处方录入 | 19 | 24-27 | ⭐⭐⭐ |
| 处方打印 | 5 | 6-8 | ⭐⭐⭐ |
| 数据导入 | 4 | 4-6 | ⭐⭐⭐ |
| 诊疗搜索 | 3 | 2-3 | ⭐⭐ |
| 处方编号 | 3 | 1-2 | ⭐⭐ |
| 业务规则 | 5 | 4-6 | ⭐⭐⭐ |
| 状态管理 | 3 | 1-2 | ⭐⭐ |
| **总计** | **57** | **56-74** | - |

### 3.2 完整任务清单

#### Phase 1: 验方模块（15个任务，14-20小时）
参见：`docs/reports/formula-feature-requirements-and-design-2025-10-16.md`

- [FORMULA-1] 修改 FormulaHerbItemDto 数据模型
- [FORMULA-2] 添加 FormulaValidationStatus 枚举
- [FORMULA-3] 调整数据库架构
- [FORMULA-4] 重写 ImportFromExcelAsync（主-从表格式）
- [FORMULA-5] 实现 ValidateFormulaHerbAsync
- [FORMULA-6] 实现 GetPendingValidationFormulasAsync
- [FORMULA-7] 修改 ImportFormulaIntoPrescriptionAsync（验证检查）
- [FORMULA-8] 实现 HerbRepository.GetByNameOrPinyinAsync
- [FORMULA-9] 创建 FormulaValidationViewModel
- [FORMULA-10] 创建 FormulaValidationView.xaml
- [FORMULA-11] 修改 FormulaTemplateDialogViewModel
- [FORMULA-12] 修改 FormulaTemplateDialog.xaml
- [FORMULA-13] 单元测试
- [FORMULA-14] 集成测试
- [FORMULA-15] 用户文档

#### Phase 2: 处方录入（19个任务，24-27小时）
参见：`docs/reports/prescription-entry-requirements-2025-10-16.md`

**2.1 表格智能编辑（6个任务，8小时）**
- [ENTRY-1] 创建 PrescriptionItemRow 模型
- [ENTRY-2] 实现 Items → ItemRows 转换逻辑
- [ENTRY-3] 设计8列DataGrid XAML布局
- [ENTRY-4] 实现ComboBox拼音码过滤
- [ENTRY-5] 实现焦点自动跳转逻辑
- [ENTRY-6] 测试完整录入工作流

**2.2 验方导入（5个任务，5小时）**
- [ENTRY-7] 添加 Prescription.ReferencedFormulas 字段
- [ENTRY-8] 实现 ImportFormulaAsync 方法
- [ENTRY-9] 调整 FormulaTemplateDialogViewModel
- [ENTRY-10] 集成导入命令到处方编辑页
- [ENTRY-11] 测试验方导入工作流

**2.3 历史处方复制（6个任务，11小时）**
- [ENTRY-12] 创建 PrescriptionSearchResultDto
- [ENTRY-13] 实现 GetPatientRecentPrescriptionsAsync
- [ENTRY-14] 实现 SearchPrescriptionsAsync
- [ENTRY-15] 调整 ClonePrescriptionAsync
- [ENTRY-16] 集成患者历史处方下拉框
- [ENTRY-17] 创建 PrescriptionSearchDialog
- [ENTRY-18] 测试历史和搜索工作流

**2.4 快速输入占位（1个任务，0.5小时）**
- [ENTRY-19] 预留UI空间（禁用状态）

#### Phase 3: 处方打印（5个任务，6-8小时）
- [PRINT-1] 分析现有打印方法，确定输出格式
- [PRINT-2] 实现标准处方笺模板
- [PRINT-3] 实现打印布局逻辑
- [PRINT-4] 集成打印功能到处方详情页
- [PRINT-5] 测试打印功能

#### Phase 4: 数据导入（4个任务，4-6小时）
- [IMPORT-1] 完善患者导入向导逻辑
- [IMPORT-2] 实现药材导入UI
- [IMPORT-3] 实现药材导入Excel解析逻辑
- [IMPORT-4] 测试导入功能

#### Phase 5: 诊疗搜索（3个任务，2-3小时）
- [SEARCH-1] 保留现有搜索方法
- [SEARCH-2] 验证搜索功能是否满足需求
- [SEARCH-3] 集成到UI

#### Phase 6: 处方编号（3个任务，1-2小时）
- [NUMBER-1] 保留现有编号生成逻辑
- [NUMBER-2] 验证编号唯一性和并发安全
- [NUMBER-3] 集成到处方创建流程

#### Phase 7: 业务规则（5个任务，4-6小时）
- [RULE-1] 实现一病案一诊断约束
- [RULE-2] 实现一诊断一处方约束
- [RULE-3] 实现当天可改隔日锁定逻辑
- [RULE-4] UI提示：隔日后显示"只读"模式
- [RULE-5] 测试业务规则约束

#### Phase 8: 状态管理（3个任务，1-2小时）
- [STATUS-1] 添加 IsPrinted 和 PrintedAt 字段
- [STATUS-2] 打印时自动更新状态
- [STATUS-3] UI显示打印状态标记

---

## 🎯 四、实施建议

### 4.1 开发优先级

**第一优先级（MVP核心）**：
1. 验方模块（FORMULA-1至FORMULA-15）
2. 处方录入-表格编辑（ENTRY-1至ENTRY-6）
3. 处方录入-验方导入（ENTRY-7至ENTRY-11）
4. 处方录入-历史复制（ENTRY-12至ENTRY-18）
5. 处方打印（PRINT-1至PRINT-5）
6. 业务规则（RULE-1至RULE-5）

**第二优先级（MVP支撑）**：
7. 数据导入（IMPORT-1至IMPORT-4）
8. 处方编号（NUMBER-1至NUMBER-3）
9. 状态管理（STATUS-1至STATUS-3）

**第三优先级（MVP可选）**：
10. 诊疗搜索（SEARCH-1至SEARCH-3）
11. 快速输入占位（ENTRY-19）

### 4.2 实施阶段划分

**Week 1-2：验方模块 + 处方录入核心**
- 验方模块完整实现（15个任务）
- 处方录入-表格编辑（6个任务）
- 处方录入-验方导入（5个任务）

**Week 3：处方录入历史复制 + 业务规则**
- 处方录入-历史复制（6个任务）
- 业务规则约束（5个任务）

**Week 4：打印 + 导入 + 其他**
- 处方打印（5个任务）
- 数据导入（4个任务）
- 处方编号（3个任务）
- 状态管理（3个任务）
- 诊疗搜索（3个任务）

### 4.3 风险提示

1. **处方打印格式**：需要确认输出格式（文本 vs PDF），可能需要引入第三方库
2. **验方验证UI**：复杂度较高，药材匹配逻辑需要充分测试
3. **历史处方复制**：涉及多表查询，性能需要关注
4. **业务规则约束**：需要在Server和Client两端同时实现

---

## 📚 五、附录

### 5.1 相关文档

- 📄 `docs/reports/formula-feature-requirements-and-design-2025-10-16.md` - 验方模块详细设计
- 📄 `docs/reports/prescription-entry-requirements-2025-10-16.md` - 处方录入详细设计
- 📄 `docs/tasks/mvp-task-checklist-2025-10-16.md` - MVP任务清单（待更新）

### 5.2 数据模型变更摘要

```csharp
// 1. FormulaHerbItemDto - 验方药材项
public class FormulaHerbItemDto : BaseDto
{
    public Guid? HerbId { get; set; }              // 改为可空
    public string? OriginalHerbName { get; set; }  // 新增
    public bool IsValidated { get; set; }          // 新增
    // ... 其他字段
}

// 2. FormulaDto - 验方
public class FormulaDto : BaseDto
{
    public FormulaValidationStatus ValidationStatus { get; set; }  // 新增
    // ... 其他字段
}

// 3. PrescriptionDto - 处方
public class PrescriptionDto : BaseDto
{
    public string? ReferencedFormulas { get; set; }  // 新增：引用的验方名称
    public bool IsPrinted { get; set; }              // 新增：是否已打印
    public DateTime? PrintedAt { get; set; }         // 新增：打印时间
    // ... 其他字段
}

// 4. PrescriptionItemRow - 处方显示模型（Client端）
public class PrescriptionItemRow
{
    public PrescriptionItemViewModel? Item1 { get; set; }
    public PrescriptionItemViewModel? Item2 { get; set; }
    public PrescriptionItemViewModel? Item3 { get; set; }
    public PrescriptionItemViewModel? Item4 { get; set; }
}

// 5. PrescriptionSearchResultDto - 处方搜索结果
public class PrescriptionSearchResultDto
{
    public Guid PrescriptionId { get; set; }
    public string PatientName { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string? TCMDiagnosis { get; set; }  // 从Consultation查询
    public int HerbCount { get; set; }
    public List<PrescriptionItemDto> Items { get; set; }
}
```

### 5.3 数据库索引建议

```sql
-- 1. 处方查询优化
CREATE INDEX IX_Prescriptions_CreatedAt ON Prescriptions(CreatedAt DESC);
CREATE INDEX IX_Prescriptions_PatientId_CreatedAt ON Prescriptions(PatientId, CreatedAt DESC);

-- 2. 诊疗记录搜索优化
CREATE INDEX IX_Consultations_TCMDiagnosis ON Consultations(TCMDiagnosis);

-- 3. 药材拼音码查询优化
CREATE INDEX IX_Herbs_PinyinCode ON Herbs(PinyinCode);
CREATE INDEX IX_Herbs_Name ON Herbs(Name);

-- 4. 验方验证状态查询优化
CREATE INDEX IX_Formulas_ValidationStatus ON Formulas(ValidationStatus);
```

---

## ✅ 六、总结

本次需求确认采用"每次一个问题"的方式，逐项确认了MVP "能看诊"功能的8个关键事项：

1. ✅ **验方模块**：保留，延迟绑定设计（15个任务，14-20小时）
2. ✅ **处方录入**：四种方式（19个任务，24-27小时）
3. ✅ **处方打印**：标准处方笺格式（5个任务，6-8小时）
4. ✅ **数据导入**：UI实现，持续功能（4个任务，4-6小时）
5. ✅ **诊疗搜索**：关键词搜索（3个任务，2-3小时）
6. ✅ **处方编号**：自动生成流水号（3个任务，1-2小时）
7. ✅ **业务规则**：一病案一诊断，当天可改隔日锁定（5个任务，4-6小时）
8. ✅ **状态管理**：简单标记"是否已打印"（3个任务，1-2小时）

**总开发任务**：57个任务，总工作量：56-74小时（约2-3周）

**下一步建议**：
1. 基于本报告创建GitHub Issues（Epic + 子任务）
2. 更新 `docs/tasks/mvp-task-checklist-2025-10-16.md`
3. 按优先级启动开发（建议从验方模块开始）
