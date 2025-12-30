# Tasks: standardize-converter-organization

**Change ID**: standardize-converter-organization
**Total Tasks**: 4
**Completed**: 0/4

---

## Task List

### Task 1: Split BoolToTranslateXConverter.cs
- **ID**: SCO-001
- **Status**: completed
- **Priority**: P0
- **Effort**: 20min

**Description**: 将BoolToTranslateXConverter.cs拆分为两个文件

**Current State**:
```
Shell/Converters/BoolToTranslateXConverter.cs
  - class BoolToTranslateXConverter (Shell专用)
  - class BoolToOpacityConverter (通用)
```

**Target State**:
```
Shell/Converters/BoolToTranslateXConverter.cs
  - class BoolToTranslateXConverter

Infrastructure/Converters/BoolToOpacityConverter.cs
  - class BoolToOpacityConverter
```

**Acceptance Criteria**:
- [ ] Shell中只保留BoolToTranslateXConverter
- [ ] Infrastructure中新建BoolToOpacityConverter
- [ ] 命名空间正确

---

### Task 2: Move PatientCardDisplayModeToVisibilityConverter
- **ID**: SCO-002
- **Status**: skipped (设计决策: 与枚举紧密耦合，保持在Controls目录)
- **Priority**: P0
- **Effort**: 20min

**Description**: 将Converter从Controls目录移到Converters目录

**Current Location**:
`Infrastructure/Controls/PatientCardDisplayModeToVisibilityConverter.cs`
`namespace LYBT.Desktop.Infrastructure.Controls`

**Target Location**:
`Infrastructure/Converters/PatientCardDisplayModeToVisibilityConverter.cs`
`namespace LYBT.Desktop.Infrastructure.Converters`

**Acceptance Criteria**:
- [ ] 文件移动到正确目录
- [ ] 命名空间更新
- [ ] 编译通过

---

### Task 3: Update XAML References
- **ID**: SCO-003
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min
- **Depends On**: SCO-001, SCO-002

**Description**: 更新所有XAML文件中的Converter引用

**Files to Check**:
- `Infrastructure/Controls/PatientInfoCardControl.xaml`
- `Shell/Views/MainWindow.xaml`
- 其他使用这些Converter的XAML

**Acceptance Criteria**:
- [ ] 所有xmlns声明更新
- [ ] StaticResource引用正确
- [ ] XAML编译通过

---

### Task 4: Verify UI Functionality
- **ID**: SCO-004
- **Status**: pending
- **Priority**: P0
- **Effort**: 20min
- **Depends On**: SCO-003

**Description**: 验证UI功能正常

**Acceptance Criteria**:
- [ ] 应用启动正常
- [ ] 患者卡片显示正常
- [ ] Shell侧边栏动画正常

---

## Progress Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Split | SCO-001 | pending |
| Move | SCO-002 | pending |
| Update | SCO-003 | pending |
| Verify | SCO-004 | pending |
