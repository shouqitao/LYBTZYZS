# Tasks: consolidate-wpf-converters

## Overview

统一Desktop层WPF转换器管理，分3个Phase执行。

---

## Phase 1: 创建转换器资源字典 (Converters.xaml)

### Task 1.1: 创建Converters.xaml资源字典
- [ ] 在`Infrastructure/Converters/`目录创建`Converters.xaml`
- [ ] 注册所有15个现有转换器
- [ ] 使用语义化分组注释

### Task 1.2: 统一ApiHealthStatusToColorConverter颜色值
- [ ] 更新颜色值为Fluent Design标准色
  - Healthy: #22C55E (绿色)
  - Checking: #FBBF24 (黄色)
  - Unhealthy: #EF4444 (红色)

### Task 1.3: 更新App.xaml合并资源字典
- [ ] 添加Converters.xaml到MergedDictionaries
- [ ] 移除手动定义的转换器实例
- [ ] 验证编译通过

**验证点**: `dotnet build` 通过，App.xaml引用正确

---

## Phase 2: 删除重复转换器

### Task 2.1: 删除Shell中的重复转换器
- [ ] 删除`Shell/Converters/FirstCharConverter.cs`
- [ ] 删除`Shell/Converters/ApiHealthStatusToColorConverter.cs`
- [ ] 删除`Shell/Converters/ApiHealthStatusToTextConverter.cs`
- [ ] 更新Shell项目中的XAML引用

### Task 2.2: 删除MedicalCase中的重复转换器
- [ ] 删除`MedicalCase/Converters/InvertedBoolConverter.cs`
- [ ] 更新MedicalCase项目中使用`InverseBooleanConverter`

### Task 2.3: 评估Shell专用转换器
- [ ] 检查`BoolToSidebarWidthConverter.cs` - Shell专用，保留
- [ ] 检查`BoolToTranslateXConverter.cs` - Shell专用，保留
- [ ] 确认这些转换器确实是Shell专用，不需要迁移

**验证点**: `dotnet build` 通过，无遗漏引用

---

## Phase 3: 清理View中的本地转换器定义

### Task 3.1: 清理Users模块View
- [ ] `UserProfileView.xaml` - 移除本地BooleanToVisibilityConverter定义
- [ ] `UserMasterDetailView.xaml` - 移除本地转换器定义
- [ ] `ChangePasswordView.xaml` - 移除本地BooleanToVisibilityConverter定义

### Task 3.2: 清理Herbs模块View
- [ ] `HerbMasterDetailView.xaml` - 移除本地转换器定义

### Task 3.3: 清理Patients模块View
- [ ] `PatientSelectionView.xaml` - 移除本地转换器定义
- [ ] 更新InverseBooleanConverter引用

### Task 3.4: 全面搜索确认
- [ ] 使用Grep搜索所有`<.*Converter x:Key=`模式
- [ ] 确认无遗漏的本地转换器定义
- [ ] 确认所有View使用全局StaticResource

**验证点**: 
- `dotnet build` 通过
- 无View内重复定义转换器
- UI功能正常

---

## Phase 4: 文档与规范更新

### Task 4.1: 更新Spec
- [ ] 创建`converter-conventions` spec delta
- [ ] 定义转换器命名规范
- [ ] 定义转换器注册规范

### Task 4.2: 删除空目录
- [ ] 删除`Shell/Converters/`目录（如果为空）
- [ ] 删除`MedicalCase/Converters/`目录（如果为空）

**验证点**: OpenSpec validate通过

---

## Rollback Plan

如果发现问题:
1. 恢复App.xaml原有转换器定义
2. 保留Converters.xaml但不合并
3. 逐步迁移而非一次性删除

---

## Dependencies

- Phase 2 依赖 Phase 1 完成
- Phase 3 依赖 Phase 2 完成
- Phase 4 可与 Phase 3 并行

## Estimated Effort

| Phase | 任务数 | 复杂度 |
|-------|--------|--------|
| Phase 1 | 3 | 低 |
| Phase 2 | 3 | 中 |
| Phase 3 | 4 | 低 |
| Phase 4 | 2 | 低 |

**总计**: 12个任务
