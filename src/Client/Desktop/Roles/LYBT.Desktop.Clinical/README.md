# LYBT.Desktop.Clinical

> 临床医生角色模块 | 诊疗工作台 | 今日统计 + 开始看诊

## 项目定位

- **层级**: Desktop Roles
- **职责**: 提供临床医生角色专属工作台,核心功能为"开始看诊"入口,展示今日接诊统计和待处理病案数量,优化医生日常诊疗工作效率
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Clinical/
├── ClinicalModule.cs              # Prism模块注册
├── ViewModels/
│   └── ClinicalHomeViewModel.cs   # 医生工作台ViewModel(统计+看诊)
└── Views/
    ├── ClinicalHomeView.xaml       # 医生工作台视图
    └── ClinicalHomeView.xaml.cs    # 视图后置代码
```

## 核心组件

| 名称 | 说明 |
|------|------|
| ClinicalModule | Prism模块注册,自动发现Views和ViewModels |
| ClinicalHomeViewModel | 今日统计(TodayConsultationCount/PendingCaseCount) + StartMedicalCaseCommand |
| ClinicalHomeView | 医生工作台UI,包含统计卡片和"开始看诊"按钮 |

## 设计依据

临床工作台采用"统计仪表盘 + 核心操作"模式:顶部展示今日接诊数和待处理病案的实时统计,中部提供"开始看诊"核心按钮。与Admin模块的多导航枢纽不同,Clinical模块聚焦单一诊疗流程入口,减少医生操作步骤。统计数据在OnNavigatedTo时自动刷新,确保每次进入页面看到最新数据。

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (Desktop端基础类型和接口)
- LYBT.Desktop.Infrastructure (区域管理、导航服务)
- LYBT.Desktop.Models (ViewModelBase基类)
- LYBT.Desktop.Contracts (区域名称常量)
- LYBT.Shared.Models (共享DTO模型,病案数据)

### 被依赖
- LYBT.Desktop.Shell (Prism模块注册,加载临床医生模块)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 按精简规范重写README,代码示例迁移至CLAUDE.md |
| 2025-10-29 | 初始版本 |
