# 医案模块完整设计清单

**Change ID**: optimize-medicalcase-navigation (综合设计)
**版本**: v1.0
**创建时间**: 2025-12-29

---

## 1. 业务流程总览

### 1.1 完整看诊流程

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  患者选择    │ ──▶ │  开始看诊    │ ──▶ │  填写诊断    │ ──▶ │  完成/暂存   │
│PatientSelect│     │ CreateCase  │     │Consultation │     │Complete/Draft│
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                   │                   │                   │
       │                   │                   │                   │
       ▼                   ▼                   ▼                   ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ 待诊队列     │     │ 医案状态     │     │ 处方编辑     │     │ 返回选择    │
│PendingQueue │     │ Active      │     │Prescription │     │ Navigation  │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
```

### 1.2 操作入口

| 入口 | 场景 | 操作 |
|------|------|------|
| 待诊队列双击 | 选择待诊患者 | StartMedicalCase |
| 待诊队列回车 | 选择待诊患者 | StartMedicalCase |
| "开始看诊"按钮 | 选择待诊患者 | StartMedicalCase |
| 患者列表双击 | 选择已有患者 | NavigateToWorkspace |
| Management列表 | 查看历史医案 | ViewMedicalCase (ReadOnly) |

---

## 2. 医案状态机

### 2.1 MedicalCaseStatus (后端)

```
         ┌─────────────────────────────────────────────┐
         │                                             │
         ▼                                             │
┌─────────────┐      保存      ┌─────────────┐        │
│   Draft     │ ────────────▶ │  Completed  │        │
│   (草稿)    │               │  (已完成)    │        │
└─────────────┘               └─────────────┘        │
    │   ▲                           │                │
    │   │                           │ 修改后保存     │
    │   │ 暂存                      ▼                │
    │   │                     ┌─────────────┐        │
    │   └──────────────────── │  Audited    │ ───────┘
    │                         │ (已审核修改) │
    │                         └─────────────┘
    │
    │ 取消
    ▼
┌─────────────┐
│  Cancelled  │
│  (已取消)    │
└─────────────┘
```

### 2.2 PendingCaseType (前端显示)

| 后端状态 | 有Consultation? | 前端显示 | 颜色 |
|----------|-----------------|----------|------|
| Active | - | InProgress (看诊中) | 绿色 #4CAF50 |
| Draft | 有 | Suspended (挂起) | 橙色 #FF9800 |
| Draft | 无 | Waiting (等待) | 灰色 #E0E0E0 |

### 2.3 EditState (编辑状态)

```csharp
public enum EditState
{
    ReadOnly = 0,    // 只读模式 - 不可编辑
    Editing = 1,     // 编辑模式 - 可以修改
    Saving = 2       // 保存中 - 正在保存
}
```

---

## 3. 工作模式

### 3.1 WorkspaceMode

| 模式 | 来源 | 行为 |
|------|------|------|
| Clinical | ClinicalHomeView | 看诊模式，返回PatientSelectionView |
| Management | 管理列表 | 查看/编辑模式，返回列表 |

### 3.2 模式与编辑状态组合

| WorkspaceMode | EditState | 场景 | 返回行为 |
|---------------|-----------|------|----------|
| Clinical | Editing | 正在看诊 | 显示确认弹窗(暂存/取消/继续) |
| Clinical | ReadOnly | 查看已暂存 | 直接返回，无弹窗 |
| Management | ReadOnly | 查看历史 | 直接返回列表 |
| Management | Editing | 修改历史 | 显示确认弹窗(保存/放弃) |

---

## 4. 诊断与处方设计

### 4.1 诊断字段 (ConsultationPanelViewModel)

| 字段 | 属性名 | 必填 | 说明 |
|------|--------|------|------|
| 现病史 | PresentIllness | 否 | 病情描述 |
| 舌诊 | TongueDiagnosis | 否 | 舌象描述 |
| 脉诊 | PulseDiagnosis | 否 | 脉象描述 |
| 中医诊断 | TCMDiagnosis | **是** | 完成必填 |
| 需要处方 | NeedsPrescription | - | 控制处方验证 |

### 4.2 NeedsPrescription 设计

**默认值**: `false` (默认不开处方)

**UI表现**:
- RadioButton: "开处方" / "不开处方"
- 不开处方时，处方区折叠但保留数据

**数据流**:
```
NeedsPrescription 变化
    │
    ├─▶ UI: 处方区 IsExpanded 绑定
    │
    ├─▶ 验证: UpdateCanComplete() 重新计算
    │
    └─▶ 保存: SaveAsync() 根据值决定处理方式
```

### 4.3 处方字段 (PrescriptionPanelViewModel)

| 字段 | 属性名 | 必填 | 说明 |
|------|--------|------|------|
| 药材列表 | HerbItems | 条件 | NeedsPrescription=true时必填 |
| 每项药材 | HerbId, HerbName | 是 | 药材标识和名称 |
| 剂量 | Dosage | 是 | 用量数值 |
| 单位 | **Unit** | **是** | 必须从药材库同步 |
| 备注 | Remark | 否 | 特殊说明 |

---

## 5. 按钮可用性逻辑

### 5.1 完成按钮 (CanComplete)

```
诊断必填(TCMDiagnosis)有值?
├── 否 ──▶ 不可用
└── 是 ──▶ NeedsPrescription?
           ├── false ──▶ 可用
           └── true ──▶ 药材数量 > 0?
                        ├── 是 ──▶ 可用
                        └── 否 ──▶ 不可用
```

### 5.2 保存按钮 (ShowSaveButton)

- 编辑模式下始终显示
- CanExecute: 有未保存变更

### 5.3 暂存按钮 (ShowDraftButton)

- Clinical模式下显示
- 编辑模式下可用
- 不验证完整性，保存当前状态

### 5.4 打印处方 (CanPrintPrescription)

- 处方已保存 (有PrescriptionId)
- 药材数量 > 0

---

## 6. 保存逻辑设计

### 6.1 保存 (ExecuteSave / CompleteMedicalCase)

**触发**: 点击"完成医案"

**验证规则**:
1. TCMDiagnosis 必填
2. 如果 NeedsPrescription = true:
   - HerbItems.Count > 0
   - 每个Item的Unit不能为空

**数据处理**:
```csharp
if (NeedsPrescription)
{
    // 开处方: 验证并保存处方
    ValidatePrescription();
    SavePrescriptionItems();
}
else
{
    // 不开处方: 清空处方数据
    ClearPrescriptionItems();
}
SaveMedicalCase(Status = Completed);
```

### 6.2 暂存 (ExecuteSaveDraft)

**触发**: 点击"暂存医案"

**验证规则**: 无 (允许任何状态暂存)

**数据处理**:
```csharp
// 保存当前UI状态，不清空任何数据
SaveConsultation();
SavePrescriptionItems(); // 包括NeedsPrescription值
SaveMedicalCase(Status = Draft);

// 切换为只读模式
EnterReadOnlyMode();
```

**后置行为**:
- 切换为只读模式 (EditState.ReadOnly)
- 此时返回无需确认弹窗

### 6.3 保存变更 (ExecuteSaveChanges)

**触发**: Management模式下修改后保存

**验证规则**: 同 ExecuteSave

**数据处理**: 同 ExecuteSave，但可能需要审核原因

---

## 7. 导航确认逻辑

### 7.1 返回按钮行为

```
点击返回
    │
    ├─▶ Management模式?
    │       └── 是 ──▶ 有未保存变更?
    │                   ├── 是 ──▶ 确认弹窗(保存/放弃)
    │                   └── 否 ──▶ 直接返回列表
    │
    └─▶ Clinical模式
            └── IsReadOnly?
                    ├── 是 ──▶ 直接返回PatientSelection
                    └── 否 ──▶ 三选项弹窗
                                ├── 是(暂存) ──▶ SaveDraft + Navigate
                                ├── 否(取消) ──▶ CancelCase + Navigate
                                └── 取消 ──▶ 留在当前界面
```

### 7.2 待诊切换行为

```
双击待诊列表项
    │
    ├─▶ 当前有医案 (MedicalCaseId != Empty)?
    │       ├── 否 ──▶ 直接切换到新患者
    │       └── 是 ──▶ 有未保存变更?
    │                   ├── 是 ──▶ 确认弹窗(暂存/取消/继续)
    │                   └── 否 ──▶ 直接切换到新患者
    │
    └─▶ 加载新医案
```

---

## 8. Unit同步设计

### 8.1 问题根因

药材添加到处方时，Unit字段未从源数据同步。

### 8.2 数据来源与同步点

| 来源 | DTO | 同步位置 | Unit字段 |
|------|-----|----------|----------|
| 药材库搜索 | HerbListDto | CreateHerbItemFromDto | dto.Unit |
| 方剂导入 | FormulaItemListDto | ProcessFormulaImport | item.Unit |
| 历史复制 | PrescriptionItemListDto | ProcessHistoryCopy | item.Unit |

### 8.3 修复方案

**HerbItemToAdd** 添加 Unit 属性:
```csharp
private class HerbItemToAdd
{
    public required Guid HerbId { get; init; }
    public required string HerbName { get; init; }
    public decimal Dosage { get; init; }
    public string? Remark { get; init; }
    public string? Unit { get; init; }  // 新增
}
```

**各导入方法** 设置 Unit:
```csharp
// ProcessFormulaImport
Unit = fi.Unit

// ProcessHistoryCopy
Unit = item.Unit

// CreateHerbItemFromDto
Unit = dto.Unit
```

---

## 9. 边界条件清单

### 9.1 空状态处理

| 场景 | 处理 |
|------|------|
| 待诊队列为空 | 显示空状态提示 |
| 无当前医案时切换 | 跳过SaveDraft，直接导航 |
| 药材列表为空时保存 | 根据NeedsPrescription判断 |

### 9.2 并发与状态

| 场景 | 处理 |
|------|------|
| 保存中再次点击 | 按钮禁用 (IsBusy) |
| 导航中再次导航 | 忽略重复请求 |
| 数据加载失败 | 显示错误，允许重试 |

### 9.3 数据一致性

| 场景 | 处理 |
|------|------|
| 处方保存失败 | 回滚，显示错误 |
| 部分数据验证失败 | 定位到错误字段 |
| 网络断开 | 提示重试 |

---

## 10. 实现优先级

### Phase 1: 紧急修复 (当前问题)
- [ ] Unit同步修复 (5处修改)
- [ ] NeedsPrescription验证逻辑

### Phase 2: 逻辑完善
- [ ] UpdateCanComplete完善
- [ ] 处方切换逻辑

### Phase 3: 体验优化
- [ ] 导航确认行为统一
- [ ] 错误提示改进

---

## 11. 文件修改清单

| 文件 | 修改内容 | Phase |
|------|----------|-------|
| PrescriptionImportHandler.cs | HerbItemToAdd.Unit + 3处赋值 | 1 |
| PrescriptionItemHandler.cs | CreateHerbItemFromDto.Unit | 1 |
| MedicalCaseWorkspaceCoordinator.cs | SaveAsync NeedsPrescription处理 | 1 |
| MedicalCaseWorkspaceViewModel.cs | UpdateCanComplete | 2 |
| MedicalCaseNavigationHandler.cs | ReadOnly直接返回 | 2 |

---

## 12. 验证检查清单

### 12.1 功能验证

- [ ] 从药材库添加药材，Unit有值
- [ ] 从方剂导入药材，Unit有值
- [ ] 从历史复制药材，Unit有值
- [ ] 不开处方时保存成功
- [ ] 不开处方时完成按钮可用(仅需诊断)
- [ ] 开处方无药材时完成按钮不可用
- [ ] 暂存后切换为只读模式
- [ ] 只读模式返回无弹窗

### 12.2 边界验证

- [ ] 两个挂起患者时双击切换正常
- [ ] 无当前医案时切换正常
- [ ] 空待诊队列显示正常
- [ ] 保存中按钮禁用

---

**文档版本**: v1.0
**最后更新**: 2025-12-29
