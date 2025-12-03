# OpenSpec Proposal: cleanup-ui-layer

## 元数据

- **提案ID**: cleanup-ui-layer
- **创建日期**: 2025-12-01
- **状态**: Draft
- **关联**: refactor-viewmodel-layer (前置依赖)
- **范围**: Client Desktop UI层全面清理

## Why

Client Desktop UI层经过多次迭代后积累了技术债务，需要全面清理以提升代码质量和可维护性：

1. **ViewModel层未完成重构** - refactor-viewmodel-layer仅完成Phase 1，仍有2个1400+行的大型ViewModel
2. **View层(XAML)样式分散** - 缺少统一的样式库，各模块样式定义重复
3. **基础设施层混乱** - ViewModelBase继承链过长(4层)，职责不清晰
4. **交互模式不统一** - 对话框、通知、导航等实现方式各异

## What Changes

### Phase 1: ViewModel层继续重构 (依赖refactor-viewmodel-layer完成)
- 拆分PrescriptionPanelViewModel (1484行)
- 拆分PatientSelectionViewModel (1429行)
- 精简ViewModelBase继承链

### Phase 2: View层样式统一
- 创建全局样式库
- 提取公共控件模板
- 清理模块内重复样式

### Phase 3: 基础设施层整理
- 重构ViewModelBase继承体系
- 统一服务接口定义
- 清理未使用代码

### Phase 4: 交互模式标准化
- 统一对话框使用模式
- 统一通知显示机制
- 标准化导航流程

## 当前状态分析

### ViewModel层现状

| ViewModel | 行数 | 状态 |
|-----------|------|------|
| PrescriptionPanelViewModel | 1484行 | 待拆分 |
| MedicalCaseWorkspaceViewModel | 1481行 | 已初步重构(添加Coordinator) |
| PatientSelectionViewModel | 1429行 | 待拆分 |
| PrescriptionEditorViewModel | 986行 | 评估是否需要拆分 |
| FormulaDetailViewModel | 983行 | 已有Components |

**ViewModelBase继承链**:
```
BindableBase (Prism)
    └─ ViewModelBase (537行)
        └─ UnifiedViewModelBase (576行)
            └─ UnifiedListViewModelBase<T> (605行)
```
总计: 1718行基类代码，职责分散

### View层现状

| 统计项 | 数量 |
|--------|------|
| XAML文件总数 | 71个 |
| 对话框数量 | 19个 |
| 样式文件 | MedicalCaseStyles.xaml (317行) |
| 自定义控件 | HerbCardControl.xaml (159行) |

**问题**:
- 模块内样式定义分散，缺少全局样式资源
- 对话框放置位置不统一 (Views/ vs Dialogs/)
- 控件模板重复定义

### 基础设施层现状

**Foundation层** (42个文件):
- Security: 11个文件 (Token, Credential, Auth)
- Http: 4个文件 (API通信)
- Exceptions: 4个文件 (异常处理)
- 其他: 23个文件 (杂项服务)

**Infrastructure层** (12个服务):
- UserActivityTracker (309行)
- SessionManager (247行)
- ValidationService, KeyboardShortcutService等

**Presentation层**:
- Components, Theming, Navigation, Notifications等
- 职责与Foundation/Infrastructure有重叠

### 交互模式现状

**对话框实现**:
- Prism IDialogService (主要方式)
- 自定义DialogViewModel基类
- 部分使用MessageBox直接调用

**通知机制**:
- INotificationService
- UserNotificationService
- UnifiedErrorHandlingService
- 直接MessageBox调用

## 受影响的组件

### 新增/修改规范
- `openspec/specs/viewmodel-conventions/spec.md` - 完善
- `openspec/specs/ui-style-conventions/spec.md` - 新增
- `openspec/specs/dialog-patterns/spec.md` - 新增

### 重构代码

#### ViewModel层
- `PrescriptionPanelViewModel.cs` - 拆分
- `PatientSelectionViewModel.cs` - 拆分
- ViewModelBase继承体系 - 精简

#### View层
- 创建 `Presentation/Themes/GlobalStyles.xaml`
- 统一对话框目录结构
- 提取公共DataTemplates

#### 基础设施层
- 合并重复服务
- 清理未使用接口
- 统一服务命名

## 成功标准

1. 所有ViewModel < 800行
2. 全局样式覆盖率 > 80%
3. ViewModelBase继承链 <= 3层
4. 对话框使用统一模式
5. 编译通过，现有功能不受影响
6. 单元测试覆盖率不下降

## 风险评估

- **风险**: 高
- **原因**: 涉及多层重构，影响面广
- **缓解**:
  - 分Phase执行，每Phase独立可验证
  - Phase 1依赖refactor-viewmodel-layer完成
  - 优先保持向后兼容

## 工作量估算

| Phase | 工作量 | 依赖 |
|-------|--------|------|
| Phase 1 (ViewModel继续重构) | 中 | refactor-viewmodel-layer |
| Phase 2 (样式统一) | 中 | 无 |
| Phase 3 (基础设施整理) | 大 | Phase 1 |
| Phase 4 (交互模式标准化) | 中 | Phase 2, 3 |

---

**提案状态**: Draft
