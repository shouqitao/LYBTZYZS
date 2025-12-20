# Tasks: optimize-desktop-core

## Phase 1: 接口迁移到Contracts (约2小时)

### Task 1.1: 创建Contracts目录结构
- [ ] 在Contracts项目中创建 `Services/` 目录
- [ ] 在Contracts项目中创建 `Components/` 目录

### Task 1.2: 迁移Infrastructure.Interfaces到Contracts.Services
- [ ] 复制 `IActiveConsultationService.cs` → `Contracts/Services/`
- [ ] 复制 `IApplicationTickService.cs` → `Contracts/Services/`
- [ ] 复制 `IClinicSettingsService.cs` → `Contracts/Services/`
- [ ] 复制 `ICommonDialogService.cs` → `Contracts/Services/`
- [ ] 复制 `ICustomDialogAware.cs` → `Contracts/Services/`
- [ ] 复制 `IFeatureToggleService.cs` → `Contracts/Services/`
- [ ] 复制 `IKeyboardShortcutService.cs` → `Contracts/Services/`
- [ ] 复制 `ILoginCoordinator.cs` → `Contracts/Services/`
- [ ] 复制 `IMainWindowServicesFacade.cs` → `Contracts/Services/`
- [ ] 复制 `IPermissionService.cs` → `Contracts/Services/`
- [ ] 复制 `IPrescriptionSettingsService.cs` → `Contracts/Services/`
- [ ] 复制 `IRoleNavigationService.cs` → `Contracts/Services/`
- [ ] 复制 `ISessionManager.cs` → `Contracts/Services/`
- [ ] 复制 `IStartupPipeline.cs` → `Contracts/Services/`
- [ ] 复制 `IUserActivityTracker.cs` → `Contracts/Services/`
- [ ] 复制 `IUserNotificationService.cs` → `Contracts/Services/`
- [ ] 更新所有文件的命名空间为 `LYBT.Desktop.Contracts.Services`

### Task 1.3: 迁移Infrastructure.Interfaces.Components到Contracts.Components
- [ ] 复制 `ICommandHandler.cs` → `Contracts/Components/`
- [ ] 复制 `IComponentValidator.cs` → `Contracts/Components/`
- [ ] 复制 `IDataManager.cs` → `Contracts/Components/`
- [ ] 复制 `IValidationService.cs` → `Contracts/Components/`
- [ ] 更新所有文件的命名空间为 `LYBT.Desktop.Contracts.Components`

### Task 1.4: 更新Contracts.csproj
- [ ] 添加必要的包引用(如果接口依赖特定类型)
- [ ] 确保项目编译通过

### Task 1.5: 批量替换using语句
- [ ] 替换所有 `using LYBT.Desktop.Infrastructure.Interfaces;` → `using LYBT.Desktop.Contracts.Services;`
- [ ] 替换所有 `using LYBT.Desktop.Infrastructure.Interfaces.Components;` → `using LYBT.Desktop.Contracts.Components;`

### Task 1.6: 删除原接口目录
- [ ] 删除 `Infrastructure/Interfaces/` 目录
- [ ] 验证编译通过

### Task 1.7: Phase 1验证
- [ ] `dotnet build LYBT.All.sln` 成功
- [ ] 无命名空间错误
- [ ] 提交代码: `git commit -m "refactor(Contracts): 迁移接口定义到Contracts层"`

---

## Phase 2: Presentation合并到Infrastructure (约1.5小时)

### Task 2.1: 创建Infrastructure目标目录
- [ ] 创建 `Infrastructure/Controls/Components/` 目录
- [ ] 创建 `Infrastructure/Services/Notifications/` 目录
- [ ] 创建 `Infrastructure/Services/UserExperience/` 目录

### Task 2.2: 迁移Presentation.Components
- [ ] 移动 `HerbCardControl.xaml` → `Infrastructure/Controls/Components/`
- [ ] 移动 `HerbCardControl.xaml.cs` → `Infrastructure/Controls/Components/`
- [ ] 移动 `HerbListEditor.xaml` → `Infrastructure/Controls/Components/`
- [ ] 移动 `HerbListEditor.xaml.cs` → `Infrastructure/Controls/Components/`
- [ ] 移动 `HerbListView.xaml` → `Infrastructure/Controls/Components/`
- [ ] 移动 `HerbListView.xaml.cs` → `Infrastructure/Controls/Components/`
- [ ] 更新命名空间为 `LYBT.Desktop.Infrastructure.Controls.Components`
- [ ] 更新XAML的 `x:Class` 属性

### Task 2.3: 迁移Presentation.Notifications
- [ ] 移动 `INotificationService.cs` → `Infrastructure/Services/Notifications/`
- [ ] 移动 `NotificationService.cs` → `Infrastructure/Services/Notifications/`
- [ ] 更新命名空间为 `LYBT.Desktop.Infrastructure.Services.Notifications`

### Task 2.4: 迁移Presentation.UserExperience
- [ ] 移动 `UserExperienceService.cs` → `Infrastructure/Services/UserExperience/`
- [ ] 更新命名空间为 `LYBT.Desktop.Infrastructure.Services.UserExperience`

### Task 2.5: 迁移Presentation.Theming
- [ ] 移动 `MedicalCaseStyles.xaml` → `Infrastructure/Themes/`
- [ ] 更新资源字典引用路径

### Task 2.6: 批量替换Presentation命名空间
- [ ] 替换 `LYBT.Desktop.Presentation.Components` → `LYBT.Desktop.Infrastructure.Controls.Components`
- [ ] 替换 `LYBT.Desktop.Presentation.Notifications` → `LYBT.Desktop.Infrastructure.Services.Notifications`
- [ ] 替换 `LYBT.Desktop.Presentation.UserExperience` → `LYBT.Desktop.Infrastructure.Services.UserExperience`
- [ ] 替换 `LYBT.Desktop.Presentation.Theming` → `LYBT.Desktop.Infrastructure.Themes`

### Task 2.7: 更新XAML命名空间声明
- [ ] 更新所有引用Presentation.Components的XAML文件
- [ ] 更新xmlns声明从 `assembly=LYBT.Desktop.Presentation` → `assembly=LYBT.Desktop.Infrastructure`

### Task 2.8: Phase 2验证
- [ ] `dotnet build LYBT.All.sln` 成功
- [ ] 无XAML解析错误
- [ ] 提交代码: `git commit -m "refactor(Infrastructure): 合并Presentation内容到Infrastructure"`

---

## Phase 3: Models依赖解耦 (约1小时)

### Task 3.1: 创建IErrorMessageMapper接口
- [ ] 在Contracts.Services中创建 `IErrorMessageMapper.cs`
- [ ] 定义 `GetUserFriendlyMessage(Exception)` 方法
- [ ] 定义 `GetShortTrackingCode()` 方法

### Task 3.2: 修改ViewModelBase
- [ ] 将 `ClientErrorMessageMapper` 静态调用改为接口调用
- [ ] 添加 `protected virtual IErrorMessageMapper? GetErrorMessageMapper()` 方法
- [ ] 更新 `HandleError` 方法使用接口

### Task 3.3: 创建ErrorMessageMapper实现
- [ ] 在Infrastructure.Localization中确保 `ClientErrorMessageMapper` 实现 `IErrorMessageMapper`
- [ ] 或创建适配器类

### Task 3.4: 更新Models.csproj
- [ ] 移除对 `LYBT.Desktop.Infrastructure` 的项目引用
- [ ] 添加对 `LYBT.Desktop.Foundation` 的项目引用(如需要)
- [ ] 保留对 `LYBT.Desktop.Contracts` 的项目引用

### Task 3.5: 修复编译错误
- [ ] 解决Models中任何对Infrastructure的直接依赖
- [ ] 将必要的类型移动到Contracts或Foundation

### Task 3.6: Phase 3验证
- [ ] `dotnet build LYBT.All.sln` 成功
- [ ] Models不再依赖Infrastructure
- [ ] 提交代码: `git commit -m "refactor(Models): 解耦对Infrastructure的依赖"`

---

## Phase 4: 清理和删除Presentation项目 (约30分钟)

### Task 4.1: 更新解决方案文件
- [ ] 从 `LYBT.All.sln` 移除 `LYBT.Desktop.Presentation` 项目

### Task 4.2: 更新项目引用
- [ ] 从所有引用Presentation的项目中移除引用
- [ ] Shell.csproj
- [ ] 各Module的csproj

### Task 4.3: 删除Presentation项目
- [ ] 删除 `src/Client/Desktop/Core/LYBT.Desktop.Presentation/` 目录

### Task 4.4: 最终验证
- [ ] `dotnet build LYBT.All.sln` 成功
- [ ] `dotnet test` 所有测试通过
- [ ] 应用程序启动正常

### Task 4.5: 最终提交
- [ ] 提交代码: `git commit -m "refactor(Core): 删除Presentation项目，完成Core层优化"`

---

## 验收标准

### 功能验收
- [ ] 药材卡片(HerbCard)控件正常显示
- [ ] 药材列表(HerbList)控件正常工作
- [ ] 通知服务正常弹出消息
- [ ] 登录流程正常
- [ ] 所有业务模块功能正常

### 代码质量验收
- [ ] 无编译警告(除已知忽略项)
- [ ] 依赖方向正确(Models不依赖Infrastructure)
- [ ] 命名空间整洁一致
- [ ] 项目结构清晰

### 文档验收
- [ ] 更新各项目的README.md
- [ ] 更新CHANGELOG.md

---

## 估算工时

| Phase | 任务 | 估算 |
|-------|------|------|
| Phase 1 | 接口迁移 | 2小时 |
| Phase 2 | Presentation合并 | 1.5小时 |
| Phase 3 | Models解耦 | 1小时 |
| Phase 4 | 清理删除 | 0.5小时 |
| **总计** | | **5小时** |

---

## 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 循环依赖 | 中 | 高 | 每个Phase后验证编译 |
| XAML解析失败 | 中 | 中 | 逐个文件迁移，立即验证 |
| 运行时错误 | 低 | 高 | DI注册验证，启动测试 |
| 遗漏引用 | 中 | 低 | 全局搜索命名空间 |
