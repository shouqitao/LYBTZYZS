# LYBT.Desktop.Infrastructure

> WPF基础设施库 | 会话管理/控件/转换器/事件

## 项目定位

- **层级**: Client Core层
- **职责**: 提供WPF专属的UI基础设施(Controls/Converters/Events/Services)

## 目录结构

```
LYBT.Desktop.Infrastructure/
├── Commands/                     # 全局命令
├── Constants/                    # 常量定义(3文件)
├── Controls/                     # 自定义控件(7个)
├── Converters/                   # 数据转换器(13个)
├── Events/                       # Prism事件(11个)
├── Extensions/                   # 扩展方法(2文件)
├── Helpers/                      # 辅助类(3文件)
├── Interfaces/                   # 服务接口(11个)
├── Repositories/                 # 仓储基类
├── Services/                     # 核心服务(8个)
│   ├── ErrorHandling/
│   ├── Navigation/
│   ├── SessionManager.cs
│   └── ...
└── Templates/                    # T4模板
```

## 核心服务

| 服务 | 成员数 | 说明 |
|------|--------|------|
| ISessionManager | 27 | 用户会话/权限检查/会话事件 |
| ErrorHandlingService | 13 | 全局异常处理/友好消息 |
| EnhancedNavigationService | 6 | 页面导航/历史管理 |
| UserNotificationService | 8 | Toast通知/确认对话框 |
| KeyboardShortcutService | 11 | 全局快捷键注册 |
| FeatureToggleService | 2 | 功能开关 |

## 自定义控件

| 控件 | 说明 |
|------|------|
| VirtualizedDataGrid | 虚拟化数据网格(支持10000+行) |
| VirtualizedListView | 虚拟化列表视图 |
| GlobalStatusBar | 全局状态栏 |
| LoginStatusControl | 登录状态控件 |
| ErrorNotificationControl | 错误通知控件 |

## 数据转换器

| 转换器 | 说明 |
|--------|------|
| BooleanToVisibilityConverter | 布尔值→可见性 |
| DateTimeFormatConverter | 日期时间格式化 |
| EnumDescriptionConverter | 枚举→描述文本 |
| StatusToColorConverter | 状态→颜色 |
| NullToVisibilityConverter | 空值→可见性 |

## Prism事件

| 事件 | 说明 |
|------|------|
| PatientSelectedEvent | 患者选中 |
| LoginSuccessEvent | 登录成功 |
| LogoutEvent | 登出 |
| PrescriptionCompletedEvent | 处方完成 |
| DataRefreshEvent | 数据刷新 |

## 设计依据

- 与Foundation分层: Foundation提供平台无关能力，Infrastructure提供WPF专属基础设施，职责边界清晰
- SessionManager集中管理用户会话和权限状态，避免各模块各自维护登录状态导致的不一致
- 自定义控件(VirtualizedDataGrid等)封装WPF虚拟化技术，使业务模块无需关心大数据量渲染性能
- Prism事件总线实现模块间松耦合通信，模块通过发布/订阅事件交互而非直接引用

## 依赖关系

### 依赖
- LYBT.Shared.Models
- LYBT.Desktop.Foundation
- Prism.Core/Prism.Wpf (8.x)
- NPOI (Excel操作)

### 被依赖
- LYBT.Desktop.Shell
- 所有Desktop业务模块
- 所有Desktop工作站

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
