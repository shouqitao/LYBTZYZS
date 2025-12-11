# desktop-structure-cleanup Specification

## Purpose
TBD - created by archiving change cleanup-desktop-empty-directories. Update Purpose after archive.
## Requirements
### Requirement: DESKTOP-CLEANUP-001 删除空模块目录

Desktop层 SHALL 删除完全空的模块目录。

#### Scenario: 删除LYBT.Desktop.Admin
- **WHEN** 清理Desktop模块目录
- **THEN** SHALL 删除 `src/Client/Desktop/Modules/LYBT.Desktop.Admin/` 目录
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: DESKTOP-CLEANUP-002 删除空Core子目录

Desktop层 SHALL 删除未使用的空Core子目录。

#### Scenario: 删除LYBT.Desktop.Services
- **WHEN** 清理Desktop Core目录
- **THEN** SHALL 删除 `src/Client/Desktop/Core/LYBT.Desktop.Services/` 目录

#### Scenario: 删除Infrastructure/Enums
- **WHEN** 清理Desktop Infrastructure目录
- **THEN** SHALL 删除 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Enums/` 目录

---

### Requirement: DESKTOP-CLEANUP-003 接口文件组织规范

每个Desktop模块的接口文件 SHALL 放置在 `Interfaces/` 子目录中。

#### Scenario: Prescriptions模块接口整理
- **WHEN** 整理Prescriptions模块
- **THEN** SHALL 创建 `LYBT.Desktop.Prescriptions/Interfaces/` 目录
- **AND** SHALL 移动 `IPrescriptionPrintService.cs` 到 `Interfaces/`
- **AND** SHALL 更新namespace为 `LYBT.Desktop.Prescriptions.Interfaces`

#### Scenario: Auth模块接口整理
- **WHEN** 整理Auth模块
- **THEN** SHALL 创建 `LYBT.Desktop.Auth/Interfaces/` 目录
- **AND** SHALL 移动 `IConnectionSettingsService.cs` 到 `Interfaces/`
- **AND** SHALL 更新namespace为 `LYBT.Desktop.Auth.Interfaces`

#### Scenario: Patients模块接口整理
- **WHEN** 整理Patients模块
- **THEN** SHALL 移动 `Services/IPatientSearchCache.cs` 到 `Interfaces/`
- **AND** SHALL 更新namespace为 `LYBT.Desktop.Patients.Interfaces`
- **AND** 编译 SHALL 通过，无错误

