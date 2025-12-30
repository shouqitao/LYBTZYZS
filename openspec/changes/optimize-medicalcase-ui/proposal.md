# Proposal: 医案界面UI优化

**Change ID**: `optimize-medicalcase-ui`
**Type**: UI Optimization
**Priority**: P1
**Status**: Applied
**Author**: Claude Code
**Created**: 2025-12-28
**Applied**: 2025-12-30
**Target Version**: v1.1.0

---

## 1. Executive Summary

### 1.1 问题陈述

基于`refactor-medicalcase-workspace`完成后的用户反馈，医案编辑界面存在以下问题：

| 问题 | 当前状态 | 影响 | 严重程度 |
|------|----------|------|----------|
| 患者信息重复显示 | 左侧PatientInfoCardControl + 右上角Header同时显示 | 视觉冗余，浪费空间 | 中 |
| 诊断区布局有滚动条 | 4个字段各自带滚动条，MinHeight固定 | 不符合"一屏显示"最佳实践 | 中 |
| 待诊队列状态不清晰 | Type字段硬编码为"暂存" | 无法区分等待/看诊中/挂起 | 高 |

### 1.2 提案目标

1. **消除患者信息重复**: 移除Header中的患者信息，仅保留左侧PatientInfoCardControl
2. **优化诊断区布局**: 采用2x2网格无滚动条布局，自适应填充空间
3. **实现待诊队列三种状态**: 等待(Waiting)、看诊中(InProgress)、挂起(Suspended)

### 1.3 设计原则（来自EMR/EHR最佳实践）

| 原则 | 说明 |
|------|------|
| 一屏一患者 | 单屏显示患者所有必要信息 |
| 简洁清晰 | 减少视觉噪音，使用简单布局 |
| 无滚动优先 | 紧凑布局，自适应高度 |
| 状态可视化 | 用颜色区分不同状态 |

### 1.4 预期收益

| 收益 | 量化指标 |
|------|----------|
| 界面简洁 | 移除重复信息，视觉噪音减少 |
| 无滚动体验 | 诊断区4个字段一目了然 |
| 状态清晰 | 3种状态颜色区分，一眼识别 |

---

## 2. Scope

### 2.1 In Scope

- Phase 1: 移除Header患者信息重复
- Phase 2: 诊断区2x2无滚动条布局重构
- Phase 3: 待诊队列三种状态实现（枚举/DTO/Repository/UI）

### 2.2 Out of Scope

- 后端API变更（待诊队列状态由客户端计算）
- 新增诊断字段（当前4字段已完整）
- 待诊队列排序/筛选功能

---

## 3. Technical Approach

### 3.1 Phase 1: 移除Header患者信息

**修改文件**: `MedicalCaseWorkspaceView.xaml`

删除`BaseDetailContainer.ActionButtons`中的患者信息StackPanel，患者信息仅在左侧PatientInfoCardControl显示。

### 3.2 Phase 2: 诊断区2x2无滚动条布局

**修改文件**: `ConsultationPanel.xaml`

当前布局问题：
- 现病史 MinHeight="80" + VerticalScrollBarVisibility="Auto"
- 舌诊+脉诊 Height="100"固定 + VerticalScrollBarVisibility="Auto"
- 中医诊断 MinHeight="80" + VerticalScrollBarVisibility="Auto"

新布局方案：
```
+------------------------+------------------------+
|      现病史            |      中医诊断*          |
|   (Height="*")        |    (Height="*")        |
+------------------------+------------------------+
|      舌诊              |       脉诊             |
|   (Height="Auto")     |    (Height="Auto")     |
+------------------------+------------------------+
|         处方选项（是否开处方）                   |
+------------------------------------------------+
```

关键修改：
1. 2x2网格：现病史|中医诊断（上）、舌诊|脉诊（下）
2. 上行`Height="*"`自适应填充
3. 下行`Height="Auto"`根据内容调整
4. 移除所有VerticalScrollBarVisibility
5. 保留TextWrapping="Wrap"和AcceptsReturn="True"

### 3.3 Phase 3: 待诊队列三种状态

#### 3.3.1 新增PendingCaseType枚举

```csharp
public enum PendingCaseType
{
    Waiting = 0,     // 等待（已挂号，未开始看诊）
    InProgress = 1,  // 看诊中（正在进行诊疗）
    Suspended = 2    // 挂起（暂停看诊，稍后继续）
}
```

#### 3.3.2 状态判定逻辑

| MedicalCaseStatus | 有Consultation记录 | PendingCaseType |
|-------------------|-------------------|-----------------|
| Draft | 否 | Waiting（等待） |
| Draft | 是 | Suspended（挂起） |
| Active | - | InProgress（看诊中） |

#### 3.3.3 UI视觉区分

| 状态 | 颜色 | 显示文本 |
|------|------|----------|
| Waiting | 灰色 | 等待 |
| InProgress | 绿色 | 看诊中 |
| Suspended | 橙色 | 挂起 |

---

## 4. Files to Modify

| 文件 | 修改类型 | Phase |
|------|----------|-------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml` | 删除ActionButtons | 1 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml` | 重构2x2网格布局 | 2 |
| `src/Shared/LYBT.Shared.Models/Enums/PendingCaseType.cs` | 新建 | 3 |
| `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/PendingMedicalCaseDto.cs` | 修改Type属性 | 3 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs` | 修改查询逻辑 | 3 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml` | 添加状态颜色 | 3 |

---

## 5. Validation Criteria

- [ ] 患者信息仅在左侧PatientInfoCardControl显示，Header无重复
- [ ] 4个诊断字段采用2x2网格布局，无滚动条，自适应高度
- [ ] 待诊队列显示三种状态并有颜色区分
- [ ] 编译通过，无运行时错误
- [ ] 现有功能不受影响（看诊流程、处方编辑等）

---

## 6. Dependencies

- 依赖 `refactor-medicalcase-workspace` 提案完成（当前39/41）
- 无后端API依赖

---

## 7. Risks and Mitigations

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 布局在不同分辨率下显示异常 | 低 | 中 | 使用Star尺寸自适应 |
| 待诊状态判定逻辑复杂 | 低 | 低 | 明确状态判定规则 |

---

## 8. Implementation Record (2025-12-30)

### 8.1 Phase 1: 移除Header患者信息重复

**状态**: 已完成

**修改文件**:
- `MedicalCaseWorkspaceView.xaml` - 移除BaseDetailContainer.ActionButtons中的患者信息，保留左侧PatientInfoCardControl

### 8.2 Phase 2: 诊断区2x2无滚动条布局

**状态**: 已完成

**修改文件**:
- `ConsultationPanel.xaml` - 采用2x2网格布局
  - Row 0: 现病史 | 中医诊断 (Height="*" 自适应)
  - Row 1: 舌诊 | 脉诊 (Height="Auto")
  - MinWidth="150" 防止列被挤压

### 8.3 Phase 3: 待诊队列三种状态

**状态**: 已完成

**修改文件**:
| 文件 | 修改内容 |
|------|----------|
| `MedicalCaseEnums.cs` | 新增PendingCaseType枚举(Waiting/InProgress/Suspended) |
| `PendingMedicalCaseDto.cs` | Type属性改为PendingCaseType，添加TypeDisplay计算属性 |
| `PendingQueueControl.xaml` | 添加三种状态颜色(灰/绿/橙)和DataTrigger样式 |

### 8.4 验证状态

- [x] 编译通过 (0错误, 0警告)
- [ ] 手动测试验证 (待用户验证)
