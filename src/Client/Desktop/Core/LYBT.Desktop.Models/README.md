# LYBT.Desktop.Models

> ViewModel基类与映射服务 | MVVM核心 | 状态管理

## 项目定位

- **层级**: Client Core层
- **职责**: 提供MVVM模式的ViewModel基类、对象映射服务、异常类

## 目录结构

```
LYBT.Desktop.Models/
├── Exceptions/
│   └── ApiCallException.cs       # API调用异常
├── Http/
│   └── ProblemDetails.cs         # RFC 7807标准错误响应
├── Mappers/
│   └── SimpleMapper.cs           # 简单对象映射器
├── Mapping/
│   └── MappingService.cs         # 映射服务
├── Prescriptions/
│   └── PrescriptionTemplate.cs   # 处方模板模型
└── ViewModels/Base/
    ├── ViewModelBase.cs          # ViewModel基类(核心)
    ├── UnifiedViewModelBase.cs   # 统一ViewModel基类
    └── UnifiedListViewModelBase.cs # 列表ViewModel基类
```

## ViewModelBase核心功能

| 功能类别 | 成员 | 说明 |
|----------|------|------|
| 状态属性 | IsLoading/IsBusy/HasError/ErrorMessage/StatusMessage | 统一状态管理 |
| 异步执行 | ExecuteSafelyAsync(2重载)/ExecuteSafely | 自动异常处理 |
| 错误处理 | HandleError/AddValidationError/ClearValidationErrors | 验证错误管理 |
| 资源管理 | AddDisposable/Dispose | IDisposable实现 |

## 设计特点

| 特点 | 说明 |
|------|------|
| 继承Prism.BindableBase | INotifyPropertyChanged实现 |
| INotifyDataErrorInfo | 数据验证接口 |
| IDisposable | 资源自动清理 |
| EventAggregator集成 | Prism事件订阅 |

## 依赖关系

### 依赖
- LYBT.Desktop.Infrastructure
- LYBT.Desktop.Contracts
- LYBT.Shared.Models
- Prism.Core/Prism.Wpf (8.x)
- Microsoft.Extensions.Logging (8.0.x)

### 被依赖
- 所有Desktop业务模块的ViewModel
- 所有Desktop工作站的ViewModel

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
