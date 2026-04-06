# Desktop 层

> WPF + Prism.DryIoc 桌面客户端，基于 .NET 8.0 的模块化 MVVM 架构

## 架构概览

Desktop 层采用三层 MVVM + 模块化架构: View (XAML) <- 绑定 -> ViewModel -> Service -> API。
Shell 负责启动和模块加载，Core 提供基础设施（契约、本地数据、打印），
Modules 实现业务功能，Roles 提供角色驱动的工作台导航。

通过 Refit 类型安全 HTTP 客户端与 Server 层通信，JWT 认证自动注入。

## 项目列表

| 项目 | 职责 | 状态 |
|------|------|------|
| Shell | 应用启动壳、主窗口、模块加载容器 | 稳定 |
| LYBT.Desktop.Contracts | 核心接口和服务契约定义 | 稳定 |
| LYBT.Desktop.Foundation | 基础设施（MVVM基类、转换器、行为） | 稳定 |
| LYBT.Desktop.Infrastructure | UI组件（自定义控件、样式资源） | 稳定 |
| LYBT.Desktop.Models | 桌面端视图模型和数据模型 | 稳定 |
| LYBT.Desktop.Utilities | 通用工具类和扩展方法 | 稳定 |
| LYBT.Desktop.LocalData | SQLite 本地数据访问 | 稳定 |
| LYBT.Desktop.Printing | 打印服务（A4模板、处方打印） | 稳定 |
| LYBT.Desktop.CardReader | 身份证读卡器集成 | 稳定 |
| LYBT.Desktop.Auth | 登录界面、权限验证 | 稳定 |
| LYBT.Desktop.Users | 用户管理界面 | 稳定 |
| LYBT.Desktop.Patients | 患者档案管理界面 | 稳定 |
| LYBT.Desktop.MedicalCase | 医案诊疗流程界面 | 稳定 |
| LYBT.Desktop.Herbs | 中药材信息维护界面 | 稳定 |
| LYBT.Desktop.Formula | 验方模板管理界面 | 稳定 |
| LYBT.Desktop.Sync | 数据同步模块 | 开发中 |
| LYBT.Desktop.Admin | 系统管理工作台 | 稳定 |
| LYBT.Desktop.Clinical | 诊疗工作台 | 稳定 |

## 目录结构

```
src/Client/Desktop/
├── Shell/                  # 应用启动壳
├── Core/
│   ├── LYBT.Desktop.Contracts/
│   ├── LYBT.Desktop.Foundation/
│   ├── LYBT.Desktop.Infrastructure/
│   ├── LYBT.Desktop.Models/
│   ├── LYBT.Desktop.Utilities/
│   ├── LYBT.Desktop.LocalData/
│   ├── LYBT.Desktop.Printing/
│   └── LYBT.Desktop.CardReader/
├── Modules/
│   ├── LYBT.Desktop.Auth/
│   ├── LYBT.Desktop.Users/
│   ├── LYBT.Desktop.Patients/
│   ├── LYBT.Desktop.MedicalCase/
│   ├── LYBT.Desktop.Herbs/
│   ├── LYBT.Desktop.Formula/
│   └── LYBT.Desktop.Sync/
├── Roles/
│   ├── LYBT.Desktop.Admin/
│   └── LYBT.Desktop.Clinical/
└── Resources/
```

## 依赖关系

```
Shell -> Modules -> Core (Foundation/Contracts/Infrastructure)
      -> Roles   -> Core
Modules/Roles -> Shared.Models (DTO契约)
```

- **上游**: 调用 Server 层 API (Refit HTTP 客户端)
- **平级**: 依赖 Shared 层的 DTO、枚举、工具类

## 快速启动

```bash
# 先启动后端
dotnet run --project src/Server/Services/LYBT.WebAPI
# 启动桌面客户端
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简 README，详细内容迁移至 CLAUDE.md |
| 2025-12-04 | 按 README 规范重写文档 |

## 开发笔记

# Desktop 层开发指南

## 技术栈

- .NET 8.0 Windows + WPF + Prism.DryIoc 8.1.97
- Refit (类型安全 REST 客户端) | Riok.Mapperly (对象映射)
- MVVM DataBinding + DelegateCommand + EventAggregator

## MVVM 规范

- ViewModel 命名: `{Function}ViewModel`
- 依赖注入: 构造函数注入，Prism DryIoc 容器
- 异步操作: async/await，避免阻塞 UI 线程
- 数据绑定: 双向绑定 + INotifyPropertyChanged

## Refit API 客户端

```csharp
// 注册示例
services.AddRefitClient<IAuthApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:7001"))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
```

### API 客户端接口

| 接口 | 功能 |
|------|------|
| IAuthApi | 登录、登出、Token刷新 |
| IUserApi | 用户 CRUD |
| IPatientApi | 患者搜索、档案管理 |
| IMedicalCaseApi | 医案流程管理 |
| IHerbApi | 药材搜索、价格查询 |
| IFormulaApi | 验方模板管理 |
| ISyncApi | 数据同步 |

## JWT 认证流程

1. `IAuthApi.LoginAsync()` 获取 AccessToken + RefreshToken
2. Token 存储到用户配置
3. `AuthorizationMessageHandler` 自动注入 `Authorization: Bearer {token}`
4. 过期时自动调用 `RefreshTokenAsync()`
5. `SessionManager` 统一管理登录状态和权限

## 错误处理策略

- 网络错误: 自动重试 3 次，指数退避
- 认证失败: 跳转登录界面，清除本地会话
- 服务器错误: 用户友好提示 + 详细日志
- 超时: 30 秒请求超时，长操作支持取消

## 角色体系

- **Admin**: 系统配置、用户管理、数据导入导出
- **Doctor**: 患者档案、诊疗记录、处方开具、验方管理

## 开发注意事项

- Desktop 测试需要 `net8.0-windows` 目标框架，不能和 Server 测试混在同一项目
- Riok.Mapperly 生成映射代码需 partial class，与 CommunityToolkit.Mvvm 的 [ObservableProperty] 共存时需注意生成顺序
- Pack URI 用于资源文件统一引用
- Shell 中的 Logger 注册需与 Modules 同步清理
