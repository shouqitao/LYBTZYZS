# Proposal: 统一配置系统重构

## 概述

创建独立的配置管理项目 `LYBT.Shared.Configuration`，采用 .NET Options Pattern 统一管理整个项目的配置定义、验证和访问。

## 动机

### 当前问题

1. **配置分散**: 服务端和客户端各自维护 `appsettings.json`，配置项定义分散
2. **重复定义**: JWT 配置等在多处重复定义，存在不一致风险
3. **弱类型访问**: 使用 `ConfigurationHelper` 静态方法，缺乏编译时类型检查
4. **无验证机制**: 配置值无法在启动时验证，运行时才发现错误
5. **耦合度高**: 各模块直接依赖 `IConfiguration`，违反接口隔离原则

### 预期收益

1. **类型安全**: 强类型 Options 类，编译时检测配置错误
2. **启动验证**: 使用 `ValidateOnStart` 在应用启动时验证配置完整性
3. **职责分离**: 配置类按领域划分，遵循接口隔离原则 (ISP)
4. **统一管理**: 集中定义所有配置项，便于维护和审计
5. **变更感知**: 使用 `IOptionsMonitor<T>` 支持配置热更新

## 技术方案

### 核心架构

```
LYBT.Shared.Configuration/
├── Options/                    # 强类型配置类
│   ├── JwtOptions.cs
│   ├── DatabaseOptions.cs
│   ├── SecurityOptions.cs
│   ├── SessionOptions.cs
│   ├── LoggingOptions.cs
│   ├── ApiClientOptions.cs
│   └── FeatureToggleOptions.cs
├── Validation/                 # 自定义验证器
│   └── OptionsValidators.cs
├── Extensions/                 # 扩展方法
│   ├── ServiceCollectionExtensions.cs
│   └── ConfigurationExtensions.cs
└── Constants/                  # 配置常量
    └── ConfigurationSections.cs
```

### 技术选型

| 组件 | 选择 | 理由 |
|------|------|------|
| 配置绑定 | Microsoft.Extensions.Options | .NET 官方推荐，成熟稳定 |
| 验证框架 | DataAnnotations + IValidateOptions | 声明式验证 + 自定义逻辑 |
| 启动验证 | ValidateOnStart (.NET 8) | 启动时即发现配置错误 |
| 变更通知 | IOptionsMonitor<T> | 支持配置热更新场景 |

### Options 接口选择策略

```csharp
// 单例配置 (应用生命周期内不变)
IOptions<JwtOptions>        // JWT 密钥、发行者等

// 作用域配置 (每请求更新)
IOptionsSnapshot<T>         // 本项目暂不使用

// 变更感知配置 (运行时可能变更)
IOptionsMonitor<FeatureToggleOptions>  // 功能开关
IOptionsMonitor<LoggingOptions>        // 日志级别
```

## 影响范围

### 需要修改的模块

1. **LYBT.Shared.Utilities**: 移除 `ConfigurationHelper`，改用 Options 注入
2. **LYBT.WebAPI**: 使用扩展方法注册 Options，替换直接 `IConfiguration` 访问
3. **LYBT.Desktop.Shell**: 客户端配置统一使用 Options Pattern
4. **所有业务模块**: 通过构造函数注入 Options 接口

### 重构范围

- **appsettings.json 结构**: 重新设计，统一 Server/Client 配置格式
- **配置节名称**: 简化层级，采用更清晰的命名
- **一次性迁移**: 直接替换，不保留旧代码

## 约束条件

1. **不引入外部依赖**: 仅使用 `Microsoft.Extensions.*` 官方包
2. **保持环境变量覆盖**: 生产环境通过环境变量覆盖敏感配置

**注意**: 不考虑向后兼容性，可重新设计配置结构

## 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 迁移遗漏 | 运行时错误 | 编译时检测 + ValidateOnStart |
| Options 类设计不当 | 维护困难 | 遵循 ISP，按领域拆分 |
| 验证规则过严 | 启动失败 | 开发环境放宽验证，生产环境严格 |

## 成功标准

1. **零编译警告**: 所有配置访问使用强类型
2. **启动验证通过**: 配置不完整时应用无法启动
3. **测试覆盖**: Options 验证逻辑有单元测试
4. **文档完整**: 配置项说明文档同步更新

## 参考资料

- [Microsoft Options Pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [.NET 8 Options Validation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options#options-validation)
- [Configuration Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

---
created: 2025-12-23
status: draft
