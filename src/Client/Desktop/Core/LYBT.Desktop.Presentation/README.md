# LYBT.Desktop.Presentation

> Desktop端UI基础设施层 | 导航/通知/主题/用户体验

## 项目定位

- **层级**: Client Core层
- **职责**: 提供UI相关技术服务(导航/通知/主题/错误处理/跨模块组件)

## 目录结构

```
LYBT.Desktop.Presentation/
├── Extensions/                   # DI注册扩展
├── Navigation/                   # 导航服务
│   └── INavigationService.cs
├── Notifications/                # 通知与错误处理
│   ├── INotificationService.cs
│   ├── NotificationService.cs
│   └── UnifiedErrorHandlingService.cs
├── Theming/                      # 主题管理
│   └── ThemeService.cs
├── UserExperience/               # 用户体验
│   └── UserExperienceService.cs
├── Components/                   # 跨模块共享组件
│   └── PatientSelector/
├── Mapping/                      # AutoMapper配置
└── DependencyInjection/          # Prism容器扩展
```

## 核心服务

| 服务 | 方法数 | 说明 |
|------|--------|------|
| INavigationService | 5 | 页面导航/返回/历史管理 |
| INotificationService | 13+2事件 | Toast通知/加载指示器/确认对话框 |
| IErrorHandlingService | 20+2事件 | 全局异常/友好消息/错误分类 |
| IThemeService | 5 | 亮色/暗色主题切换 |
| IUserExperienceService | 29 | 加载指示器/进度条/反馈系统 |

## 跨模块组件

| 组件 | 成员数 | 说明 |
|------|--------|------|
| PatientSelector | 34 | 患者选择器(搜索/选择/快速创建) |

## 错误处理分级

| 错误类别 | 用户提示 | 可重试 |
|---------|---------|--------|
| Network | "网络连接失败，请检查网络" | 是 |
| Validation | "输入数据不符合要求" | 否 |
| Business | "操作失败：{业务规则}" | 否 |
| System | "系统错误，请联系管理员" | 视情况 |

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation
- LYBT.Desktop.Contracts
- LYBT.Shared.Models
- Prism.Core/Prism.Wpf (8.x)
- AutoMapper (13.x)

### 被依赖
- LYBT.Desktop.Shell
- 所有Desktop业务模块(使用导航/通知/PatientSelector)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-10 | 从Infrastructure迁移UI服务 |
