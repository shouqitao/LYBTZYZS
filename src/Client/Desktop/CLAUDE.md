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
