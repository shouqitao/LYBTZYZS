# Desktop Prism Phase 3 实施方案 - Dialog 标准化

**创建日期**：2025-10-01
**分析方法**：UltraThink Sequential Thinking (10 步骤)
**Issue**：#828 Desktop Prism Refactoring Epic
**阶段**：Phase 3 - Dialog Standardization
**预计工期**：7-10 工作日

---

## 📋 执行摘要

Phase 3 目标是将所有对话框迁移到 Prism IDialogService 标准，移除自定义 SimplifiedDialogService，实现统一的对话框管理架构。

**关键发现**：
- 10个 Dialog 文件，6个需迁移，4个已符合标准
- Prescriptions 模块 80% 完成（仅需注册）
- MedicalCase 模块 100% 完成（可作参考模板）
- SimplifiedDialogService 仅用于 MessageBox 封装，无复杂业务逻辑

---

## 🔍 UltraThink 分析结果

### 当前对话框清单

#### Window（旧格式，需迁移）：6个

| 模块 | 文件 | ViewModel | IDialogAware | RegisterDialog |
|------|------|-----------|--------------|----------------|
| Formula | EditFormulaDialog.xaml | EditFormulaDialogViewModel | ❌ | ❌ |
| Formula | ViewFormulaDialog.xaml | ViewFormulaDialogViewModel | ❌ | ❌ |
| Prescriptions | SelectFormulaDialog.xaml | SelectFormulaDialogViewModel | ✅ | ❌ |
| Users | ChangePasswordDialog.xaml | ? | ? | ✅ |
| Users | ResetPasswordDialog.xaml | ? | ? | ✅ |
| Users | UserProfileDialog.xaml | ? | ? | ✅ |

#### UserControl（已符合标准）：4个

| 模块 | 文件 | ViewModel | IDialogAware | RegisterDialog |
|------|------|-----------|--------------|----------------|
| Prescriptions | FormulaTemplateDialog.xaml | FormulaTemplateDialogViewModel | ✅ | ❌ |
| Prescriptions | HerbSelectionDialog.xaml | HerbSelectionDialogViewModel | ✅ | ❌ |
| Prescriptions | PrescriptionEditorDialog.xaml | PrescriptionEditorDialogViewModel | ✅ | ❌ |
| MedicalCase | CreateMedicalCaseDialog.xaml | CreateMedicalCaseViewModel | ✅ | ❌ |

### 模块迁移进度分析

| 模块 | 总数 | 已完成视图 | 已实现 IDialogAware | 已注册 | 完成度 | 优先级 |
|------|------|-----------|---------------------|--------|--------|--------|
| **Prescriptions** | 4 | 3/4 (75%) | 4/4 (100%) | 0/4 (0%) | **80%** | P1 |
| **MedicalCase** | 1 | 1/1 (100%) | 1/1 (100%) | 0/1 (0%) | **100%** | ✅ 参考 |
| **Users** | 3 | 0/3 (0%) | ? | 3/3 (100%) | **50%** | P3 |
| **Formula** | 2 | 0/2 (0%) | 0/2 (0%) | 0/2 (0%) | **0%** | P2 |

**总计**：10个对话框，4个已符合标准（40%），6个需迁移（60%）

---

## 📅 实施计划（7-10天）

### Day 1-2: Prescriptions 模块完成 ✅ 快速见效

**目标**：完成 Prescriptions 模块 Dialog 标准化（从 80% → 100%）

#### 任务清单

1. **SelectFormulaDialog 视图迁移**（2小时）
   - [ ] 修改 `SelectFormulaDialog.xaml`：`<Window>` → `<UserControl>`
   - [ ] 添加 `prism:ViewModelLocator.AutoWireViewModel="True"`
   - [ ] 添加 `prism:Dialog.WindowStyle`（参考 MedicalCase 模板）
   - [ ] 调整布局（移除 Window 特定属性：Title, Height, Width, WindowStartupLocation）

2. **PrescriptionsModule 注册**（1小时）
   - [ ] 在 `PrescriptionsModule.RegisterTypes()` 添加：
     ```csharp
     // 注册对话框
     containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, ViewModels.FormulaTemplateDialogViewModel>();
     containerRegistry.RegisterDialog<Views.HerbSelectionDialog, ViewModels.HerbSelectionDialogViewModel>();
     containerRegistry.RegisterDialog<Views.PrescriptionEditorDialog, ViewModels.PrescriptionEditorDialogViewModel>();
     containerRegistry.RegisterDialog<Views.SelectFormulaDialog, ViewModels.SelectFormulaDialogViewModel>();
     ```

3. **编译测试**（1小时）
   - [ ] `dotnet build LYBT.Desktop.sln`
   - [ ] 验证 4个 Dialog 注册成功
   - [ ] 检查无编译错误

**验收标准**：
- ✅ Prescriptions 模块 4个 Dialog 全部为 UserControl
- ✅ 4个 ViewModel 已实现 IDialogAware
- ✅ 4个 Dialog 已在模块中注册
- ✅ 编译成功（0错误）

---

### Day 3-4: Formula 模块迁移 🔧 完整迁移

**目标**：Formula 模块从 0% → 100%

#### 任务清单

**EditFormulaDialog 迁移**（3小时）

1. **视图改造**（1.5小时）
   - [ ] `EditFormulaDialog.xaml`：Window → UserControl
   - [ ] 添加 prism 命名空间和属性
   - [ ] 添加 Dialog.WindowStyle
   - [ ] 调整布局和绑定

2. **ViewModel 实现 IDialogAware**（1小时）
   - [ ] 在 `EditFormulaDialogViewModel` 实现 IDialogAware
   - [ ] 添加 `Title` 属性
   - [ ] 添加 `RequestClose` 事件
   - [ ] 实现 `OnDialogOpened()`, `OnDialogClosed()`, `CanCloseDialog()`
   - [ ] 修改 OK/Cancel 命令调用 `RequestClose`

3. **参考实现**（使用 MedicalCase/CreateMedicalCaseDialog 作为模板）

**ViewFormulaDialog 迁移**（3小时）

1. **视图改造**（1.5小时）
   - [ ] `ViewFormulaDialog.xaml`：Window → UserControl
   - [ ] 与 EditFormulaDialog 相同步骤

2. **ViewModel 实现 IDialogAware**（1小时）
   - [ ] `ViewFormulaDialogViewModel` 实现 IDialogAware
   - [ ] 完整实现所有接口方法

**模块注册**（30分钟）

- [ ] 在 `FormulaModule.RegisterTypes()` 添加：
  ```csharp
  // 注册对话框
  containerRegistry.RegisterDialog<Views.EditFormulaDialog, ViewModels.EditFormulaDialogViewModel>();
  containerRegistry.RegisterDialog<Views.ViewFormulaDialog, ViewModels.ViewFormulaDialogViewModel>();
  ```

**编译测试**（1小时）

- [ ] 编译 Formula 模块
- [ ] 测试对话框显示
- [ ] 验证参数传递和结果返回

**验收标准**：
- ✅ 2个 Dialog 视图为 UserControl + prism:Dialog.WindowStyle
- ✅ 2个 ViewModel 实现 IDialogAware
- ✅ 2个 Dialog 已注册
- ✅ 编译成功，对话框可正常显示和关闭

---

### Day 5-6: Users 模块迁移 🔐 关键业务

**目标**：Users 模块从 50% → 100%

#### 前置检查（1小时）

1. **ViewModel 审计**
   - [ ] 检查 3个 Dialog 是否有 ViewModel 文件
   - [ ] 检查是否已实现 IDialogAware
   - [ ] 如无 ViewModel，需先创建

#### 迁移任务（每个 Dialog 约 2小时）

**ChangePasswordDialog**（2小时）

1. **视图改造**（1小时）
   - [ ] `ChangePasswordDialog.xaml`：Window → UserControl
   - [ ] 添加 prism 属性和 Dialog.WindowStyle

2. **ViewModel 处理**（1小时）
   - [ ] 检查 `ChangePasswordDialogViewModel` 是否存在
   - [ ] 如存在，验证是否实现 IDialogAware
   - [ ] 如未实现，添加 IDialogAware 接口实现

**ResetPasswordDialog**（2小时）
- [ ] 与 ChangePasswordDialog 相同步骤

**UserProfileDialog**（2小时）
- [ ] 与 ChangePasswordDialog 相同步骤

#### 模块验证（1小时）

- [ ] 验证 `UsersModule.RegisterTypes()` 中已有 RegisterDialog 调用
- [ ] 确认注册使用正确的 ViewModel 类型
- [ ] 编译测试

**验收标准**：
- ✅ 3个 Dialog 视图为 UserControl
- ✅ 3个 ViewModel 实现 IDialogAware（如存在）
- ✅ 3个 Dialog 已注册（已有）
- ✅ 编译成功

---

### Day 7: SimplifiedDialogService 移除 🗑️ 清理遗留

**目标**：完全移除 SimplifiedDialogService，统一使用 Prism IDialogService

#### 任务清单

1. **调用位置搜索**（1小时）
   - [ ] 全局搜索 `ICustomDialogService`
   - [ ] 全局搜索 `SimplifiedDialogService`
   - [ ] 列出所有调用位置（预计：PatientImportWizardViewModel + 2-3处）

2. **替换策略**（2小时）
   - [ ] MessageBox 类调用：保持不变（ShowError/ShowInfo/ShowConfirm）
   - [ ] 自定义对话框调用：替换为 `IDialogService.ShowDialog()`
   - [ ] 文件对话框：保持使用 SaveFileDialog/OpenFileDialog

3. **DI 容器移除**（30分钟）
   - [ ] 在 `ServiceCollectionExtensions.cs` 移除：
     ```csharp
     // 移除此行
     services.AddSingleton<ICustomDialogService, SimplifiedDialogService>();
     ```

4. **删除文件**（15分钟）
   - [ ] 删除 `SimplifiedDialogService.cs`
   - [ ] 删除 `ICustomDialogService.cs`（如存在）

5. **编译测试**（1小时）
   - [ ] 全解决方案编译
   - [ ] 检查无编译错误
   - [ ] 运行应用，测试所有对话框功能

**验收标准**：
- ✅ 0 处 SimplifiedDialogService 调用
- ✅ DI 容器无 SimplifiedDialogService 注册
- ✅ SimplifiedDialogService.cs 文件已删除
- ✅ 编译成功，应用正常运行

---

## 🎯 Phase 3 总体验收标准

### 代码层面

- [ ] **0 个 Window Dialog**（所有 Dialog 为 UserControl）
- [ ] **10 个 UserControl Dialog + IDialogAware**
- [ ] **10 个 RegisterDialog 调用**（分布在各模块）
- [ ] **0 处 SimplifiedDialogService 使用**

### 架构层面

- [ ] **统一使用 Prism IDialogService**
- [ ] **DialogViewModelBase 作为基类**（如需要）
- [ ] **对话框参数传递标准化**（IDialogParameters）
- [ ] **对话框结果返回标准化**（IDialogResult）

### 质量层面

- [ ] **编译成功**（0 错误）
- [ ] **功能无回归**（所有对话框正常显示和交互）
- [ ] **代码无警告**（或仅既有警告）

---

## 🏗️ 参考模板

### MedicalCase/CreateMedicalCaseDialog（标准实现）

**视图文件**（CreateMedicalCaseDialog.xaml）：

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.CreateMedicalCaseDialog"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             Width="600" Height="500">

    <prism:Dialog.WindowStyle>
        <Style TargetType="Window">
            <Setter Property="prism:Dialog.WindowStartupLocation" Value="CenterOwner" />
            <Setter Property="ShowInTaskbar" Value="False" />
            <Setter Property="ResizeMode" Value="NoResize" />
        </Style>
    </prism:Dialog.WindowStyle>

    <Grid>
        <!-- 对话框内容 -->
    </Grid>
</UserControl>
```

**ViewModel**（CreateMedicalCaseViewModel）：

```csharp
public class CreateMedicalCaseViewModel : UnifiedViewModelBase, IDialogAware
{
    // 1. 对话框标题
    public string Title { get; set; } = "创建病历";

    // 2. 对话框关闭事件
    public event Action<IDialogResult>? RequestClose;

    // 3. 对话框打开时
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 接收参数
        var patientId = parameters.GetValue<Guid>("PatientId");
        LoadPatient(patientId);
    }

    // 4. 对话框关闭时
    public void OnDialogClosed()
    {
        // 清理资源
    }

    // 5. 是否可关闭
    public bool CanCloseDialog() => true;

    // 6. 确认命令
    private void OnConfirm()
    {
        var result = new DialogResult(ButtonResult.OK, new DialogParameters
        {
            { "MedicalCase", CurrentMedicalCase }
        });
        RequestClose?.Invoke(result);
    }

    // 7. 取消命令
    private void OnCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
```

**模块注册**：

```csharp
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册对话框
        containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseViewModel>();
    }
}
```

**调用方式**：

```csharp
public class SomeViewModel : BindableBase
{
    private readonly IDialogService _dialogService;

    public SomeViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    private void ShowCreateMedicalCaseDialog()
    {
        var parameters = new DialogParameters
        {
            { "PatientId", selectedPatientId }
        };

        _dialogService.ShowDialog("CreateMedicalCaseDialog", parameters, result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var medicalCase = result.Parameters.GetValue<MedicalCaseDto>("MedicalCase");
                // 处理返回结果
            }
        });
    }
}
```

---

## 📊 工作量估算

| 任务 | 工作量 | 难度 | 风险 |
|------|--------|------|------|
| Prescriptions 完成 | 4小时 | ⭐ 低 | 🟢 低 |
| Formula 迁移 | 8小时 | ⭐⭐ 中 | 🟡 中 |
| Users 迁移 | 8小时 | ⭐⭐ 中 | 🟡 中 |
| SimplifiedDialogService 移除 | 4小时 | ⭐ 低 | 🟢 低 |
| 测试与验证 | 8小时 | ⭐⭐ 中 | 🟡 中 |
| **总计** | **32小时** | **≈ 4-5 工作日** | **中等** |

**建议工期**：7-10 工作日（含缓冲时间）

---

## ⚠️ 风险与应对

### 风险1：SelectFormulaDialog 布局问题

**描述**：从 Window 改为 UserControl 可能影响布局渲染

**应对**：
- 使用 MedicalCase 模板作为参考
- 先在独立分支测试布局
- 保留原 Window 备份

### 风险2：Users 模块 ViewModel 缺失

**描述**：3个 Dialog 可能无对应 ViewModel

**应对**：
- 先检查文件是否存在
- 如不存在，参考 MedicalCase 创建最小 ViewModel
- 优先实现基本功能，复杂验证后续迭代

### 风险3：SimplifiedDialogService 潜在依赖

**描述**：可能有未发现的业务逻辑依赖

**应对**：
- 全局搜索所有调用位置
- 逐一评估替换策略
- 保留原文件至测试通过后再删除

---

## 🔄 迁移最佳实践

### 1. 渐进式迁移

- 按模块逐一完成，避免一次性改动过大
- 每完成一个模块立即编译测试
- 保持 master 分支稳定

### 2. 向后兼容

- 迁移期间保留旧对话框调用方式
- 新旧调用方式并存，逐步替换
- 测试通过后统一移除旧代码

### 3. 代码复用

- 提取 DialogViewModelBase 基类（如 UnifiedViewModelBase 已有部分实现）
- 统一对话框样式（prism:Dialog.WindowStyle）
- 标准化命令模式（ConfirmCommand, CancelCommand）

### 4. 测试策略

- 单元测试：ViewModel 的 IDialogAware 实现
- 集成测试：对话框显示和参数传递
- 手工测试：UI 交互和布局验证

---

## 📝 提交规范

### Commit Message 格式

```
feat(prism-phase3): [模块名] Dialog 标准化 - [具体内容]

[PHASE3-STEP3.X] 详细描述

- 改动点1
- 改动点2

验收：
- ✅ 编译成功
- ✅ 功能验证通过
```

### 示例

```
feat(prism-phase3): Prescriptions 模块 Dialog 标准化完成

[PHASE3-STEP3.1.1] SelectFormulaDialog 视图迁移 + 模块注册

- SelectFormulaDialog.xaml: Window → UserControl
- 添加 prism:Dialog.WindowStyle
- PrescriptionsModule.cs: 注册 4个 Dialog

验收：
- ✅ 4个 Dialog 已注册
- ✅ 编译成功（0错误）
- ✅ 对话框显示正常
```

---

## 🔗 关联资源

### 文档

- **架构规划**：`docs/architecture/desktop-prism-refactoring-plan.md`
- **Phase 2 完成报告**：`docs/reports/issue-828-phase2-completion.md`
- **本方案**：`docs/architecture/desktop-prism-phase3-dialog-plan.md`

### Issue & PR

- **Epic Issue**：#828 - Desktop Prism Refactoring Epic
- **分支**：`feature/prism-phase3`（待创建）

### 参考实现

- **模板**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/`
  - `Views/CreateMedicalCaseDialog.xaml`
  - `ViewModels/CreateMedicalCaseViewModel.cs`
  - `MedicalCaseModule.cs`

- **已迁移**：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/`
  - `Views/FormulaTemplateDialog.xaml`（参考 UserControl 结构）
  - `ViewModels/FormulaTemplateDialogViewModel.cs`（参考 IDialogAware 实现）

---

## 🚀 启动 Phase 3

### 前置准备

1. **确认 Phase 2 已合并**
   - [ ] Phase 2 PR 已审核通过
   - [ ] `feature/prism-phase2` 已合并到 `master`
   - [ ] 本地 master 分支已同步

2. **创建 Phase 3 分支**
   ```bash
   git checkout master
   git pull origin master
   git checkout -b feature/prism-phase3
   ```

3. **环境验证**
   ```bash
   dotnet build LYBT.Desktop.sln
   # 确保编译成功
   ```

### 首日任务（Day 1）

1. **Prescriptions - SelectFormulaDialog 视图迁移**（2小时）
2. **Prescriptions - 模块注册**（1小时）
3. **编译测试**（1小时）

---

**方案结束** | **Ready to Start Phase 3** 🚀
