# OpenSpec Proposal: standardize-converter-organization

**Change ID**: standardize-converter-organization
**Status**: applied
**Applied**: 2025-12-30
**Priority**: High
**Estimated Effort**: 1.5h
**Created**: 2025-12-30

---

## 1. Problem Statement

### 1.1 Current Situation

Converter组织存在以下问题：

#### 问题1: 单文件多类
`Shell/Converters/BoolToTranslateXConverter.cs` 包含两个类：
- BoolToTranslateXConverter
- BoolToOpacityConverter

违反：单一职责原则、一文件一类规范

#### 问题2: 目录位置错误
`Infrastructure/Controls/PatientCardDisplayModeToVisibilityConverter.cs`
- Converter放在Controls目录下
- 命名空间为 `LYBT.Desktop.Infrastructure.Controls`
- 应该在 `Infrastructure/Converters/` 目录

#### 问题3: Shell专用 vs 通用
当前结构：
- `Shell/Converters/` - 2个文件（Shell专用）
- `Infrastructure/Converters/` - 17个文件（通用）

问题：Shell中的BoolToOpacityConverter其实是通用的，可以移到Infrastructure

### 1.2 Impact

- 代码组织混乱，难以查找
- 命名空间污染
- 违反项目结构规范

---

## 2. Solution

### 2.1 Design Principles

1. **一文件一类**: 每个Converter单独一个文件
2. **统一目录**: 所有Converter放在 `**/Converters/` 目录
3. **通用优先**: 通用Converter放Infrastructure，模块专用放模块内
4. **命名规范**: `[Type]To[Type]Converter.cs`

### 2.2 Implementation Steps

#### Step 1: 拆分多类文件
将 `Shell/Converters/BoolToTranslateXConverter.cs` 拆分为：
- `Shell/Converters/BoolToTranslateXConverter.cs` (保留，Shell专用)
- `Infrastructure/Converters/BoolToOpacityConverter.cs` (新建，通用)

#### Step 2: 移动错位文件
将 `Infrastructure/Controls/PatientCardDisplayModeToVisibilityConverter.cs` 移动到：
`Infrastructure/Converters/PatientCardDisplayModeToVisibilityConverter.cs`

更新命名空间：
```csharp
// Before
namespace LYBT.Desktop.Infrastructure.Controls

// After
namespace LYBT.Desktop.Infrastructure.Converters
```

#### Step 3: 更新XAML引用
更新所有使用这些Converter的XAML文件的xmlns声明

### 2.3 File Changes

| Action | Source | Target |
|--------|--------|--------|
| SPLIT | Shell/Converters/BoolToTranslateXConverter.cs | 拆分为两个文件 |
| CREATE | - | Infrastructure/Converters/BoolToOpacityConverter.cs |
| MOVE | Infrastructure/Controls/PatientCardDisplayModeToVisibilityConverter.cs | Infrastructure/Converters/ |
| UPDATE | 相关XAML文件 | 更新xmlns引用 |

---

## 3. Validation

### 3.1 Acceptance Criteria

- [x] 每个Converter文件只包含一个类
- [x] 所有Converter在正确的Converters/目录
- [x] 命名空间与目录结构一致
- [x] XAML编译通过
- [ ] UI运行正常 (待手动验证)

### 3.2 Test Commands

```bash
dotnet build LYBT.All.sln -c Release
# 手动测试：启动应用验证Converter功能
```

---

## 4. Affected Files

### XAML Files to Update
需要更新xmlns引用的文件：
- `Infrastructure/Controls/PatientInfoCardControl.xaml`
- `Shell/Views/MainWindow.xaml`
- 其他使用这些Converter的XAML

---

## 5. Dependencies

- 需要先完成 cleanup-interface-duplication（可并行）

---

## 6. Timeline

| Phase | Task | Duration |
|-------|------|----------|
| 1 | 拆分BoolToTranslateXConverter.cs | 20min |
| 2 | 移动PatientCardDisplayModeToVisibilityConverter | 20min |
| 3 | 更新XAML引用 | 30min |
| 4 | 验证编译和UI | 20min |

---

## 7. Implementation Record (2025-12-30)

### 7.1 Step 1: 拆分多类文件

**状态**: 已完成 (之前会话)

**说明**: `BoolToOpacityConverter` 已单独存在于 `Infrastructure/Converters/BoolToOpacityConverter.cs`

### 7.2 Step 2: 移动错位文件

**状态**: 已完成

**变更**:
| 操作 | 文件 |
|------|------|
| 创建 | `Infrastructure/Converters/PatientCardDisplayModeToVisibilityConverter.cs` |
| 删除 | `Infrastructure/Controls/PatientCardDisplayModeToVisibilityConverter.cs` |

**命名空间更新**:
```csharp
// Before
namespace LYBT.Desktop.Infrastructure.Controls

// After
namespace LYBT.Desktop.Infrastructure.Converters
```

### 7.3 Step 3: 更新XAML引用

**状态**: 已完成

**变更文件**: `PatientInfoCardControl.xaml`
- 添加命名空间: `xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters"`
- 更新引用: `<converters:PatientCardDisplayModeToVisibilityConverter x:Key="DisplayModeConverter"/>`

### 7.4 验证结果

- [x] 编译通过 (0错误, 0警告)
- [ ] 手动测试验证 (待用户验证)
