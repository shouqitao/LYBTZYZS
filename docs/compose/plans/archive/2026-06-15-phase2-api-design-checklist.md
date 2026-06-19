# Phase 2 API 端点设计 — 详细任务清单

> 日期: 2026-06-15
> 状态: 待用户确认

## 背景

Phase 1 清理后，当前状态：
- Server WebAPI：66 个端点（11 个 Controller）
- LocalWebAPI：84 个端点（10 个 Controller）
- Registration 实体：无 QueueNumber、无 RegistrationFee
- Reports：零代码
- 遗留：FormulasController 仍有 export/import-template（Phase 1 遗漏）

---

## 区域 1：Registration 加字段

### 修改文件

| 文件 | 改动 |
|------|------|
| `src/Server/Core/LYBT.Entities/Registrations/RegistrationModel.cs` | 新增 `QueueNumber` (int) + `RegistrationFee` (decimal) 字段 |
| `src/Shared/LYBT.Shared.Models/Contracts/Registration/RegistrationInputDto.cs` | 新增 `RegistrationFee` 字段（QueueNumber 由系统生成） |
| `src/Shared/LYBT.Shared.Models/Contracts/Registration/RegistrationDetailDto.cs` | 新增 `QueueNumber` + `RegistrationFee` 字段 |
| `src/Shared/LYBT.Shared.Models/Contracts/Registration/RegistrationListDto.cs` | 新增 `QueueNumber` + `RegistrationFee` 字段 |
| `src/Server/Modules/LYBT.Module.Registration/Services/RegistrationService.cs` | 创建挂号时自动生成 QueueNumber（当日最大号+1） |
| `src/Server/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs` | 新增 `GetTodayMaxQueueNumberAsync` 方法 |
| `src/Server/Modules/LYBT.Module.Registration/Interfaces/IRegistrationRepository.cs` | 新增方法签名 |
| `src/Client/Desktop/LocalWebAPI/Mappers/LocalApiMapper.cs` | 更新 Registration 映射 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Registration/` 相关文件 | 更新 Desktop 端 DTO 映射 |

### 业务规则

- QueueNumber：当日顺序号，创建时自动生成（当天最大号+1）
- RegistrationFee：创建挂号时由前台输入，单位：元
- QueueNumber 在挂号列表中用于排序显示

---

## 区域 2：新建日报统计模块

### 新建文件

| 文件 | 说明 |
|------|------|
| `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs` | 日报统计控制器 |
| `src/Server/Modules/LYBT.Module.Reports/ReportsModule.cs` | 模块注册 |
| `src/Server/Modules/LYBT.Module.Reports/Interfaces/IReportService.cs` | 报表服务接口 |
| `src/Server/Modules/LYBT.Module.Reports/Services/ReportService.cs` | 报表服务实现 |
| `src/Shared/LYBT.Shared.Models/Contracts/Reports/DailyIncomeDto.cs` | 当日收入 DTO |
| `src/Shared/LYBT.Shared.Models/Contracts/Reports/DailyConsultationDto.cs` | 当日看诊量 DTO |
| `src/Shared/LYBT.Shared.Models/Contracts/Reports/DailyHerbUsageDto.cs` | 当日药材频次 DTO |

### 端点设计

| 端点 | 方法 | 说明 |
|------|------|------|
| `GET /api/v1/reports/daily/income` | GET | 当日收入（挂号费合计 + 处方药费合计） |
| `GET /api/v1/reports/daily/consultations` | GET | 当日看诊量（总数 + 按医生分组） |
| `GET /api/v1/reports/daily/herbs` | GET | 当日药材使用频次（按药材名分组，降序） |

### 查询逻辑

**收入**：
```sql
-- 挂号费
SELECT SUM(RegistrationFee) FROM Registrations WHERE DATE(CreatedAt) = TODAY AND IsDeleted = 0
-- 处方药费
SELECT SUM(Amount) FROM PrescriptionItems WHERE PrescriptionId IN 
  (SELECT Id FROM Prescriptions WHERE MedicalCaseId IN 
    (SELECT Id FROM MedicalCases WHERE DATE(CompletedAt) = TODAY AND IsDeleted = 0))
```

**看诊量**：
```sql
-- 总数
SELECT COUNT(*) FROM MedicalCases WHERE DATE(CompletedAt) = TODAY AND IsDeleted = 0
-- 按医生
SELECT DoctorName, COUNT(*) FROM MedicalCases WHERE DATE(CompletedAt) = TODAY AND IsDeleted = 0 GROUP BY DoctorName
```

**药材频次**：
```sql
SELECT h.Name, COUNT(*) as UsageCount 
FROM PrescriptionItems pi 
JOIN Herbs h ON pi.HerbId = h.Id
WHERE pi.PrescriptionId IN (同上)
GROUP BY h.Name ORDER BY UsageCount DESC
```

### LocalWebAPI 同步

| 文件 | 改动 |
|------|------|
| `src/Client/Desktop/LocalWebAPI/Controllers/ReportsController.cs` | 新建本地报表控制器（复用 ReportService） |
| `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` | 注册 ReportsModule |

---

## 区域 3：清理遗留端点

### 3.1 FormulasController 遗留（Phase 1 遗漏）

| 文件 | 移除端点 |
|------|---------|
| `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs` | `GET /export` + `GET /import-template` |
| `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs` | `GET /export` + `GET /import-template` |

### 3.2 LocalWebAPI 多余端点

| 文件 | 移除端点 | 原因 |
|------|---------|------|
| `LocalWebAPI/Controllers/AuthController.cs` | `POST /refresh` | Phase 1 已简化 auth（无 refresh token） |
| `LocalWebAPI/Controllers/HerbsController.cs` | `GET /export` + `GET /export-all` | Excel 导出已移除 |
| `LocalWebAPI/Controllers/FormulasController.cs` | `GET /export` + `GET /import-template` | Excel 导出已移除 |

### 3.3 Server HerbsController 遗留

| 文件 | 移除端点 |
|------|---------|
| `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs` | 检查是否还有 export/template 端点 |

---

## 执行顺序

| 步骤 | 区域 | 说明 |
|------|------|------|
| 1 | 区域 1 | Registration 加字段（QueueNumber + RegistrationFee） |
| 2 | 区域 3 | 清理遗留端点（Formulas export/import-template） |
| 3 | 区域 2 | 新建日报统计模块 |
| 4 | 验证 | `dotnet build` + `dotnet test` |

## 验证

每个区域完成后：
1. `dotnet build LYBTZYZS.sln` — 编译通过
2. `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Validator"` — 验证器测试通过
