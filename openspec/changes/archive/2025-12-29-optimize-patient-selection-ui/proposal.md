# Change: 优化患者选择界面布局与交互

## Why

医生工作台患者选择界面存在两个体验问题：
1. 右侧患者信息区域使用垂直StackPanel+ScrollViewer布局，导致内容过长出现滚动条，影响信息快速浏览
2. "新建患者"使用弹窗逻辑，与患者管理模块的inline编辑模式不一致，用户体验割裂

## What Changes

### 1. 患者信息区域布局优化
- **BEFORE**: 6个InfoCard垂直堆叠在ScrollViewer中
- **AFTER**: 采用2x3 Grid布局，InfoCard分两列显示，去除ScrollViewer

布局规划：
```
┌─────────────────┬─────────────────┐
│   基本信息      │   身份信息      │
├─────────────────┼─────────────────┤
│   健康信息      │   联系信息      │
├─────────────────┼─────────────────┤
│   紧急联系人    │   就诊统计      │
└─────────────────┴─────────────────┘
```

### 2. 新建患者交互模式统一
- **BEFORE**: NewPatientCommand -> 弹出Dialog
- **AFTER**: 复用PatientMasterDetailView的Detail模式
  - CreateNewCommand -> 右侧区域切换到EditMode
  - PatientEditControl inline编辑
  - Save/Cancel按钮内嵌在Detail区域

## Impact

- Affected specs: `desktop-detail-views`
- Affected code:
  - `PatientViewControl.xaml` - 布局重构
  - `PatientSelectionView.xaml` - Detail区域重构
  - `PatientSelectionViewModel.cs` - 添加编辑模式支持
  - `PatientsModule.cs` - 可能需要移除Dialog注册
