# 设计文档：诊断字段精简

## 架构决策

### 字段设计

**移除字段**（5个）：

| 字段名 | 中文名 | 移除原因 |
|--------|--------|----------|
| `ChiefComplaint` | 主诉 | 与现病史重叠，信息冗余 |
| `FourDiagnosis` | 四诊 | 舌诊脉诊已独立，剩余内容可合并到现病史 |
| `TreatmentPrinciple` | 治疗原则 | 体现在处方中，无需单独记录 |
| `MedicalAdvice` | 医嘱 | 应移至处方模块 |
| `Remark` | 备注 | 使用率极低 |

**保留字段**（4个核心诊断字段）：

| 字段名 | 中文名 | 类型 | 长度 | 说明 |
|--------|--------|------|------|------|
| `PresentIllness` | 现病史 | string? | 2000 | 病情描述，可包含原主诉内容 |
| `TongueDiagnosis` | 舌诊 | string? | 500 | 舌象描述 |
| `PulseDiagnosis` | 脉诊 | string? | 500 | 脉象描述 |
| `TCMDiagnosis` | 中医诊断 | string? | 500 | 诊断结论（唯一必填字段） |

**设计理由**：
- 4个字段足以完整记录中医诊断过程
- 现病史字段扩大到2000字符，可容纳更多描述
- TCMDiagnosis作为唯一必填字段，确保诊断有结论

### 数据流

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Database   │────>│    Entity    │────>│      DTO        │
│ Consultations│     │ Consultation │     │ ConsultationDto │
│ (4 fields)  │     │ (4 fields)   │     │ (4 fields)      │
└─────────────┘     └──────────────┘     └─────────────────┘
                                                 │
                   ┌─────────────────────────────┼─────────────────────────────┐
                   │                             │                             │
                   v                             v                             v
         ┌─────────────────┐         ┌─────────────────────┐       ┌───────────────────┐
         │ ConsultationItem │         │ConsultationFormVM   │       │ConsultationPanelVM│
         │   (Model)        │         │   (ViewModel)       │       │   (ViewModel)     │
         └─────────────────┘         └─────────────────────┘       └───────────────────┘
                   │                             │                             │
                   v                             v                             v
         ┌─────────────────┐         ┌─────────────────────┐       ┌───────────────────┐
         │ Print DTOs      │         │ConsultationFormView │       │ConsultationPanel  │
         └─────────────────┘         │     (XAML)          │       │MedicalCaseView    │
                                     └─────────────────────┘       └───────────────────┘
```

## 变更清单

### Phase 1: 后端数据层

1. **实体变更** `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`
   - 移除: `ChiefComplaint`, `FourDiagnosis`, `TreatmentPrinciple`, `MedicalAdvice`, `Remark`

2. **DTO变更** `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDtos.cs`
   - `ConsultationDto`: 移除5个字段
   - `ConsultationInputDto`: 移除5个字段

3. **验证器变更** `src/Shared/LYBT.Shared.Validators/`
   - 移除ChiefComplaint必填验证
   - 移除其他4个字段的验证规则

4. **数据库迁移**
   - 创建EF Core迁移删除5个列
   - 可选：创建备份表保留历史数据

5. **服务层映射** `src/Server/Modules/LYBT.Module.MedicalCase/Services/`
   - 更新AutoMapper配置或手动映射

### Phase 2: 客户端数据层

6. **模型变更** `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Models/ConsultationItem.cs`
   - 移除5个字段属性

7. **ViewModel变更**
   - `ConsultationFormViewModel.cs` - 移除字段和属性
   - `ConsultationPanelViewModel.cs` - 移除字段和属性

8. **DataManager变更**
   - `ConsultationDataManager.cs` - 移除字段处理
   - `MedicalCaseDataManager.cs` - 移除字段处理

### Phase 3: 客户端UI层

9. **视图变更**
   - `ConsultationFormView.xaml` - 移除5个输入框，重新布局
   - `ConsultationPanel.xaml` - 移除5个显示区域
   - `MedicalCaseWorkspaceView.xaml` - 移除预览区域

### Phase 4: 打印功能

10. **打印DTO** `PrescriptionPrintDto.cs`
    - 移除5个字段

11. **打印服务**
    - `PrescriptionPrintService.cs` - 移除字段填充
    - `PrescriptionFlowDocumentBuilder.cs` - 移除打印内容
    - `PrescriptionPrintTemplate.xaml.cs` - 移除显示内容

### Phase 5: 测试

12. **单元测试**
    - 更新所有涉及这5个字段的测试用例

## UI设计

### 诊断表单布局（精简后）

```
┌────────────────────────────────────────────────────┐
│ 现病史                                              │
│ ┌────────────────────────────────────────────────┐ │
│ │                                                │ │
│ │ [多行文本框 - 高度较大]                        │ │
│ │                                                │ │
│ └────────────────────────────────────────────────┘ │
├────────────────────────────────────────────────────┤
│ 舌诊                          脉诊                 │
│ ┌──────────────────────┐     ┌──────────────────┐ │
│ │                      │     │                  │ │
│ │ [多行文本框]         │     │ [多行文本框]     │ │
│ │                      │     │                  │ │
│ └──────────────────────┘     └──────────────────┘ │
├────────────────────────────────────────────────────┤
│ 中医诊断 *                                          │
│ ┌────────────────────────────────────────────────┐ │
│ │ [多行文本框 - 必填]                            │ │
│ └────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────┘
```

### 打印格式

```
现病史：[PresentIllness内容]
舌诊：[TongueDiagnosis内容]
脉诊：[PulseDiagnosis内容]
中医诊断：[TCMDiagnosis内容]
```

## 兼容性考虑

### 数据迁移

迁移脚本需要：
1. 可选：备份现有数据到历史表
2. 删除5个列
3. 处理NULL和空字符串

### API版本

本次变更为破坏性变更，需要客户端同步更新。由于是内部系统，无需API版本控制。
