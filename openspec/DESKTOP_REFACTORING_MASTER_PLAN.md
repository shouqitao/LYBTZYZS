# Desktop层重构总体规划

**创建时间**: 2025-12-30
**状态**: 规划阶段
**维护者**: Claude Code

---

## Executive Summary

Desktop层当前有12个活跃OpenSpec提案，经分析整合后归类为4个优先级层次，建议按"清理→规范→优化→增强"的顺序执行。

---

## 1. 提案全景图

### 1.1 活跃提案清单 (12个)

| # | 提案ID | 状态 | 优先级 | 工作量 | 类别 |
|---|--------|------|--------|--------|------|
| 1 | cleanup-interface-duplication | draft | Critical | 2h | 技术债务 |
| 2 | standardize-converter-organization | draft | High | 1.5h | 代码规范 |
| 3 | standardize-service-naming | draft | Medium | 3h | 代码规范 |
| 4 | standardize-usercontrol-organization | draft | High | 3h | 代码规范 |
| 5 | slim-medicalcase-viewmodel | partial | High | 4h | ViewModel优化 |
| 6 | slim-medicalcase-workspace-viewmodel | draft | High | 6h | ViewModel优化 |
| 7 | optimize-medicalcase-navigation | in-progress | High | - | MedicalCase专项 |
| 8 | optimize-medicalcase-ui | draft | Medium | - | MedicalCase专项 |
| 9 | refactor-medicalcase-workspace | draft | Medium | - | MedicalCase专项 |
| 10 | unify-desktop-architecture | draft | Low | 大型 | 宏观愿景 |
| 11 | refactor-desktop-comprehensive | draft | Low | 大型 | 宏观愿景 |

### 1.2 提案关系图

```
                    ┌─────────────────────────────────────┐
                    │     refactor-desktop-comprehensive  │
                    │     unify-desktop-architecture      │
                    │         (宏观愿景 - 暂缓)           │
                    └─────────────────────────────────────┘
                                      │
        ┌─────────────────────────────┼─────────────────────────────┐
        │                             │                             │
        ▼                             ▼                             ▼
┌───────────────────┐    ┌──────────────────────┐    ┌──────────────────────┐
│   技术债务清理     │    │     代码规范统一      │    │   ViewModel优化      │
│                   │    │                      │    │                      │
│ • cleanup-        │    │ • standardize-       │    │ • slim-medicalcase-  │
│   interface-      │    │   converter-         │    │   viewmodel          │
│   duplication     │    │   organization       │    │ • slim-workspace-    │
│                   │    │ • standardize-       │    │   viewmodel          │
│   [优先级: P0]    │    │   service-naming     │    │                      │
│                   │    │ • standardize-       │    │   [优先级: P2]       │
│                   │    │   usercontrol-       │    │                      │
│                   │    │   organization       │    └──────────────────────┘
│                   │    │                      │
│                   │    │   [优先级: P1]       │
└───────────────────┘    └──────────────────────┘
        │                             │
        │                             │
        └─────────────┬───────────────┘
                      │
                      ▼
        ┌──────────────────────────────┐
        │    MedicalCase专项优化        │
        │                              │
        │ • optimize-navigation        │
        │ • optimize-ui               │
        │ • refactor-workspace        │
        │                              │
        │   [优先级: P3 - 功能驱动]    │
        └──────────────────────────────┘
```

---

## 2. 提案合并与取消决策

### 2.1 需要合并的提案

| 主提案 | 被合并提案 | 原因 |
|--------|-----------|------|
| slim-medicalcase-workspace-viewmodel | slim-medicalcase-viewmodel | 同一目标，后者更具体 |
| refactor-desktop-comprehensive | unify-desktop-architecture | 内容重叠，保留一个 |

### 2.2 建议取消的提案

| 提案 | 取消原因 | 替代方案 |
|------|----------|----------|
| unify-desktop-architecture | 与refactor-desktop-comprehensive重叠 | 合并到comprehensive |
| slim-medicalcase-viewmodel | 已部分完成，剩余工作归入workspace-viewmodel | slim-medicalcase-workspace-viewmodel |

### 2.3 建议暂缓的提案

| 提案 | 暂缓原因 | 重启条件 |
|------|----------|----------|
| refactor-desktop-comprehensive | 范围过大，需分解 | P0-P2提案完成后重新评估 |
| refactor-medicalcase-workspace | 与workspace-viewmodel重叠 | workspace-viewmodel完成后评估 |

---

## 3. 推荐执行顺序

### Sprint 1: 技术债务清理 (约2小时)

**目标**: 消除架构隐患，建立干净基础

| 序号 | 提案 | 工作量 | 关键任务 |
|------|------|--------|----------|
| 1 | cleanup-interface-duplication | 2h | 删除9个重复接口文件 |

**验收标准**:
- 编译通过，0接口歧义
- Infrastructure/Services/Interfaces/只保留2个唯一接口

---

### Sprint 2: 代码规范统一 (约7.5小时)

**目标**: 建立统一的目录结构和命名规范

| 序号 | 提案 | 工作量 | 关键任务 |
|------|------|--------|----------|
| 2 | standardize-converter-organization | 1.5h | 拆分BoolToTranslateXConverter + 移动Converter |
| 3 | standardize-usercontrol-organization | 3h | 合并StatusBadge + 位置规范文档 |
| 4 | standardize-service-naming | 3h | 命名规范文档 + 审查报告 |

**验收标准**:
- 一文件一类原则落实
- 控件位置规范文档完成
- 服务命名规范文档完成

---

### Sprint 3: ViewModel优化 (约6小时)

**目标**: MedicalCaseWorkspaceViewModel从1183行降至950行以下

| 序号 | 提案 | 工作量 | 关键任务 |
|------|------|--------|----------|
| 5 | slim-medicalcase-workspace-viewmodel | 6h | 创建3个组件类，迁移逻辑 |

**验收标准**:
- WorkspaceStatusDisplay组件完成
- WorkspaceButtonState组件完成
- PendingQueueViewModel组件完成
- ViewModel行数<950

---

### Sprint 4: MedicalCase功能优化 (按需)

**目标**: 完善MedicalCase模块功能

| 序号 | 提案 | 触发条件 |
|------|------|----------|
| 6 | optimize-medicalcase-navigation | 用户报告导航问题 |
| 7 | optimize-medicalcase-ui | 用户提出UI优化需求 |

**注**: 这些是功能驱动的优化，而非架构必须项。

---

## 4. 每个提案的详细设计

### 4.1 cleanup-interface-duplication (Critical)

**问题**: 9个接口在Contracts和Infrastructure两处重复定义

**解决方案**: 
- 保留Contracts/Services/MasterDetail/中的定义
- 删除Infrastructure/Services/Interfaces/中的重复定义
- 保留IAsyncExecutor.cs和IListViewServices.cs（唯一接口）

**影响文件**:
```
删除:
- Infrastructure/Services/Interfaces/IDetailEditorService.cs
- Infrastructure/Services/Interfaces/IDialogManager.cs
- Infrastructure/Services/Interfaces/IErrorHandler.cs
- Infrastructure/Services/Interfaces/ILoadingStateManager.cs
- Infrastructure/Services/Interfaces/IMasterDetailServices.cs
- Infrastructure/Services/Interfaces/IPaginationService.cs
- Infrastructure/Services/Interfaces/ISearchService.cs
- Infrastructure/Services/Interfaces/ISelectionService.cs
- Infrastructure/Services/Interfaces/IViewNavigationService.cs
```

---

### 4.2 standardize-converter-organization (High)

**问题**: 
- BoolToTranslateXConverter.cs包含两个类
- PatientCardDisplayModeToVisibilityConverter在Controls目录

**解决方案**:
1. 拆分BoolToTranslateXConverter.cs
2. 将BoolToOpacityConverter移到Infrastructure/Converters/
3. 将PatientCardDisplayModeToVisibilityConverter移到Converters/

---

### 4.3 standardize-usercontrol-organization (High)

**核心原则**:
> 不要为了控件化而控件化。架构优越性 + 设计优越性并重。

**行动项**:
1. 合并StatusBadge和UnifiedStatusBadge（真正重复）
2. 保留DataGridToolbar和UnifiedManagementToolBar（各有用途）
3. 创建控件位置规范文档

**控件设计评估**:
| 控件 | 复用次数 | 集成度 | 评价 |
|------|----------|--------|------|
| PatientInfoCardControl | 3+ | 高(2绑定) | 设计优秀 |
| PatientViewControl | 3 | 低(10绑定) | 架构合理，可渐进优化 |
| StatusBadge | 10+ | 高(2绑定) | 设计优秀 |

---

### 4.4 standardize-service-naming (Medium)

**命名规范**:
| 后缀 | 用途 | 示例 |
|------|------|------|
| Service | 业务逻辑（无状态） | AuthenticationService |
| Handler | HTTP/事件处理 | TokenRefreshHandler |
| Manager | 状态管理（有状态） | TokenManager |
| Coordinator | 复杂编排 | MedicalCaseWorkspaceCoordinator |
| Validator | 验证逻辑 | ConsultationValidator |
| Calculator | 计算逻辑 | PrescriptionCalculator |

---

### 4.5 slim-medicalcase-workspace-viewmodel (High)

**目标**: ViewModel从1183行降至<950行

**组件化设计**:
```
MedicalCaseWorkspaceViewModel
├── WorkspaceStatusDisplay          (状态显示组件)
│   ├── ConsultationStatusText
│   ├── ConsultationStatusColor
│   ├── PrescriptionStatusText
│   └── UpdateXxxStatus()
│
├── WorkspaceButtonState            (按钮状态组件)
│   ├── ShowCompleteButton
│   ├── ShowDraftButton
│   └── Update()
│
└── PendingQueueViewModel           (待诊队列子ViewModel)
    ├── Queue
    ├── RefreshCommand
    └── CaseSelected event
```

**迁移内容**:
| 从ViewModel | 到组件 | 行数 |
|-------------|--------|------|
| UpdateConsultationStatus | WorkspaceStatusDisplay | ~50 |
| UpdatePrescriptionStatus | WorkspaceStatusDisplay | ~50 |
| ShowCompleteButton等属性 | WorkspaceButtonState | ~30 |
| LoadPendingQueueAsync | PendingQueueViewModel | ~60 |
| ExecuteSelectPendingCaseAsync | PendingQueueViewModel | ~50 |

---

## 5. 风险与缓解

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 删除接口后编译失败 | 低 | 高 | 先分析引用，逐个删除 |
| 控件合并后UI异常 | 中 | 中 | 合并前对比功能，合并后测试 |
| ViewModel拆分引入Bug | 中 | 高 | 每个组件完成后立即测试 |
| 规范文档无人遵循 | 中 | 低 | 在CLAUDE.md中引用，CI检查 |

---

## 6. 工作量估算

| Sprint | 内容 | 工作量 | 累计 |
|--------|------|--------|------|
| Sprint 1 | 技术债务清理 | 2h | 2h |
| Sprint 2 | 代码规范统一 | 7.5h | 9.5h |
| Sprint 3 | ViewModel优化 | 6h | 15.5h |
| Sprint 4 | 功能优化 | 按需 | - |

**总计**: 约15.5小时基础工作

---

## 7. 提案状态追踪

### 活跃提案

| 提案 | 状态 | 进度 | 下一步 |
|------|------|------|--------|
| cleanup-interface-duplication | draft | 0% | 执行删除 |
| standardize-converter-organization | draft | 0% | 拆分文件 |
| standardize-usercontrol-organization | draft | 0% | 合并StatusBadge |
| standardize-service-naming | draft | 0% | 创建规范文档 |
| slim-medicalcase-workspace-viewmodel | draft | 0% | 创建组件类 |
| optimize-medicalcase-navigation | in-progress | 80% | 测试验证 |

### 已完成/部分完成

| 提案 | 状态 | 完成内容 |
|------|------|----------|
| slim-medicalcase-viewmodel | partial | Phase 1-2完成，Phase 3取消 |

### 建议取消/合并

| 提案 | 建议 | 原因 |
|------|------|------|
| unify-desktop-architecture | 合并 | 与comprehensive重叠 |
| refactor-desktop-comprehensive | 暂缓 | 范围过大 |
| refactor-medicalcase-workspace | 暂缓 | 与workspace-viewmodel重叠 |

---

## 8. 决策记录

### DR-001: 宏观提案处理方式

**决策**: 暂缓unify-desktop-architecture和refactor-desktop-comprehensive

**原因**: 
1. 范围过大难以估算
2. 包含的具体工作已分解到独立提案
3. 待Sprint 1-3完成后重新评估剩余工作

### DR-002: ViewModel优化策略

**决策**: 使用组合模式而非继续拆分Handler

**原因**:
1. slim-medicalcase-viewmodel中Phase 3取消证明Handler模式有局限
2. 组合模式更适合UI状态相关逻辑
3. 子ViewModel可以复用现有MVVM基础设施

### DR-003: 控件合并策略

**决策**: 仅合并功能完全重复的控件

**原因**:
1. DataGridToolbar和UnifiedManagementToolBar虽名称类似但用途不同
2. 过度合并可能引入不必要的抽象
3. 保持"不为控件化而控件化"原则

---

## Changelog

| 日期 | 变更 |
|------|------|
| 2025-12-30 | 初始版本，整合12个活跃提案 |

