# Configuration (配置管理)

> 版本: v1.0 | 日期: 2026-06-28 | 状态: ✅ 已完成
> Split from 11-platform.md (2026-06-28)

## 模块概述

配置模块基于 ASP.NET Core Options 模式，提供强类型绑定 + DataAnnotation 验证 + 分环境覆盖 + 生产启动验证。运行时配置查询通过 `ConfigurationController`（`AdminOrSuperAdmin`）暴露。

> 原 4 US，保留 **4 US**：US-CFG-001~004。

---

### US-CFG-001: 查询所有配置

**角色**: 超级管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 通过 API 查询系统全部配置项，**以便** 在不接触服务器文件的情况下掌握当前运行参数。

**验收标准**:
- [ ] 非 SuperAdmin → 403
- [ ] 返回当前生效的全部配置（合并 appsettings + 环境变量）
- [ ] 敏感字段（密钥/密码）不明文返回

**业务规则**:
1. 端点受 `AdminOrSuperAdmin` 策略保护。
2. 配置节名称通过 `ConfigurationSections` 常量统一管理。
3. 敏感配置（SecretKey/Password）脱敏展示。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET` 服务端配置 |
| 本地 | 完全一致（LocalWebAPI 复用 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs:15`、`ISystemConfigurationService`

---

### US-CFG-002: 查询单个配置

**角色**: 超级管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 按配置节名查询单个配置详情，**以便** 精确定位需要调整的参数。

**验收标准**:
- [ ] 指定配置节名 → 返回该节的强类型配置对象
- [ ] 节名不存在 → 返回 404
- [ ] 非 SuperAdmin → 403

**业务规则**:
1. 支持 14 个 Options 类（8 服务端 + 1 共享 + 4 客户端 + 1 WebAPI）。
2. 客户端 `ClinicSettings`（Name/Department/Address/Phone）驱动处方打印标题区。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端读取 |
| 本地 | 完全一致 |

**实现参考**: `ConfigurationController.cs:15`、Options 类（`LYBT.Shared.Configuration`）

---

### US-CFG-003: 验证生产配置

**角色**: 运维人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 生产环境启动时自动验证关键配置项，**以便** 配置缺失时立即报错并给出修复指引，而非运行时出现莫名异常。

**验收标准**:
- [ ] Production + 连接字符串缺失 → 启动失败，退出码 1
- [ ] Production + JWT 密钥过短 → 启动失败
- [ ] Development + 连接字符串缺失 → 正常启动（不触发验证）
- [ ] 错误输出含具体配置路径与对应环境变量设置命令

**业务规则**:
1. 仅 `ASPNETCORE_ENVIRONMENT=Production` 时触发 `ProductionConfigurationValidator`。
2. 严重级别：Critical（连接字符串/JWT 密钥）阻止启动；Important（默认密码/SystemAdmin）警告；Optional（AllowedHosts）建议。
3. 验证失败 → 控制台输出 + Fatal 日志 + `Environment.Exit(1)`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端启动时验证 |
| 本地 | 不适用 |

**实现参考**: `ProductionConfigurationValidator`、`Program.cs`

---

### US-CFG-004: 功能开关

**角色**: 运维人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 通过 `FeatureToggles` 控制 Desktop 功能可见性与行为策略，**以便** 分阶段发布功能或调整处理策略。

**验收标准**:
- [ ] `FeatureToggle=false` → 对应功能按钮/菜单项完全隐藏（Collapsed）
- [ ] `OverwriteConflicts=false` → 同步冲突时不自动覆盖
- [x] `ConfigurationOptionsMonitor` 支持文件变更时自动刷新（热更新），无需重启 Desktop

**业务规则**:
1. v1.0 `FeatureToggleOptions` 仅含 `OverwriteConflicts` 与 `DuplicateHerbMergeStrategy`（"Max"）。
2. 早期定义的 18 个布尔开关已废弃（2026-06-28 清理），UI 可见性由各模块 ViewModel 按角色/业务状态自管。
3. FeatureToggle 仅控 UI 层，API 端点不受影响。
4. FeatureToggle 通过 `ConfigurationOptionsMonitor<T>` + `OptionsMonitorWrapper<T>`（`PrismConfigurationExtensions.cs`）实现热更新——配置文件保存后自动生效，无需重启。其他配置（JWT/ApiClient/ClinicSettings/CardReader）仍需重启。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 客户端 appsettings.json 配置 API 连接 |
| 本地 | `ApiClient` 配置无效，使用本地数据源 |

**实现参考**: `FeatureToggleOptions`、`ConfigurationOptionsMonitor`（`PrismConfigurationExtensions.cs:80`）、`OptionsMonitorWrapper`（`PrismConfigurationExtensions.cs:133`）

---

## 变更记录

| 版本 | 日期 | 变更 | 原因 |
|------|------|------|------|
| v1.0 | 2026-06-28 | Split from 11-platform.md into focused module | 文档结构优化 S4 批次 3 |
