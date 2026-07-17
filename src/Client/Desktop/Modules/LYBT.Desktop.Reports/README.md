# LYBT.Desktop.Reports

统计报表模块 -- 提供诊所日常运营数据的可视化展示（当前为 STUB 实现）。

## 项目定位

报表模块为管理人员和医生提供每日运营数据概览，包括收入统计、问诊量统计和药材使用统计。当前为存根实现，仅提供基础导航和数据加载框架，后续迭代完善。

## 目录结构

```
LYBT.Desktop.Reports/
├── ReportsModule.cs              # Prism IModule 入口（STUB）
├── ViewModels/
│   └── ReportsHomeViewModel.cs
└── Views/
    └── ReportsHomeView.xaml(.cs)
```

## 核心组件

### ReportsModule

**设计依据**: Prism IModule 标准入口，当前为存根实现，仅注册基础视图和 ViewModel。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `ReportsHomeViewModel` | ViewModelLocator | 报表主页 VM |
| `ReportsHomeView` | Navigation | 导航视图 |

**模块依赖**: 无

### ReportsHomeViewModel

**设计依据**: NavigableViewModelBase 子类，三个并行 API 请求 + 日期联动刷新。

| 属性 | 类型 | 说明 |
|------|------|------|
| `DailyIncome` | `DailyIncomeDto?` | 每日收入统计 |
| `DailyConsultations` | `DailyConsultationDto?` | 每日问诊量统计 |
| `DailyHerbUsage` | `DailyHerbUsageDto?` | 每日药材使用统计 |
| `SelectedDate` | `DateTime` | 选中日期（默认今天） |
| `IsLoading` | `bool` | 加载状态 |
| `ErrorMessage` | `string?` | 错误信息 |

| 命令 | 说明 |
|------|------|
| `LoadDataCommand` | 加载三个报表数据（`Task.WhenAll` 并行） |
| `GoToTodayCommand` | 回到今天 |

- **并行加载**: `Task.WhenAll` 同时请求收入、问诊、药材三个 API
- **日期联动**: `OnSelectedDateChanged` 自动触发 `LoadDataAsync()`
- **API 路径**: `/api/v1/reports/daily/income`, `/api/v1/reports/daily/consultations`, `/api/v1/reports/daily/herbs`

## 依赖关系

```
ReportsModule
├── LYBT.Desktop.Contracts    # IApiClient (Reports 子接口), IViewModelServices
├── LYBT.Desktop.Infrastructure  # NavigableViewModelBase
└── LYBT.Shared.Models        # DailyIncomeDto, DailyConsultationDto, DailyHerbUsageDto
```

## 设计决策

1. **STUB 模块标记**: 模块代码中标注 `// STUB`，明确当前为占位实现
2. **Task.WhenAll 并行**: 三个独立 API 请求并行执行，减少总等待时间
3. **SelectedDate 自动刷新**: 通过 `partial void OnSelectedDateChanged` 响应属性变更，无需显式命令绑定
4. **IApiClient 统一入口**: 通过 `IApiClient.Reports` 子接口访问报表 API，保持客户端统一

## 已知陷阱

- **STUB 状态**: 当前模块功能不完整，服务端报表 API 可能返回空数据或未实现
- **SelectedDate 边界**: `SelectedDate.Date` 作为 `startDate`，`SelectedDate.Date.AddDays(1)` 作为 `endDate`，注意时区差异
- **错误处理**: 三个 API 独立失败不影响其他两个的展示（部分成功模式）
