# Tasks: cleanup-ui-layer

## 前置依赖

- [x] 0.1 等待 `refactor-viewmodel-layer` 完成并归档
- [x] 0.2 确认 MedicalCaseWorkspaceViewModel 重构完成

## Phase 1: ViewModel继续重构

### 1.1 拆分PrescriptionPanelViewModel

- [x] 1.1.1 分析PrescriptionPanelViewModel职责边界
- [x] 1.1.2 创建 `MedicalCase/ViewModels/Components/PrescriptionCalculator.cs`
- [x] 1.1.3 创建 `MedicalCase/ViewModels/Components/PrescriptionValidator.cs`
- [x] 1.1.4 创建 `MedicalCase/ViewModels/Components/PrescriptionItemHandler.cs` (原PrescriptionEventHandler简化)
- [x] 1.1.5 提取计算逻辑到Calculator
- [x] 1.1.6 提取验证逻辑到Validator
- [x] 1.1.7 提取药材项操作逻辑到ItemHandler
- [x] 1.1.8 更新PrescriptionPanelViewModel为协调器
- [x] 1.1.9 更新MedicalCaseModule注册新Components
- [ ] 1.1.10 为新Components编写单元测试 (延迟)
- [x] 1.1.11 委托核心方法到Components (1484行→1423行, 减少61行)
- [x] 1.1.12 创建PrescriptionSaveHandler处理保存逻辑
- [x] 1.1.13 创建PrescriptionImportHandler处理导入逻辑
- [x] 1.1.14 委托Save/Import方法到新Components (1423行→1300行, 减少123行)
- [x] 1.1.15 创建PrescriptionDataLoader处理数据加载 (1300行→1236行, 减少64行)
- [ ] 1.1.16 进一步优化以达成 < 500行目标 (可选，评估后决定)

### 1.2 拆分PatientSelectionViewModel

- [x] 1.2.1 分析PatientSelectionViewModel职责边界
- [x] 1.2.2 创建 `Patients/ViewModels/Components/MedicalCaseStartCoordinator.cs` (处理医案启动流程)
- [x] 1.2.3 提取医案启动逻辑到Coordinator (检查未完成医案、多医生场景、关闭/继续/新建操作)
- [x] 1.2.4 更新PatientSelectionViewModel使用MedicalCaseStartCoordinator
- [x] 1.2.5 更新PatientsModule注册MedicalCaseStartCoordinator
- [x] 1.2.6 更新PatientSelectionViewModelTests添加Coordinator依赖
- [x] 1.2.7 验证构建和测试 (0 errors, 10/10 tests passed)
- [x] 1.2.8 PatientSelectionViewModel行数: 1429 → 1347行 (减少82行)
- [x] 1.2.9 评估进一步拆分: **不建议** (已委托6个组件,剩余为核心ViewModel职责)

**说明**:
- 原计划的PatientSearchHandler、PatientQueueManager、PatientEventCoordinator已由现有的PatientSearchManager、PendingQueueManager、UnfinishedCaseHandler服务覆盖
- 本次新增MedicalCaseStartCoordinator处理医案启动流程
- 1347行虽超600行规范,但已最大化组件委托,剩余为UI状态/导航/事件等核心ViewModel职责
- 强行拆分会违反MVVM模式自然边界,增加不必要复杂度

### 1.3 ViewModelBase继承链优化 (可选，评估后决定)

- [x] 1.3.1 分析当前继承链使用情况
- [x] 1.3.2 识别可提取为组合的功能
- [x] 1.3.3 创建设计方案文档 (phase-1.3-evaluation.md)
- [x] 1.3.4 评估迁移成本和收益
- [x] 1.3.5 决定是否执行: **不执行** (ROI过低，继承深度已达标)
- [~] 1.3.6-1.3.10 跳过 (基于1.3.5决策)

**评估结论**:
- 继承深度: 4层 (3层在项目控制下)，已符合viewmodel-conventions spec要求
- 唯一可行候选: IMessagePresenter提取，但需修改29个ViewModel，ROI过低
- 详见: `phase-1.3-evaluation.md`

## Phase 2: View层样式统一

### 2.1 建立全局样式标准 (已完成)

- [x] 2.1.1 增强 `Shell/Styles/Colors.xaml` - 完整颜色系统
- [x] 2.1.2 增强 `Shell/Styles/Typography.xaml` - 完整文字样式
- [x] 2.1.3 增强 `Shell/Styles/Controls.xaml` - 按钮/输入框/DataGrid
- [x] 2.1.4 移除 App.xaml 中 CommonStyles.xaml 引用
- [x] 2.1.5 创建重构方案文档 `phase-2-style-unification.md`
- [x] 2.1.6 验证编译通过 (0 errors)

**说明**:
- 全局样式已位于 `Shell/Styles/`，App.xaml 已引用
- 详见: `phase-2-style-unification.md`

### 2.2 按模块迁移样式 (进行中)

**MedicalCase模块** (高优先级):
- [x] 2.2.1 更新 MedicalCaseWorkspaceView.xaml (移除MedicalCaseStyles引用, 硬编码颜色→全局Brush)
- [x] 2.2.2 更新 PrescriptionEditorPanel.xaml (移除MedicalCaseStyles引用, 添加WarningLightBrush)
- [x] 2.2.3 更新 ConsultationPanel.xaml (移除MedicalCaseStyles引用, 添加FormTextAreaStyle)
- [ ] 2.2.4 更新 MedicalCase对话框 (可选优化)
- [ ] 2.2.5 评估是否删除 MedicalCaseStyles.xaml (待其他模块迁移后决定)

**新增全局样式 (2025-12-02)**:
- Colors.xaml: 新增 SuccessLightBrush, WarningLightBrush, ErrorLightBrush, DangerLightBrush
- Controls.xaml: 新增 FormTextArea/FormTextAreaStyle
- Typography.xaml: 已有完整文字样式

**Auth模块**:
- [x] 2.2.6 更新 LoginWindow.xaml (清理无效主题引用; LoginView保持中医风格设计 Issue #1826)

**Admin模块**:
- [x] 2.2.7 更新 AdminHomeView.xaml (迁移到全局Brush: PrimaryBrush, SurfaceBrush, TextSecondaryBrush)

**其他模块** (Issue #2251 完成 2025-12-02):
- [x] 2.2.8 Patients 模块迁移完成 (PatientSelectionView, PatientDetailView, UnfinishedCaseDialog)
- [x] 2.2.9 Users 模块迁移完成 (ChangePasswordView, UserProfileView, UserDetailView)
- [x] 2.2.10 Herbs 模块迁移完成 (HerbDetailView)
- [x] 2.2.11 Formula 模块迁移完成 (FormulaValidationView, FormulaDetailView)

**迁移结果**:
- 所有模块硬编码颜色已迁移到全局Brush
- 仅保留DropShadowEffect Color (不支持Brush)
- 所有模块构建验证: 0 errors, 0 warnings
- Phase 2样式统一目标完成

### 2.3 统一对话框目录结构 (延后)

- [ ] 2.3.1 盘点所有模块的对话框位置
- [ ] 2.3.2 创建标准目录结构文档
- [ ] 2.3.3 迁移Views/下的对话框到Dialogs/
- [ ] 2.3.4 更新模块注册代码
- [ ] 2.3.5 验证所有对话框正常工作

## Phase 3: 基础设施层整理

### 3.1 服务职责梳理

- [ ] 3.1.1 审计Foundation层所有服务
- [ ] 3.1.2 审计Infrastructure层所有服务
- [ ] 3.1.3 审计Presentation层所有服务
- [ ] 3.1.4 创建服务职责矩阵文档
- [ ] 3.1.5 识别重复和可合并服务

### 3.2 通知服务统一

- [ ] 3.2.1 比较INotificationService实现
- [ ] 3.2.2 统一到Infrastructure/Notifications/
- [ ] 3.2.3 标记旧接口为Obsolete
- [ ] 3.2.4 更新所有使用处
- [ ] 3.2.5 删除Presentation层重复通知代码

### 3.3 清理未使用代码

- [ ] 3.3.1 运行代码覆盖率分析
- [ ] 3.3.2 识别未被引用的接口和类
- [ ] 3.3.3 确认删除安全性
- [ ] 3.3.4 删除未使用代码
- [ ] 3.3.5 更新相关文档

## Phase 4: 交互模式标准化

### 4.1 对话框使用标准化

- [ ] 4.1.1 创建IDialogCoordinator接口
- [ ] 4.1.2 实现DialogCoordinator
- [ ] 4.1.3 创建dialog-patterns spec
- [ ] 4.1.4 迁移确认对话框使用
- [ ] 4.1.5 迁移信息对话框使用
- [ ] 4.1.6 迁移错误对话框使用
- [ ] 4.1.7 删除直接MessageBox调用

### 4.2 通知显示标准化

- [ ] 4.2.1 创建IUserNotification统一接口
- [ ] 4.2.2 实现UserNotification服务
- [ ] 4.2.3 更新ViewModelBase消息方法
- [ ] 4.2.4 迁移所有通知调用
- [ ] 4.2.5 验证通知显示一致性

### 4.3 导航模式文档化

- [ ] 4.3.1 审计现有导航实现
- [ ] 4.3.2 记录标准导航模式
- [ ] 4.3.3 更新viewmodel-conventions spec
- [ ] 4.3.4 添加导航示例代码

## Phase 5: 验证和文档

### 5.1 集成验证

- [ ] 5.1.1 运行所有单元测试
- [ ] 5.1.2 运行集成测试
- [ ] 5.1.3 手动测试关键流程
    - [ ] 患者选择和创建
    - [ ] 医案完整流程
    - [ ] 处方编辑和计算
    - [ ] 对话框交互
- [ ] 5.1.4 验证编译无错误无警告
- [ ] 5.1.5 代码覆盖率检查

### 5.2 文档更新

- [ ] 5.2.1 更新 `docs/guides/` 开发指南
- [ ] 5.2.2 添加样式使用示例
- [ ] 5.2.3 添加对话框使用示例
- [ ] 5.2.4 更新架构图

## 验收标准

- [ ] 所有ViewModel行数 < 800行
- [ ] ViewModelBase继承链 <= 3层 (或保持现状但有文档说明)
- [ ] 全局样式覆盖主要UI元素
- [ ] 对话框使用统一接口
- [ ] 通知显示统一接口
- [ ] 编译通过，0 errors, 0 warnings
- [ ] 单元测试覆盖率不下降
- [ ] 所有手动测试通过

## 依赖关系

```
Phase 0 (前置依赖)
    │
    └─► Phase 1 (ViewModel重构)
            │
            ├─► Phase 2 (样式统一) [可并行]
            │
            └─► Phase 3 (基础设施)
                    │
                    └─► Phase 4 (交互模式)
                            │
                            └─► Phase 5 (验证)
```

**说明**: Phase 2可与Phase 1并行执行，Phase 3和Phase 4需要顺序执行。

## 工作量估算

| Phase | 预计工作量 | 风险等级 | 可并行 |
|-------|-----------|---------|--------|
| Phase 0 (前置) | - | - | - |
| Phase 1 (ViewModel) | 大 | 高 | 否 |
| Phase 2 (样式) | 中 | 低 | 是 |
| Phase 3 (基础设施) | 大 | 高 | 否 |
| Phase 4 (交互模式) | 中 | 中 | 否 |
| Phase 5 (验证) | 小 | 低 | 否 |
