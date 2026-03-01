# LYBT.Shared.Configuration

> 强类型配置 Options | Server/Client 双端 | 启动时验证

## 项目定位

- **层级**: Shared
- **职责**: 集中管理 Server 和 Client 的强类型配置选项，提供配置节注册扩展和启动时验证
- **状态**: Active

## 目录结构

```
LYBT.Shared.Configuration/
├── Constants/
│   └── ConfigurationSections.cs    # 配置节名称常量
├── Extensions/
│   ├── ClientConfigurationExtensions.cs  # 客户端配置注册
│   └── ServerConfigurationExtensions.cs  # 服务端配置注册
├── Options/
│   ├── Client/                     # 客户端 Options
│   │   ├── ApiClientOptions.cs     # API 连接配置
│   │   ├── ClientSessionOptions.cs # 客户端会话
│   │   ├── ClinicSettingsOptions.cs # 诊所设置
│   │   ├── FeatureToggleOptions.cs # 功能开关
│   │   ├── PrescriptionOptions.cs  # 处方配置
│   │   └── SyncOptions.cs         # 同步配置
│   ├── Common/
│   │   └── JwtOptions.cs          # JWT 配置 (双端共享)
│   └── Server/                    # 服务端 Options (11个)
└── Validation/                    # Options 验证器
    ├── DatabaseOptionsValidator.cs
    ├── JwtOptionsValidator.cs
    └── SecurityOptionsValidator.cs
```

## 核心接口

| 名称 | 说明 |
|------|------|
| ConfigurationSections | 配置节名称常量 (避免魔法字符串) |
| ServerConfigurationExtensions | 服务端 Options 批量注册 |
| ClientConfigurationExtensions | 客户端 Options 批量注册 |
| *OptionsValidator | IValidateOptions 启动时验证 |

## 设计依据

- Options Pattern (IOptions/IOptionsSnapshot) 实现强类型配置
- Server/Client 分离但共享 JwtOptions，支持双模式架构
- 启动时验证 (ValidateOnStart) 快速失败，避免运行时配置错误

## 依赖关系

### 依赖
- Microsoft.Extensions.Options (NuGet)

### 被依赖
- LYBT.Infrastructure (服务端配置注册)
- LYBT.WebAPI (服务端配置注册)
- LYBT.Module.Auth (JWT 配置)
- LYBT.Desktop.Foundation (客户端配置注册)
- LYBT.Desktop.LocalData (同步/API 配置)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建 README |
| 2026-01 | 添加 MemoryCacheOptions |
| 2025-12 | Options 验证器体系建立 |
