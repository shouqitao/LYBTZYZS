# 阶段 C：配置与热更新精简 PRD

## 目标
- 在 Record‑Only 模式下，简化/解耦复杂特性开关与热更新逻辑，保留必要的配置变更响应。

## 范围
- In Scope：`FeatureToggleService` 与 `HotReloadService` 的依赖关系梳理与最小化；注册与使用点的调整。
- Out of Scope：后端配置分发机制；配置文件存储位置与 CI/CD。

## 交付物
- 精简后的 `FeatureToggleService`（或 Null 实现）：仅提供 `IsEnabled`/`GetFeatureConfiguration` 的最小能力（从 IConfiguration 读取）；去除动态注册/变更监听/A-B 等接口。
- `HotReloadService` 不再强依赖 FeatureToggle，可独立监听配置文件或由外部触发。

## 验收标准
- 编译通过；`HotReloadService.Start/Stop/TriggerReload` 可运行。
- 移除对废弃 API 的编译依赖；保留最小变更回调能力。

## 里程碑
1. 引入 `NullFeatureToggleService`，并在容器注册中切换默认实现。
2. 改造 `HotReloadService` 仅依赖 `IConfigurationManagerService`；移除对 toggle 动态监听的硬编码。
3. 清理调用方对高级功能（注册/监听/评估）的引用，提供静态配置替代。

## 风险与缓解
- 风险：部分功能曾依赖开关动态变更。缓解：提供“手动触发重载”与简单文件监控；文档标注不再支持灰度/评估。

## 依赖
- Microsoft.Extensions.Configuration*

## 回滚方案
- 容器回切 `FeatureToggleService` 老实现，并恢复 `HotReloadService` 对其引用。

## 度量
- FeatureToggle API 面减少 ≥ 60%；相关调用处减少到仅 `IsEnabled`。

## 测试计划
- 手动：修改配置文件→触发 `TriggerReloadAsync` 验证回调生效。

## 受影响文件（示例）
- `src/Client/Desktop/Core/Services/Configuration/FeatureToggleService.cs`
- `src/Client/Desktop/Core/Services/Configuration/HotReloadService.cs`
- `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

