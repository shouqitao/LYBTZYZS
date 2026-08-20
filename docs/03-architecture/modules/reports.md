# Reports 模块设计

> 复杂度: 低 | 状态: 草稿

## 概述

Reports 提供当日经营数据统计：收入、接诊量、药材使用频率。仅查询，无写操作。

**依赖**: 上游 MedicalCase+Registration+Prescription。

## API 端点

| HTTP | 路由 | 权限 |
|------|------|------|
| GET | `/api/v1/reports/daily/income` | DoctorOrAdmin |
| GET | `/api/v1/reports/daily/consultations` | DoctorOrAdmin |
| GET | `/api/v1/reports/daily/herbs` | DoctorOrAdmin |

## Service 接口

```csharp
public interface IReportService
{
    Task<DailyIncomeDto> GetDailyIncomeAsync(CancellationToken ct);
    Task<DailyConsultationDto> GetDailyConsultationsAsync(CancellationToken ct);
    Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(CancellationToken ct);
}
```

## 数据流

- **收入**: SUM(RegistrationFee) + SUM(PrescriptionItem.Amount) WHERE 当日
- **接诊量**: COUNT(MedicalCases WHERE Completed AND 当日) GROUP BY DoctorName
- **药材使用**: JOIN MedicalCases→Prescriptions→Items GROUP BY HerbName

## 业务规则

| 规则 | 描述 |
|------|------|
| 仅当日 | 所有统计限当日数据 |
| 软删除排除 | IsDeleted=true 不计入 |

## 已知问题

- 仅支持当日统计，无历史趋势
- 无缓存，每次请求查数据库
