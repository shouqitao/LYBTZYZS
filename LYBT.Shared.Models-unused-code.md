# LYBT.Shared.Models 待清除代码清单（全方案）

## 范围与方法
- 适用方案：`LYBT.All.sln`（Server + Desktop + Shared + Tests）。
- 方法：静态引用扫描（grep），排除 `bin/obj/TestResults` 与日志、XML 文档；不覆盖反射/XAML/字符串路由的间接引用。

## 可删除文件（未被任何项目引用）
- Enums：
  - `src/Shared/LYBT.Shared.Models/Enums/ClientEnums.cs`
  - `src/Shared/LYBT.Shared.Models/Enums/LogEnums.cs`
- Common：
  - `src/Shared/LYBT.Shared.Models/Common/EnumItem.cs`
  - `src/Shared/LYBT.Shared.Models/Common/NullableEnumItem.cs`
- Extensions：
  - `src/Shared/LYBT.Shared.Models/Extensions/DateTimeExtensions.cs`
  - `src/Shared/LYBT.Shared.Models/Extensions/EnumExtensions.cs`
  - `src/Shared/LYBT.Shared.Models/Extensions/MedicalCaseStatusExtensions.cs`
  - `src/Shared/LYBT.Shared.Models/Extensions/ServiceResultExtensions.cs`
  - `src/Shared/LYBT.Shared.Models/Extensions/StringExtensions.cs`
  - `src/Shared/LYBT.Shared.Models/Extensions/UserRoleExtensions.cs`
- Exceptions（目录内全部）：
  - `src/Shared/LYBT.Shared.Models/Exceptions/*.cs`
- Constants：
  - `src/Shared/LYBT.Shared.Models/Constants/ErrorMessageKeys.cs`
  - `src/Shared/LYBT.Shared.Models/Constants/SystemConstants.cs`
- Contracts.Common：
  - `src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponseExtensions.cs`
  - `src/Shared/LYBT.Shared.Models/Contracts/Common/OperationResultDtos.cs`
  - `src/Shared/LYBT.Shared.Models/Contracts/Common/LogCreateDto.cs`
- Contracts（目录）：
  - `src/Shared/LYBT.Shared.Models/Contracts/Compatibility/`
  - `src/Shared/LYBT.Shared.Models/Contracts/Configuration/`

## 文件内未用类型（建议精简）
- `src/Shared/LYBT.Shared.Models/Enums/SystemEnums.cs`
  - 未用：DeleteStatus/OperationResult/DataStatus/AuditStatus/PaymentStatus/PaymentMethod/WorkDay/TimeSlot/CompatibilityType/CompatibilitySeverity
  - 在用：CommonStatus（被实体/DTO使用）
- `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs`
  - 未用：AuthEventType、SecurityLevel
  - 在用：UserRole、LoginType、AuthSessionStatus

## 保留说明（在用/间接在用）
- `src/Shared/LYBT.Shared.Models/Contracts/Common/HandledError.cs`：Desktop 错误详情对话框在用。
- `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationOperationDtos.cs`：`ConsultationStartDto`/`UpdateStatusDto` 在用；`ConsultationCompleteDto` 由 `IConsultationApi` 方法签名引用（保留）。
- `src/Shared/LYBT.Shared.Models/Common/BatchIdsDto.cs`：Desktop 用户批量启/禁用在用。

## 建议的清理流程
1. 将“可删除文件”先移动至 `_archived/cleanup-YYYYMMDD/` 备份。
2. 运行 `dotnet build LYBT.All.sln -c Release` 验证无误后再删除。
3. 提交 PR，附本清单与构建日志；合并后删除归档目录。
