---
type: concept
title: 功能开关策略
created: 2026-06-10
updated: 2026-06-10
tags: [ui, feature-flag, deployment]
related: [workspace-modes, card-reader-integration]
sources: ["docs/02-requirements/11-configuration.md"]
---

# 功能开关策略

## 定义
功能开关（Feature Toggles）是一种通过配置项控制 Desktop 客户端 UI 元素可见性的技术机制。它允许运维人员或管理员在不修改代码的情况下，启用或禁用特定功能模块的前端入口，支持功能的渐进式发布、灰度测试及临时下线。

## 实现机制
- **配置源**：`FeatureToggleOptions` 类，绑定至 `appsettings.json` 中的 `FeatureToggles` 节。
- **热更新**：Desktop Shell 使用 `IOptionsMonitor<FeatureToggleOptions>` 注册，`appsettings.json` 变更时自动重载（`reloadOnChange: true`），无需重启客户端。（注: 此为 v1.1 规划，v1.0 使用 `IOptions<T>` 冻结快照，修改需重启客户端）
- **UI 绑定**：ViewModel 暴露只读布尔属性（如 `CanCreateConsultation`），XAML 通过 `BoolToVisibilityConverter` 将其转换为 `Visibility.Collapsed` 或 `Visibility.Visible`。
- **行为规则**：
  - **隐藏 (Collapsed)**：当开关为 `false` 时，对应的菜单项、工具栏按钮完全隐藏，不占用布局空间。
  - **失效**：关联的快捷键同时失效。
  - **API 解耦**：开关**仅控制 UI 层**，后端 API 端点保持开放。这意味着高级用户仍可通过直接调用 API 使用功能，因此该机制主要用于引导普通用户行为，而非作为安全边界。

## v1.0 默认状态
| 模块 | 开关项 | 默认值 | 说明 |
|------|--------|--------|------|
| **Consultation** | Create / Edit / Delete | `false` | 诊断独立 CRUD 未上线，操作集成在医案流程中 |
| **Consultation** | ViewDetail / Search | `true` | 支持在医案内查看和搜索诊断信息 |
| **Prescription** | Create / Delete | `false` | 处方独立 CRUD 未上线，操作集成在医案流程中 |
| **Prescription** | Clone / Export / ViewDetail / Search | `true` | 支持处方的克隆、导出及详情查看 |
| **MedicalCase** | 全部 (Create/Edit/Delete/Search) | `true` | 核心诊疗功能，默认全部启用 |
| **CardReader** | Enabled | `false` | 身份证读卡器功能，需硬件支持，默认关闭 |

## 与工作区模式的区别
- [工作区模式](04-workspace-modes.md)：基于用户角色（医生、前台、管理员）的业务逻辑划分，决定用户进入系统后的主界面和可用功能集。
- **功能开关**：技术层面的全局或环境级开关，用于控制特定功能点的 UI 显隐，通常用于新功能上线前的灰度测试或紧急下线。

## 关联实体
- card-reader-integration：`CardReaderEnabled` 开关控制身份证读卡器功能的 UI 入口。
- [工作区模式](04-workspace-modes.md)：两者共同作用，决定最终用户看到的界面形态。

## 热更新方案 (OQ-CFG-01)

### 当前状态

v1.0 中功能开关修改需重启 Desktop 客户端。原因：Prism DryIoc 容器在启动时创建 `Options.Create(snapshot)` 冻结快照，虽然 `ConfigurationBuilder` 已启用 `reloadOnChange: true`（`IConfiguration` 层支持文件变更检测），但下游消费者注入的 `IOptions<T>` 不会更新。

### 热更新实现路径

**推荐方案：`IOptionsMonitor<T>` 替换冻结快照**

已有基础设施：

| 组件 | 状态 | 说明 |
|------|------|------|
| `ConfigurationBuilder.reloadOnChange` | ✅ 已启用 | `appsettings.json` 文件变更时 `IConfiguration` 自动重载 |
| `IOptionsMonitor<T>` | ✅ 已实现 (v1.1) | BCL 内置，支持 `OnChange` 回调 |
| Microsoft DI 注册路径 | ⚠️ 死代码 | `ClientConfigurationExtensions.AddOptions<FeatureToggleOptions>()` 已编写但未被 Prism 容器调用 |

实施步骤：

1. **切换 DI 注册路径**：从 `PrismConfigurationExtensions.RegisterOptions<FeatureToggleOptions>()`（冻结快照）改为 `ClientConfigurationExtensions` 中的 `services.AddOptions<FeatureToggleOptions>().Bind(...)` 路径
2. **消费者改用 `IOptionsMonitor<T>`**：当前仅 2 处消费（`SyncService.OverwriteConflicts`、`PrescriptionSettingsService`），改为注入 `IOptionsMonitor<FeatureToggleOptions>` 并通过 `.CurrentValue` 读取
3. **添加 WPF 通知层**：创建 `FeatureToggleViewModel` 暴露 `INotifyPropertyChanged` 属性，通过 `IOptionsMonitor.OnChange()` 触发属性变更通知，XAML 绑定自动刷新

**前置修复**：JSON 键名绑定问题 — 当前 `"Consultation.Create": false` 点分键不绑定到 `ConsultationCreate` 属性。需改为嵌套对象格式 `"Consultation": { "Create": false }` 或添加 `[JsonPropertyName]` 映射。

### 不采用的方案

| 方案 | 原因 |
|------|------|
| FileSystemWatcher | 绕过 Options 模式，与已有 `reloadOnChange` 重复 |
| 远程 Feature Flag 服务 | 单诊所 Desktop 应用无需远程下发开关 |

## 待解决问题
- **OQ-CFG-02**: 当前无 Web UI 管理界面，需直接修改配置文件。未来可根据运维反馈决定是否增加配置管理 UI。