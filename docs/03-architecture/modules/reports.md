# Reports 模块设计

> 日期: 2026-06-29
> US 数量: 3 (PRD)
> 复杂度: 低
> 状态: 草稿

## 模块概述

Reports 提供当日经营数据统计：收入、接诊量、药材使用频率。

**职责边界**:
- 当日收入统计（挂号费 + 药品费）
- 当日接诊量统计（按医生分组）
- 当日药材使用频率统计

**依赖关系**:
- 上游: MedicalCase（医案数据）、Registration（挂号费）、Prescription（药品费）
- 下游: 无
- 仅查询，无写操作

**关键 US 清单**:
RPT-001 ~ RPT-003（详见 PRD）

## 接口契约

### Service 接口（Server 端）

```csharp
public interface IReportService
{
    Task<DailyIncomeDto> GetDailyIncomeAsync(CancellationToken ct);
    Task<DailyConsultationDto> GetDailyConsultationsAsync(CancellationToken ct);
    Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(CancellationToken ct);
}
```

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| GET | `/api/v1/reports/daily/income` | GetDailyIncome | DoctorOrAdmin |
| GET | `/api/v1/reports/daily/consultations` | GetDailyConsultations | DoctorOrAdmin |
| GET | `/api/v1/reports/daily/herbs` | GetDailyHerbs | DoctorOrAdmin |

### DTO 结构

```
DailyIncomeDto
├── TotalIncome: decimal
├── RegistrationFeeTotal: decimal
└── MedicineFeeTotal: decimal

DailyConsultationDto
├── TotalCount: int
└── ByDoctor: List<DoctorCountDto>
    ├── DoctorName: string
    └── Count: int

DailyHerbUsageDto
└── Items: List<HerbUsageItemDto>
    ├── HerbName: string
    ├── UsageCount: int
    └── TotalDosage: decimal
```

## 数据流

### 获取当日收入
```
Desktop → GET /api/v1/reports/daily/income
Controller → ReportService.GetDailyIncomeAsync
  → ReportRepository.GetTodayRegistrationFeeTotalAsync
    → SUM(Registrations.RegistrationFee) WHERE CreatedAt.Date == today
  → ReportRepository.GetTodayMedicineFeeTotalAsync
    → 查找今日已完成医案 ID
    → SUM(PrescriptionItem.Amount) WHERE MedicalCaseId IN (今日IDs)
  → 合并返回 DailyIncomeDto
```

### 获取当日接诊量
```
Desktop → GET /api/v1/reports/daily/consultations
Controller → ReportService.GetDailyConsultationsAsync
  → ReportRepository.GetTodayConsultationCountAsync
    → COUNT(MedicalCases) WHERE Status=Completed AND CreatedAt.Date==today
  → ReportRepository.GetTodayConsultationsByDoctorAsync
    → GROUP BY DoctorName, ORDER BY Count DESC
  → 返回 DailyConsultationDto
```

### 获取当日药材使用
```
Desktop → GET /api/v1/reports/daily/herbs
Controller → ReportService.GetDailyHerbUsageAsync
  → ReportRepository.GetTodayHerbUsageAsync
    → JOIN MedicalCases → Prescriptions → Items
    → GROUP BY HerbName
    → 计算 UsageCount + TotalDosage
    → ORDER BY UsageCount DESC
  → 返回 DailyHerbUsageDto
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| 数据库查询失败 | SQL 异常 | 返回空数据 + 错误消息 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 仅当日 | 所有统计限当日数据 | RPT-001 |
| 软删除排除 | IsDeleted=true 的记录不计入 | RPT-002 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| MedicalCase | 查询已完成医案 | Reports → MedicalCase |
| Registration | 查询挂号费 | Reports → Registration |

**已知问题**:
- 仅支持当日统计，无历史趋势
- 无缓存，每次请求都查数据库
