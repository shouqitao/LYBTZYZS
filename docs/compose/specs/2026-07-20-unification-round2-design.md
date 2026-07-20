# [S1] 二次统一设计

## 背景

上轮统一（2026-07-19）从 40→37 项目，本轮分析发现 4 项可继续统一的机会。

## 统一清单

### [S2] 1. LYBT.Shared.Validators → 合并到 Shared.Models

**现状**：`LYBT.Shared.Validators.csproj` 物理位置在 `Shared.Models/Validators/` 目录内，是嵌套子项目。8 个 validator 文件，仅依赖 FluentValidation + Shared.Models。

**变更**：
- Shared.Models.csproj 加 `<PackageReference Include="FluentValidation" />`
- 将 `Validators/` 目录下的 `.cs` 文件保留在原位（已在 Shared.Models 目录内）
- 删除 `LYBT.Shared.Validators.csproj`
- 更新引用此项目的所有 csproj（改为引用 Shared.Models）
- 删除 `Validators/bin/` 和 `Validators/obj/` 目录

**效果**：34→33 项目，消除令人困惑的嵌套项目结构。

### [S3] 2. Admin 死代码 ManagementView 清理

**现状**：Admin 和 Clinical 各有 4 个 ManagementView 薄包装（Formula/Herb/MedicalCase/Patient）。

**关键发现**：Admin 的 4 个 ManagementView 从未在 AdminModule 或 SysadminModule 中注册，也无任何代码引用 — 是死代码。只有 Clinical 实际使用这 4 个 View。

**变更**：
- 删除 Admin 项目中 4 个未使用的 ManagementView（XAML + code-behind）：
  - `Views/FormulaManagementView.xaml` + `.xaml.cs`
  - `Views/HerbManagementView.xaml` + `.xaml.cs`
  - `Views/MedicalCaseManagementView.xaml` + `.xaml.cs`
  - `Views/PatientManagementView.xaml` + `.xaml.cs`
- Clinical 保留不变

**效果**：减少 8 个死文件，Admin 项目更精简。

### [S4] 3. Foundation vs Infrastructure 边界文档

**现状**：两个项目都有 Http/Security/Repositories 文件夹，职责边界不清晰。

**变更**：
- 在 `LYBT.Desktop.Foundation/AGENTS.md` 中明确：无头运行时层，不依赖 WPF
- 在 `LYBT.Desktop.Infrastructure/AGENTS.md` 中明确：WPF/MVVM 基础设施层
- 列出各文件夹归属规则

### [S5] 4. OperationResultDto vs Result<T> 命名澄清

**现状**：OperationResultDto（API 响应 DTO，带 OperationTime）和 Result<T>（领域结果模式）命名易混淆。

**变更**：
- 在 OperationResultDto 类的 XML 注释中明确：这是 API 批量操作的响应 DTO，非领域结果模式
- 在 Result<T> 类的 XML 注释中引用：这是领域操作结果，参见 OperationResultDto 了解 API 响应结构

## 依赖关系

1. Validators 合并 → 无依赖，可独立执行
2. Admin 死代码清理 → 无依赖，可独立执行
3. 边界文档 → 无依赖
4. 命名澄清 → 无依赖

## 验证

- `dotnet build LYBTZYZS.sln` 通过
- 所有测试通过
- 项目数从 34 降到 33
