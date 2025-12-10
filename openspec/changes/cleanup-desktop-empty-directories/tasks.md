# Tasks for cleanup-desktop-empty-directories

## Phase 1: 删除空目录

- [x] TASK-1: 删除 `src/Client/Desktop/Modules/LYBT.Desktop.Admin/` 空目录
- [x] TASK-2: 删除 `src/Client/Desktop/Core/LYBT.Desktop.Services/` 空目录
- [x] TASK-3: 删除 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Enums/` 空目录

## Phase 2: 整理Prescriptions模块接口

- [x] TASK-4: 创建 `LYBT.Desktop.Prescriptions/Interfaces/` 目录
- [x] TASK-5: 移动 `Services/IPrescriptionPrintService.cs` → `Interfaces/`
- [x] TASK-6: 更新namespace和引用

## Phase 3: 整理Auth模块接口

- [x] TASK-7: 创建 `LYBT.Desktop.Auth/Interfaces/` 目录
- [x] TASK-8: 移动 `Services/IConnectionSettingsService.cs` → `Interfaces/`
- [x] TASK-9: 更新namespace和引用

## Phase 4: 整理Patients模块接口

- [x] TASK-10: 移动 `Services/IPatientSearchCache.cs` → `Interfaces/`
- [x] TASK-11: 更新namespace和引用

## Phase 5: 验证

- [x] TASK-12: 验证Desktop解决方案编译通过
- [x] TASK-13: 提交变更到Git

## Summary

| 模块 | 操作 | 文件 |
|------|------|------|
| Prescriptions | 创建Interfaces + 移动 | IPrescriptionPrintService.cs |
| Auth | 创建Interfaces + 移动 | IConnectionSettingsService.cs |
| Patients | 移动（Interfaces已存在） | IPatientSearchCache.cs |

## Validation Criteria

1. 所有空目录已删除
2. 接口文件位于正确的Interfaces文件夹
3. Namespace与文件路径一致
4. `dotnet build LYBT.Desktop.sln` 成功

## Bonus: 解决方案清理

移除了LYBT.Desktop.sln中两个不存在的项目引用：
- LYBT.Desktop.AdminWorkstation
- LYBT.Desktop.ClinicalWorkstation
